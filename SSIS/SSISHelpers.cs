﻿using Microsoft.DataWarehouse.Design;
using Microsoft.DataWarehouse.Project;
using Microsoft.SqlServer.Dts.Runtime;

namespace BIDSHelper.SSIS
{
    public enum SsisTargetServerVersion
    {
        SQLServer2012 = 110,
        SQLServer2014 = 120,
        SQLServer2016 = 130
        //    ,
        //Latest = 130
    }

    public static class SSISHelpers
    {

#if SQL2016
        public const string CreationNameIndex = "5";
#elif SQL2014
        public const string CreationNameIndex = "4";
#elif DENALI
        public const string CreationNameIndex = "3";
#endif

        public enum SsisDesignerTabIndex
        {
            ControlFlow = 0,
            DataFlow = 1,
            Parameters = 2,
            EventHandlers = 3,
            PackageExplorer = 4
        }

#if SQL2016
        /// <summary>
        /// Extension method for DTSTargetServerVersion, that always returns a specific version. 
        /// This ensures that DTSTargetServerVersion.Latest is translated to whatevere the latest version is, 
        /// for the current compilation target.
        /// </summary>
        /// <param name="targetServerVersion">The DTSTargetServerVersion value.</param>
        /// <returns>A version specific DTSTargetServerVersion value.</returns>
        public static DTSTargetServerVersion ToSpecificTargetServerVersion(this DTSTargetServerVersion targetServerVersion)
        {
            if (targetServerVersion == DTSTargetServerVersion.Latest)
            {
                // In future versions, add conditional compiltion here to ensure correct version is returned for target compilation
                return DTSTargetServerVersion.SQLServer2016;
            }

            return targetServerVersion;
        }
#endif
        public static void MarkPackageDirty(Package package)
        {
            if (package == null) return;

            var trans = Cud.BeginTransaction(package);
            trans.ChangeComponent(package);
            trans.Commit();

        }

        public static Package GetPackageFromContainer(DtsContainer container)
        {
            while (!(container is Package))
            {
                container = container.Parent;
            }
            return (Package)container;
        }

        public static bool IsParameterVariable(Variable v)
        {
            return v.Namespace.StartsWith("$");
        }

        private static SsisTargetServerVersion CompilationVersion
        {
            get
            {
#if SQL2016
                return SsisTargetServerVersion.SQLServer2016;
#elif SQL2014
                return SsisTargetServerVersion.SQLServer2014;
#elif DENALI
                return SsisTargetServerVersion.SQLServer2012;
#endif
            }
    }

    /// <summary>
    /// Get the SsisTargetServerVersion of a project. See PackageHelper.SetTargetServerVersion and PackageHelper.TargetServerVersion or actual usage.
    /// </summary>
    /// <param name="project">The project to get the version of</param>
    /// <returns>The DTSTargetServerVersion of the project specified.</returns>
    /// <remarks>Do not use directly, see PackageHelper.SetTargetServerVersion and PackageHelper.TargetServerVersion for actual usage.</remarks>
    internal static SsisTargetServerVersion GetTargetServerVersion(EnvDTE.Project project)
        {
#if DENALI || SQL2014
            return CompilationVersion;
#else
            // TODO: If this doesn't work <2016, we can just hardcode, based on conditional compiation
            Microsoft.DataWarehouse.Interfaces.IConfigurationSettings settings = (Microsoft.DataWarehouse.Interfaces.IConfigurationSettings)((System.IServiceProvider)project).GetService(typeof(Microsoft.DataWarehouse.Interfaces.IConfigurationSettings));
            DataWarehouseProjectManager projectManager = (DataWarehouseProjectManager)PackageHelper.GetPropertyValue(settings, "ProjectManager");
            Microsoft.DataTransformationServices.Project.DataTransformationsProjectConfigurationOptions options = (Microsoft.DataTransformationServices.Project.DataTransformationsProjectConfigurationOptions)projectManager.ConfigurationManager.CurrentConfiguration.Options;
            return (SsisTargetServerVersion)options.TargetServerVersion;         
#endif              
        }

        /// <summary>s
        /// Get the DTSTargetServerVersion of a package.
        /// </summary>
        /// <param name="package">The package to get the version of</param>
        /// <returns>The DTSTargetServerVersion of the package specified.</returns>
        /// <remarks>Do not use directly, see PackageHelper.SetTargetServerVersion and PackageHelper.TargetServerVersion for actual usage.</remarks>
        internal static SsisTargetServerVersion GetTargetServerVersion(Package package)
        {
#if DENALI || SQL2014
            return CompilationVersion;
#else
            DTSTargetServerVersion targetServerVersion = (DTSTargetServerVersion)PackageHelper.GetPropertyValue(package, "TargetServerVersion");
            return (SsisTargetServerVersion)targetServerVersion;
#endif
        }

        internal static DtsContainer FindContainer(DtsContainer component, string objectId)
        {
            if (component == null)
            {
                return null;
            }
            else if (component.ID == objectId)
            {
                return component;
            }

            EventsProvider eventsProvider = component as EventsProvider;
            if (eventsProvider != null)
            {
                foreach (DtsEventHandler eventhandler in eventsProvider.EventHandlers)
                {
                    DtsContainer container = FindContainer(eventhandler, objectId);
                    if (container != null)
                    {
                        return container;
                    }
                }
            }

            IDTSSequence sequence = component as IDTSSequence;
            if (sequence != null)
            {
                foreach (Executable executable in sequence.Executables)
                {
                    DtsContainer container = FindContainer((DtsContainer)executable, objectId);
                    if (container != null)
                    {
                        return container;
                    }
                }
            }

            return null;
        }

        internal static Variable FindVariable(DtsContainer container, string objectID)
        {
            if (container != null)
            {
                if (container.Variables.Contains(objectID))
                {
                    return container.Variables[objectID];
                }
            }

            return null;
        }

        internal static ConnectionManager FindConnectionManager(Package package, string objectID)
        {
            if (package.Connections.Contains(objectID))
            {
                return package.Connections[objectID];
            }

            return null;
        }

        internal static PrecedenceConstraint FindConstraint(DtsContainer container, string objectID)
        {
            IDTSSequence sequence = container as IDTSSequence;

            if (sequence == null)
            {
                System.Diagnostics.Debug.Assert(false, "sequence cannot be found");
                return null;
            }

            if (sequence.PrecedenceConstraints.Contains(objectID))
            {
                return sequence.PrecedenceConstraints[objectID];
            }

            return null;
        }
    }
}
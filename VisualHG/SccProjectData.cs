using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHG
{
    /// <summary>
    ///     Enum of project types with workarounds
    /// </summary>
    internal enum SccProjectType
    {
        Normal,
        SolutionFolder,
        WebSite
    }

    [DebuggerDisplay("Project={ProjectName}, ProjectType={_projectType}")]
    internal class SccProjectData
    {
        private string _projectName;
        private string _projectDirectory;

        private readonly IVsSccProject2 _sccProject;
        private readonly IVsHierarchy _hierarchy;
        private readonly IVsProject _vsProject;

        private readonly SccProjectType _projectType;
        //readonly SccProvider    _context;

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        public SccProjectData(IVsSccProject2 project)
        {
            /*if (context == null)
                throw new ArgumentNullException("context");
            else if (project == null)
                throw new ArgumentNullException("project");

            _context = context;
            */
            // Project references to speed up marshalling
            _sccProject = project;
            _hierarchy = (IVsHierarchy) project; // A project must be a hierarchy in VS
            _vsProject = (IVsProject) project; // A project must be a VS project

            _projectType = GetProjectType(project);

            _projectName = ProjectName((IVsHierarchy) project);
            _projectDirectory = ProjectDirectory((IVsHierarchy) project);
            ;
        }

        public bool IsSolutionFolder => _projectType == SccProjectType.SolutionFolder;

        public bool IsWebSite => _projectType == SccProjectType.WebSite;

        public static string ProjectName(IVsHierarchy hierarchy)
        {
            var projectName = "";
            if (hierarchy != null)
            {
                object name;

                if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT,
                    (int) __VSHPROPID.VSHPROPID_Name, out name)))
                    projectName = name as string;
            }

            return projectName;
        }

        /// <summary>
        ///     Gets the project directory.
        /// </summary>
        /// <value>The project directory or null if the project does not have one</value>
        public static string ProjectDirectory(IVsHierarchy hierarchy)
        {
            var projectDirectory = "";
            if (hierarchy != null)
            {
                projectDirectory = "";
                object name;

                if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT,
                    (int) __VSHPROPID.VSHPROPID_ProjectDir, out name)))
                {
                    var dir = name as string;

                    if (dir != null)
                        dir = GetNormalizedFullPath(dir);

                    projectDirectory = dir;
                }
            }
            return projectDirectory;
        }

        /// <summary>
        ///     Checks whether the specified project is a solution folder
        /// </summary>
        private static readonly Guid SolutionFolderProjectId = new Guid("2150e333-8fdc-42a3-9474-1a3956d46de8");

        private static readonly Guid WebsiteProjectId = new Guid("e24c65dc-7377-472b-9aba-bc803b73c61a");

        private static SccProjectType GetProjectType(IVsSccProject2 project)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var pFileFormat = project as IPersistFileFormat;
            if (pFileFormat != null)
            {
                Guid guidClassId;
                if (VSConstants.S_OK != pFileFormat.GetClassID(out guidClassId))
                    return SccProjectType.Normal;

                if (guidClassId == SolutionFolderProjectId)
                    return SccProjectType.SolutionFolder;
                if (guidClassId == WebsiteProjectId)
                    return SccProjectType.WebSite;
            }

            return SccProjectType.Normal;
        }

        private static string GetNormalizedFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            path = Path.GetFullPath(path);

            if (path.Length >= 2 && path[1] == ':')
            {
                var c = path[0];

                if (c >= 'a' && c <= 'z')
                    path = c.ToString().ToUpperInvariant() + path.Substring(1);

                var r = path.TrimEnd('\\');

                if (r.Length > 3)
                    return r;
                return path.Substring(0, 3);
            }
            if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
            {
                var root = Path.GetPathRoot(path).ToLowerInvariant();

                if (!path.StartsWith(root, StringComparison.Ordinal))
                    path = root + path.Substring(root.Length).TrimEnd('\\');
            }
            else
            {
                path = path.TrimEnd('\\');
            }

            return path;
        }
    }
}
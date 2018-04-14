using HGLib;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHG
{
    // ---------------------------------------------------------------------------
    // 
    // Solution file status cache.
    //
    // To keep the files up to date we react to SccProviderSrvice requests and also
    // handles directory watcher events.
    //
    // ---------------------------------------------------------------------------
    public class HgStatusTracker : HGStatus
    {
        /// <summary>
        ///     Called by SccProviderSrvice when a scc-capable project is opened
        /// </summary>
        /// <param name="project">The loaded project</param>
        /// <param name="added">The project was added after opening</param>
        internal void UpdateProject(IVsSccProject2 project)
        {
            if (project != null)
            {
                var projectDirectory = SccProjectData.ProjectDirectory((IVsHierarchy) project);
                var projectName = SccProjectData.ProjectName((IVsHierarchy) project);

                AddWorkItem(new UpdateRootDirectoryAdded(projectDirectory));
            }
        }

        public void UpdateProjects(IVsSolution sol)
        {
            uint numberOfProjects;
            sol.GetProjectFilesInSolution(0, 0, null, out numberOfProjects);
            var projectFiles = new string[numberOfProjects];
            sol.GetProjectFilesInSolution(0, numberOfProjects, projectFiles, out numberOfProjects);

            foreach (var projectFile in projectFiles)
                if (!string.IsNullOrEmpty(projectFile))
                {
                    var projectDirectory = projectFile.Substring(0, projectFile.LastIndexOf("\\") + 1);
                    AddWorkItem(new UpdateRootDirectoryAdded(projectDirectory));
                }
        }
    }
}
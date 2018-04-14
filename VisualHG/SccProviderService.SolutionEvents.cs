using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using HGLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHG
{
    public partial class SccProviderService
    {
        //--------------------------------------------------------------------------------
        // IVsSolutionEvents and IVsSolutionEvents2 specific functions
        //--------------------------------------------------------------------------------

        #region IVsSolutionEvents interface functions

        public int OnAfterOpenSolution([In] object pUnkReserved, [In] int fNewSolution)
        {
            Trace.WriteLine("OnAfterOpenSolution");

            // Make VisualHG the active SCC controler on Mercurial solution types
            if (!Active && Configuration.Global.AutoActivatePlugin)
            {
                var root = _sccProvider.GetRootDirectory();
                if (root.Length > 0)
                {
                    var rscp = (IVsRegisterScciProvider) _sccProvider.GetService(typeof(IVsRegisterScciProvider));
                    rscp.RegisterSourceControlProvider(GuidList.GuidSccProvider);
                }
            }
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution([In] object pUnkReserved)
        {
            Trace.WriteLine("OnAfterCloseSolution");

            StatusTracker.ClearStatusCache();
            _sccProvider.LastSeenProjectDir = string.Empty;
            // update pending tool window
            UpdatePendingWindowState();

            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject([In] IVsHierarchy pStubHierarchy, [In] IVsHierarchy pRealHierarchy)
        {
            Trace.WriteLine("OnAfterLoadProject");

            _sccProvider.LastSeenProjectDir = SccProjectData.ProjectDirectory(pRealHierarchy);
            // ReSharper disable once SuspiciousTypeConversion.Global
            StatusTracker.UpdateProject(pRealHierarchy as IVsSccProject2);
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject([In] IVsHierarchy pHierarchy, [In] int fAdded)
        {
            Trace.WriteLine("OnAfterOpenProject");

            //if (fAdded == 1)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var project = pHierarchy as IVsSccProject2;

                var fileList = SccProvider.GetProjectFiles(project);
                StatusTracker.AddFileToProjectCache(fileList, project);

                if (fileList.Count > 0)
                {
                    var files = new string[fileList.Count];
                    fileList.CopyTo(files, 0);
                    // add only files wich are not ignored
                    if (Configuration.Global.AutoAddFiles)
                        StatusTracker.AddWorkItem(new TrackFilesAddedNotIgnored(files));
                    else
                        StatusTracker.AddWorkItem(new UpdateFileStatusCommand(files));
                }
            }

            _sccProvider.LastSeenProjectDir = SccProjectData.ProjectDirectory(pHierarchy);
            // ReSharper disable once SuspiciousTypeConversion.Global
            StatusTracker.UpdateProject(pHierarchy as IVsSccProject2);
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject([In] IVsHierarchy pHierarchy, [In] int fRemoved)
        {
            if (StatusTracker.FileProjectMapCacheCount() > 0)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var project = pHierarchy as IVsSccProject2;
                var fileList = SccProvider.GetProjectFiles(project);
                StatusTracker.RemoveFileFromProjectCache(fileList);
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution([In] object pUnkReserved)
        {
            StatusTracker.ClearFileToProjectCache();
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject([In] IVsHierarchy pRealHierarchy, [In] IVsHierarchy pStubHierarchy)
        {
            if (StatusTracker.FileProjectMapCacheCount() > 0)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var project = pRealHierarchy as IVsSccProject2;
                var fileList = SccProvider.GetProjectFiles(project);
                StatusTracker.RemoveFileFromProjectCache(fileList);
            }

            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject([In] IVsHierarchy pHierarchy, [In] int fRemoving, [In] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution([In] object pUnkReserved, [In] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject([In] IVsHierarchy pRealHierarchy, [In] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterMergeSolution([In] object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        #endregion


        //--------------------------------------------------------------------------------
        // IVsTrackProjectDocumentsEvents2 specific functions
        //--------------------------------------------------------------------------------

        #region IVsTrackProjectDocumentsEvents2 interface funcions

        public int OnQueryAddFiles([In] IVsProject pProject, [In] int cFiles, [In] string[] rgpszMkDocuments,
            [In] VSQUERYADDFILEFLAGS[] rgFlags, [Out] VSQUERYADDFILERESULTS[] pSummaryResult,
            [Out] VSQUERYADDFILERESULTS[] rgResults)
        {
            StatusTracker.EnableDirectoryWatching(false);
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Implement this function to update the project scc glyphs when the items are added to the project.
        ///     If a project doesn't call GetSccGlyphs as they should do (as solution folder do), this will update correctly the
        ///     glyphs when the project is controled
        /// </summary>
        public int OnAfterAddFilesEx([In] int cProjects, [In] int cFiles, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgpszMkDocuments, [In] VSADDFILEFLAGS[] rgFlags)
        {
            StatusTracker.EnableDirectoryWatching(true);

            HGFileStatusInfo info;
            StatusTracker.GetFileStatusInfo(rgpszMkDocuments[0], out info);
            if (info == null || info.status == HGFileStatus.scsRemoved || // undelete file
                info.status == HGFileStatus.scsUncontrolled) // do not add files twice
                if (Configuration.Global.AutoAddFiles)
                    StatusTracker.AddWorkItem(new TrackFilesAddedNotIgnored(rgpszMkDocuments));
            return VSConstants.S_OK;
        }

        public int OnQueryAddDirectories([In] IVsProject pProject, [In] int cDirectories,
            [In] string[] rgpszMkDocuments, [In] VSQUERYADDDIRECTORYFLAGS[] rgFlags,
            [Out] VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, [Out] VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            Trace.WriteLine("OnQueryAddDirectories");
            for (var iDirectory = 0; iDirectory < cDirectories; ++iDirectory)
                Trace.WriteLine("    dir: " + rgpszMkDocuments[iDirectory] + ", flag: " + rgFlags[iDirectory]);

            return VSConstants.S_OK;
        }

        public int OnAfterAddDirectoriesEx([In] int cProjects, [In] int cDirectories, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgpszMkDocuments, [In] VSADDDIRECTORYFLAGS[] rgFlags)
        {
            Trace.WriteLine("OnAfterAddDirectoriesEx");
            for (var iDirectory = 0; iDirectory < cDirectories; ++iDirectory)
                Trace.WriteLine("    dir: " + rgpszMkDocuments[iDirectory] + ", flag: " + rgFlags[iDirectory]);

            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Implement OnQueryRemoveFilesevent to warn the user when he's deleting controlled files.
        ///     The user gets the chance to cancel the file removal.
        /// </summary>
        public int OnQueryRemoveFiles([In] IVsProject pProject, [In] int cFiles, [In] string[] rgpszMkDocuments,
            [In] VSQUERYREMOVEFILEFLAGS[] rgFlags, [Out] VSQUERYREMOVEFILERESULTS[] pSummaryResult,
            [Out] VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            StatusTracker.EnableDirectoryWatching(false);
            return VSConstants.S_OK;
        }

        public int OnAfterRemoveFiles([In] int cProjects, [In] int cFiles, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgpszMkDocuments, [In] VSREMOVEFILEFLAGS[] rgFlags)
        {
            StatusTracker.EnableDirectoryWatching(true);

            if (rgpProjects == null || rgpszMkDocuments == null)
                return VSConstants.E_POINTER;

            if (!File.Exists(rgpszMkDocuments[0])) // EnterFileRemoved only if the file was actually removed
                StatusTracker.AddWorkItem(new TrackFileRemoved(rgpszMkDocuments));

            return VSConstants.S_OK;
        }

        public int OnQueryRemoveDirectories([In] IVsProject pProject, [In] int cDirectories,
            [In] string[] rgpszMkDocuments, [In] VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags,
            [Out] VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, [Out] VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterRemoveDirectories([In] int cProjects, [In] int cDirectories, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgpszMkDocuments, [In] VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            //StoreSolution();
            return VSConstants.S_OK;
        }

        public int OnQueryRenameFiles([In] IVsProject pProject, [In] int cFiles, [In] string[] rgszMkOldNames,
            [In] string[] rgszMkNewNames, [In] VSQUERYRENAMEFILEFLAGS[] rgFlags,
            [Out] VSQUERYRENAMEFILERESULTS[] pSummaryResult, [Out] VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            StatusTracker.EnableDirectoryWatching(false);
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Implement OnAfterRenameFiles event to rename a file in the source control store when it gets renamed in the project
        ///     Also, rename the store if the project itself is renamed
        /// </summary>
        public int OnAfterRenameFiles([In] int cProjects, [In] int cFiles, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgszMkOldNames, [In] string[] rgszMkNewNames,
            [In] VSRENAMEFILEFLAGS[] rgFlags)
        {
            StatusTracker.EnableDirectoryWatching(true);
            StatusTracker.AddWorkItem(new TrackFilesRenamed(rgszMkOldNames, rgszMkNewNames));
            return VSConstants.S_OK;
        }

        public int OnQueryRenameDirectories([In] IVsProject pProject, [In] int cDirs, [In] string[] rgszMkOldNames,
            [In] string[] rgszMkNewNames, [In] VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags,
            [Out] VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, [Out] VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRenameDirectories([In] int cProjects, [In] int cDirs, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgszMkOldNames, [In] string[] rgszMkNewNames,
            [In] VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterSccStatusChanged([In] int cProjects, [In] int cFiles, [In] IVsProject[] rgpProjects,
            [In] int[] rgFirstIndices, [In] string[] rgpszMkDocuments, [In] uint[] rgdwSccStatus)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion  // IVsTrackProjectDocumentsEvents2 interface funcions
    }
}
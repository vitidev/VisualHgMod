using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using HGLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHG
{
    [Guid(GuidList.ProviderServiceGuid)]
    public partial class SccProviderService :
        IVsSccProvider, // Required for provider registration with source control manager
        IVsSccManager2, // Base source control functionality interface
        IVsSccManagerTooltip, // Provide tooltips for source control items
        IVsSolutionEvents, // We'll register for solution events, these are usefull for source control
        IVsSolutionEvents2,
        IVsQueryEditQuerySave2, // Required to allow editing of controlled files 
        IVsTrackProjectDocumentsEvents2, // Usefull to track project changes (add, renames, deletes, etc)
        IVsSccGlyphs,
        IVsUpdateSolutionEvents,
        IDisposable
    {
        // Whether the provider is active or not
        // The service and source control provider
        private readonly SccProvider _sccProvider;

        // The cookie for solution events 
        private uint _vsSolutionEventsCookie;

        // The cookie for project document events
        private uint _tpdTrackProjectDocumentsCookie;

        // solution file status cache
        // service.advise IVsUpdateSolutionEvents cooky
        private readonly uint _dwBuildManagerCooky;

        // DirtyNodesGlyphs update flag
        private bool _bNodesGlyphsDirty = true;

        // remember the latest OnQueryRemoveDirectories remove list
        //string[] _RemoveDirectoriesQueue = null;

        #region SccProvider Service initialization/unitialization

        public SccProviderService(SccProvider sccProvider)
        {
            Debug.Assert(null != sccProvider);
            _sccProvider = sccProvider;

            // Subscribe to solution events
            var sol = (IVsSolution) _sccProvider.GetService(typeof(SVsSolution));
            sol.AdviseSolutionEvents(this, out _vsSolutionEventsCookie);
            Debug.Assert(VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie);

            // Subscribe to project documents
            var tpdService = (IVsTrackProjectDocuments2) _sccProvider.GetService(typeof(SVsTrackProjectDocuments));
            tpdService.AdviseTrackProjectDocumentsEvents(this, out _tpdTrackProjectDocumentsCookie);
            Debug.Assert(VSConstants.VSCOOKIE_NIL != _tpdTrackProjectDocumentsCookie);

            // Subscribe to status events
            StatusTracker.HGStatusChanged += SetNodesGlyphsDirty;

            var buildManagerService =
                _sccProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            buildManagerService.AdviseUpdateSolutionEvents(this, out _dwBuildManagerCooky);
        }

        public void Dispose()
        {
            // Unregister from receiving solution events
            if (VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie)
            {
                var sol = (IVsSolution) _sccProvider.GetService(typeof(SVsSolution));
                sol.UnadviseSolutionEvents(_vsSolutionEventsCookie);
                _vsSolutionEventsCookie = VSConstants.VSCOOKIE_NIL;
            }

            // Unregister from receiving project documents
            if (VSConstants.VSCOOKIE_NIL != _tpdTrackProjectDocumentsCookie)
            {
                var tpdService = (IVsTrackProjectDocuments2) _sccProvider.GetService(typeof(SVsTrackProjectDocuments));
                tpdService.UnadviseTrackProjectDocumentsEvents(_tpdTrackProjectDocumentsCookie);
                _tpdTrackProjectDocumentsCookie = VSConstants.VSCOOKIE_NIL;
            }

            // Unregister from storrage events
            StatusTracker.HGStatusChanged -= SetNodesGlyphsDirty;

            var buildManagerService =
                _sccProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            buildManagerService.UnadviseUpdateSolutionEvents(_dwBuildManagerCooky);
        }

        #endregion

        // access to the tracker object
        public HgStatusTracker StatusTracker { get; } = new HgStatusTracker();

        //--------------------------------------------------------------------------------
        // IVsSccProvider specific functions
        //--------------------------------------------------------------------------------

        #region IVsSccProvider interface functions

        // Called by the scc manager when the provider is activated. 
        // Make visible and enable if necessary scc related menu commands
        public int SetActive()
        {
            Trace.WriteLine("SetActive");

            Active = true;

            // add all projects of this solution to the status file cache
            var solution = (IVsSolution) _sccProvider.GetService(typeof(SVsSolution));
            StatusTracker.UpdateProjects(solution);
            StatusTracker.SetCacheDirty();

            return VSConstants.S_OK;
        }

        // Called by the scc manager when the provider is deactivated. 
        // Hides and disable scc related menu commands
        public int SetInactive()
        {
            Trace.WriteLine("SetInactive");

            Active = false;
            return VSConstants.S_OK;
        }

        public int AnyItemsUnderSourceControl(out int pfResult)
        {
            if (!Active)
                pfResult = 0;
            else
                pfResult = StatusTracker.AnyItemsUnderSourceControl() ? 1 : 0;

            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsQueryEditQuerySave2 specific functions
        //--------------------------------------------------------------------------------

        #region IVsQueryEditQuerySave2 interface functions

        public int BeginQuerySaveBatch()
        {
            return VSConstants.S_OK;
        }

        public int EndQuerySaveBatch()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     States that a file will be reloaded if it changes on disk.
        /// </summary>
        /// <param name="pszMkDocument">The PSZ mk document.</param>
        /// <param name="rgf">The RGF.</param>
        /// <param name="pFileInfo">The p file info.</param>
        /// <returns></returns>
        public int DeclareReloadableFile([In] string pszMkDocument, [In] uint rgf,
            [In] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     States that a file will not be reloaded if it changes on disk
        /// </summary>
        /// <param name="pszMkDocument">The PSZ mk document.</param>
        /// <param name="rgf">The RGF.</param>
        /// <param name="pFileInfo">The p file info.</param>
        /// <returns></returns>
        public int DeclareUnreloadableFile([In] string pszMkDocument, [In] uint rgf,
            [In] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            return VSConstants.S_OK;
        }

        public int IsReloadable([In] string pszMkDocument, out int pbResult)
        {
            // Since we're not tracking which files are reloadable and which not, consider everything reloadable
            pbResult = 1;
            return VSConstants.S_OK;
        }

        public int OnAfterSaveUnreloadableFile([In] string pszMkDocument, [In] uint rgf,
            [In] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Called by projects and editors before modifying a file
        /// </summary>
        public int QueryEditFiles([In] uint rgfQueryEdit, [In] int cFiles, [In] string[] rgpszMkDocuments,
            [In] uint[] rgrgf, [In] VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pfEditVerdict,
            out uint prgfMoreInfo)
        {
            // Initialize output variables
            pfEditVerdict = (uint) tagVSQueryEditResult.QER_EditOK;
            prgfMoreInfo = 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Called by editors and projects before saving the files
        /// </summary>
        public int QuerySaveFile([In] string pszMkDocument, [In] uint rgf, [In] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo,
            out uint pdwQSResult)
        {
            Trace.WriteLine("QuerySaveFile");
            Trace.WriteLine("    dir: " + pszMkDocument);
            if (Active && File.Exists(pszMkDocument))
                try
                {
                    var attribures = File.GetAttributes(pszMkDocument);

                    // Make the file writable and allow the save
                    if ((attribures & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(pszMkDocument, attribures & ~FileAttributes.ReadOnly);

                    string[] files = {pszMkDocument};
                    StatusTracker.AddWorkItem(new UpdateFileStatusCommand(files));
                }
                catch
                {
                }

            pdwQSResult = (uint) tagVSQuerySaveResult.QSR_SaveOK;
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Called by editors and projects before saving the files
        /// </summary>
        public int QuerySaveFiles([In] uint rgfQuerySave, [In] int cFiles, [In] string[] rgpszMkDocuments,
            [In] uint[] rgrgf, [In] VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pdwQSResult)
        {
            Trace.WriteLine("QuerySaveFiles");
            for (var iFile = 0; iFile < cFiles; ++iFile)
                Trace.WriteLine("    dir: " + rgpszMkDocuments[iFile]);

            StatusTracker.AddWorkItem(new UpdateFileStatusCommand(rgpszMkDocuments));

            pdwQSResult = (uint) tagVSQuerySaveResult.QSR_SaveOK;
            return VSConstants.S_OK;
        }

        #endregion

        #region Files and Project Management Functions

        /// <summary>
        ///     Returns whether this source control provider is the active scc provider.
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        ///     Checks whether the specified project or solution (pHier==null) is under source control
        /// </summary>
        /// <returns>True if project is controlled.</returns>
        public bool IsProjectControlled(IVsHierarchy pHier)
        {
            return StatusTracker.AnyItemsUnderSourceControl();
        }

        /// <summary>
        ///     query for scc file status
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public HGFileStatus GetFileStatus(string filename)
        {
            return StatusTracker.GetFileStatus(filename);
        }

        /// <summary>
        ///     set the node glyphs dirty flag to true
        /// </summary>
        public void SetNodesGlyphsDirty()
        {
            _bNodesGlyphsDirty = true;
        }

        /// <summary>
        ///     call RefreshNodesGlyphs to update all Glyphs
        ///     if the _bNodesGlyphsDirty is true
        /// </summary>
        private long lastUpdate;

        private IVsSccGlyphs _vsSccGlyphsImplementation;

        public void UpdateDirtyNodesGlyphs()
        {
            if (_bNodesGlyphsDirty && DateTime.Now.Ticks - lastUpdate > 100)
            {
                UpdatePendingWindowState();
                RefreshNodesGlyphs();
                // update main caption
                _sccProvider.UpdateMainWindowTitle(StatusTracker.FormatBranchList());

                lastUpdate = DateTime.Now.Ticks;
                _bNodesGlyphsDirty = false;
            }
        }

        public void RefreshNodesGlyphs()
        {
            var solHier = (IVsHierarchy) _sccProvider.GetService(typeof(SVsSolution));
            var projectList = _sccProvider.GetLoadedControllableProjects();

            // We'll also need to refresh the solution folders glyphs
            // to reflect the controlled state
            IList<VSITEMSELECTION> nodes = new List<VSITEMSELECTION>();

            {
                // add solution root item
                VSITEMSELECTION vsItem;
                vsItem.itemid = VSConstants.VSITEMID_ROOT;
                vsItem.pHier = solHier; // pHierarchy;
                nodes.Add(vsItem);
            }

            // add project node items
            foreach (IVsHierarchy hr in projectList)
            {
                VSITEMSELECTION vsItem;
                vsItem.itemid = VSConstants.VSITEMID_ROOT;
                vsItem.pHier = hr;
                nodes.Add(vsItem);
            }

            _sccProvider.RefreshNodesGlyphs(nodes);
        }

        private void UpdatePendingWindowState()
        {
            object pane = _sccProvider.FindToolWindow(VisualHgToolWindow.PendingChanges);
            if (pane != null)
                ((HGPendingChangesToolWindow) pane).UpdatePendingList(StatusTracker);
        }

        #endregion

        #region IVsUpdateSolutionEvents Members

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            Trace.WriteLine("UpdateSolution_Done");

            StatusTracker.UpdateSolution_Done();
            return VSConstants.E_NOTIMPL;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            Trace.WriteLine("UpdateSolution_StartUpdate");

            StatusTracker.UpdateSolution_StartUpdate();
            return VSConstants.E_NOTIMPL;
        }

        #endregion

    
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using HGLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace VisualHG
{
    /// <summary>
    ///     SccProvider VSCT defined menu command handler
    /// </summary>
    partial class SccProvider
    {
        /// <summary>
        ///     Add our command handlers for menu (commands must exist in the .vsct file)
        /// </summary>
        private void InitVSCTMenuCommandHandler()
        {
            if (!(GetService(typeof(IMenuCommandService)) is MsVsShell.OleMenuCommandService mcs))
                return;

            // ToolWindow Command
            var cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdViewToolWindow);
            var menuCmd = new MenuCommand(Exec_icmdViewToolWindow, cmd);
            mcs.AddCommand(menuCmd);

            // ToolWindow's ToolBar Command
            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdToolWindowToolbarCommand);
            menuCmd = new MenuCommand(Exec_icmdToolWindowToolbarCommand, cmd);
            mcs.AddCommand(menuCmd);

            // Source control menu commmads
            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgStatus);
            menuCmd = new MenuCommand(Exec_icmdHgStatus, cmd);
            mcs.AddCommand(menuCmd);

            // Source control menu commmads
            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgDiff);
            menuCmd = new MenuCommand(Exec_icmdHgDiff, cmd);
            mcs.AddCommand(menuCmd);

            // Source control menu commmads
            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgDiffExt);
            menuCmd = new MenuCommand(Exec_icmdHgDiffExt, cmd);
            mcs.AddCommand(menuCmd);


            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgCommitRoot);
            menuCmd = new MenuCommand(Exec_icmdHgCommitRoot, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgCommitSelected);
            menuCmd = new MenuCommand(Exec_icmdHgCommitSelected, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgHistoryRoot);
            menuCmd = new MenuCommand(Exec_icmdHgHistoryRoot, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgHistorySelected);
            menuCmd = new MenuCommand(Exec_icmdHgHistorySelected, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgSynchronize);
            menuCmd = new MenuCommand(Exec_icmdHgSynchronize, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgUpdateToRevision);
            menuCmd = new MenuCommand(Exec_icmdHgUpdateToRevision, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgRevert);
            menuCmd = new MenuCommand(Exec_icmdHgRevert, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgAnnotate);
            menuCmd = new MenuCommand(Exec_icmdHgAnnotate, cmd);
            mcs.AddCommand(menuCmd);

            cmd = new CommandID(GuidList.GuidSccProviderCmdSet, CommandId.IcmdHgAddSelected);
            menuCmd = new MenuCommand(Exec_icmdHgAddSelected, cmd);
            mcs.AddCommand(menuCmd);
        }

        #region Source Control Command Enabling IOleCommandTarget.QueryStatus

        /// <summary>
        ///     The shell call this function to know if a menu item should be visible and
        ///     if it should be enabled/disabled.
        ///     Note that this function will only be called when an instance of this editor
        ///     is open.
        /// </summary>
        /// <param name="guidCmdGroup">Guid describing which set of command the current command(s) belong to</param>
        /// <param name="cCmds">Number of command which status are being asked for</param>
        /// <param name="prgCmds">Information for each command</param>
        /// <param name="pCmdText">Used to dynamically change the command text</param>
        /// <returns>HRESULT</returns>
        public int QueryStatus(ref Guid guidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            Debug.Assert(cCmds == 1, "Multiple commands");
            Debug.Assert(prgCmds != null, "NULL argument");

            if (prgCmds == null)
                return VSConstants.E_INVALIDARG;

            // Filter out commands that are not defined by this package
            if (guidCmdGroup != GuidList.GuidSccProviderCmdSet)
            {
                return (int) Constants.OLECMDERR_E_NOTSUPPORTED;
                ;
            }

            var cmdf = OLECMDF.OLECMDF_SUPPORTED;

            // All source control commands needs to be hidden and disabled when the provider is not active
            if (!_sccService.Active)
            {
                cmdf = OLECMDF.OLECMDF_INVISIBLE;

                prgCmds[0].cmdf = (uint) cmdf;
                return VSConstants.S_OK;
            }

            // Process our Commands
            switch (prgCmds[0].cmdID)
            {
                case CommandId.IcmdHgStatus:
                    cmdf = QueryStatus_icmdHgStatus();
                    break;

                case CommandId.IcmdHgCommitRoot:
                    cmdf = QueryStatus_icmdHgCommitRoot();
                    break;

                case CommandId.IcmdHgCommitSelected:
                    cmdf = QueryStatus_icmdHgCommitSelected();
                    break;

                case CommandId.IcmdHgHistoryRoot:
                    cmdf = QueryStatus_icmdHgHistoryRoot();
                    break;

                case CommandId.IcmdHgHistorySelected:
                    cmdf = QueryStatus_icmdHgHistorySelected();
                    break;

                case CommandId.IcmdHgSynchronize:
                    cmdf = QueryStatus_icmdHgSynchronize();
                    break;

                case CommandId.IcmdHgUpdateToRevision:
                    cmdf = QueryStatus_icmdHgUpdateToRevision();
                    break;

                case CommandId.IcmdHgDiff:
                case CommandId.IcmdHgDiffExt:
                    cmdf = QueryStatus_icmdHgDiff();
                    break;

                case CommandId.IcmdHgRevert:
                    cmdf = QueryStatus_icmdHgRevert();
                    break;

                case CommandId.IcmdHgAnnotate:
                    cmdf = QueryStatus_icmdHgAnnotate();
                    break;

                case CommandId.IcmdHgAddSelected:
                    cmdf = QueryStatus_icmdHgAddSelected();
                    break;

                case CommandId.IcmdViewToolWindow:
                case CommandId.IcmdToolWindowToolbarCommand:
                    // These commmands are always enabled when the provider is active
                    cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
                    ;
                    break;

                default:
                    return (int) Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            prgCmds[0].cmdf = (uint) cmdf;

            return VSConstants.S_OK;
        }

        private OLECMDF QueryStatus_icmdHgCommitRoot()
        {
            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
        }

        private OLECMDF QueryStatus_icmdHgAddSelected()
        {
            var cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;

            var stateMask = (long) HGFileStatus.scsUncontrolled |
                            (long) HGFileStatus.scsIgnored;

            if (!Configuration.Global.EnableContextSearch || FindSelectedFirstMask(false, stateMask))
                cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;

            return cmdf;
        }

        private OLECMDF QueryStatus_icmdHgCommitSelected()
        {
            var cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;

            var stateMask = (long) HGFileStatus.scsModified |
                            (long) HGFileStatus.scsAdded |
                            (long) HGFileStatus.scsCopied |
                            (long) HGFileStatus.scsRenamed |
                            (long) HGFileStatus.scsRemoved;

            if (!Configuration.Global.EnableContextSearch || FindSelectedFirstMask(true, stateMask))
                cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;

            return cmdf;
        }


        private OLECMDF QueryStatus_icmdHgHistoryRoot()
        {
            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
        }

        private OLECMDF QueryStatus_icmdHgHistorySelected()
        {
            var filename = GetSingleSelectedFileName();
            if (filename != string.Empty)
            {
                var status = _sccService.GetFileStatus(filename);
                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsIgnored &&
                    status != HGFileStatus.scsAdded &&
                    status != HGFileStatus.scsRenamed &&
                    status != HGFileStatus.scsCopied)
                    return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
            }

            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;
        }

        private OLECMDF QueryStatus_icmdHgStatus()
        {
            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
        }

        private OLECMDF QueryStatus_icmdHgSynchronize()
        {
            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
        }

        private OLECMDF QueryStatus_icmdHgUpdateToRevision()
        {
            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
        }

        private OLECMDF QueryStatus_icmdHgDiff()
        {
            var filename = GetSingleSelectedFileName();

            if (filename != string.Empty)
            {
                var status = _sccService.GetFileStatus(filename);
                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsAdded &&
                    status != HGFileStatus.scsIgnored &&
                    status != HGFileStatus.scsClean)
                    return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
            }

            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;
        }

        private OLECMDF QueryStatus_icmdHgRevert()
        {
            var cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;

            var stateMask = (long) HGFileStatus.scsAdded |
                            (long) HGFileStatus.scsCopied |
                            (long) HGFileStatus.scsModified |
                            (long) HGFileStatus.scsRenamed |
                            (long) HGFileStatus.scsRemoved;

            if (!Configuration.Global.EnableContextSearch || FindSelectedFirstMask(false, stateMask))
                cmdf = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;

            return cmdf;
        }

        private OLECMDF QueryStatus_icmdHgAnnotate()
        {
            var filename = GetSingleSelectedFileName();
            if (filename != string.Empty)
            {
                var status = _sccService.GetFileStatus(filename);
                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsIgnored &&
                    status != HGFileStatus.scsAdded &&
                    status != HGFileStatus.scsRenamed &&
                    status != HGFileStatus.scsCopied)
                    return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
            }

            return OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;
        }

        #endregion

        #region Source Control Commands Execution

        private void Exec_icmdHgCommitRoot(object sender, EventArgs e)
        {
            StoreSolution();

            var root = GetRootDirectory();
            if (root != string.Empty)
                CommitDialog(root);
            else
                PromptSolutionNotControlled();
        }

        private void Exec_icmdHgCommitSelected(object sender, EventArgs e)
        {
            var array = GetSelectedFileNameArray(true);
            CommitDialog(array);
        }

        public void CommitDialog(List<string> array)
        {
            StoreSolution();

            var commitList = new List<string>();
            foreach (var name in array)
            {
                var status = _sccService.GetFileStatus(name);
                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsClean &&
                    status != HGFileStatus.scsIgnored)
                    commitList.Add(name);
            }

            if (commitList.Count > 0)
                CommitDialog(commitList.ToArray());
        }

        private void Exec_icmdHgAddSelected(object sender, EventArgs e)
        {
            var array = GetSelectedFileNameArray(false);
            HgAddSelected(array);
        }

        public void HgAddSelected(List<string> array)
        {
            StoreSolution();

            var addList = new List<string>();
            foreach (var name in array)
            {
                var status = _sccService.GetFileStatus(name);
                if (status == HGFileStatus.scsUncontrolled ||
                    status == HGFileStatus.scsIgnored)
                    addList.Add(name);
            }

            if (addList.Count > 0)
                AddFilesDialog(addList.ToArray());
        }

        private void Exec_icmdHgHistoryRoot(object sender, EventArgs e)
        {
            StoreSolution();

            var root = GetRootDirectory();

            if (!string.IsNullOrEmpty(root))
                RepoBrowserDialog(root);
            else
                PromptSolutionNotControlled();
        }

        private void Exec_icmdHgHistorySelected(object sender, EventArgs e)
        {
            var fileName = GetSingleSelectedFileName();
            ShowHgHistoryDlg(fileName);
        }

        public void ShowHgHistoryDlg(string fileName)
        {
            StoreSolution();

            if (fileName != string.Empty)
            {
                var status = _sccService.GetFileStatus(fileName);
                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsAdded &&
                    status != HGFileStatus.scsIgnored)
                    LogDialog(fileName);
            }
        }

        private void Exec_icmdHgStatus(object sender, EventArgs e)
        {
            StoreSolution();

            var root = GetRootDirectory();
            if (root != string.Empty)
                StatusDialog(root);
            else
                PromptSolutionNotControlled();
        }

        private void Exec_icmdHgDiff(object sender, EventArgs e)
        {
            var fileName = GetSingleSelectedFileName();
            ShowHgDiffDlg(fileName, false);
        }

        private void Exec_icmdHgDiffExt(object sender, EventArgs e)
        {
            var fileName = GetSingleSelectedFileName();
            ShowHgDiffDlg(fileName, true);
        }

        public void ShowHgDiffDlg(string fileName, bool external)
        {
            StoreSolution();

            if (fileName != string.Empty)
            {
                var versionedFile = fileName;

                var status = _sccService.GetFileStatus(fileName);
                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsAdded &&
                    status != HGFileStatus.scsIgnored)
                {
                    if (status == HGFileStatus.scsRenamed ||
                        status == HGFileStatus.scsCopied)
                        versionedFile = HG.GetOriginalOfRenamedFile(fileName);

                    if (versionedFile != null)
                        try
                        {
                            if (external)
                            {
                                if (string.IsNullOrWhiteSpace(Configuration.Global.ExternalDiffToolCommandMask))
                                {
                                    MessageBox.Show(
                                        "The DiffTool raised an error\nPlease check your command mask:\n\n" +
                                        Configuration.Global.ExternalDiffToolCommandMask,
                                        "VisualHG",
                                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                    return;
                                }

                                DiffDialog(versionedFile, fileName, Configuration.Global.ExternalDiffToolCommandMask);
                            }
                            else
                            {
                                ShowDiff(fileName, versionedFile);
                            }
                        }
                        catch
                        {
                            if (Configuration.Global.ExternalDiffToolCommandMask != string.Empty)
                                MessageBox.Show(
                                    "The DiffTool raised an error\nPlease check your command mask:\n\n" +
                                    Configuration.Global.ExternalDiffToolCommandMask,
                                    "VisualHG",
                                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                }
            }
        }

        private void ShowDiff(string fileName, string versionedFile)
        {
            var latestRevisionFile = HGTK.GetLatestFileRevisionToTemp(versionedFile, fileName);
            var leftFileMoniker = latestRevisionFile;
            var leftLabel = versionedFile + "(base)";


            var rightFileMoniker = fileName;
            var rightLabel = fileName;

            var caption = $"(Diff) {Path.GetFileName(fileName)}";

            string tooltip = null;
            string inlineLabel = null;
            string roles = null;

            var differenceService = GetService(typeof(SVsDifferenceService)) as IVsDifferenceService;
            var grfDiffOptions = __VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary;
            differenceService.OpenComparisonWindow2(leftFileMoniker, rightFileMoniker, caption, tooltip, leftLabel,
                rightLabel, inlineLabel, roles, (uint) grfDiffOptions);

            File.Delete(latestRevisionFile);
        }

        private void Exec_icmdHgSynchronize(object sender, EventArgs e)
        {
            StoreSolution();

            var root = GetRootDirectory();
            if (root != string.Empty)
                SyncDialog(root);
            else
                PromptSolutionNotControlled();
        }

        private void Exec_icmdHgUpdateToRevision(object sender, EventArgs e)
        {
            StoreSolution();

            var root = GetRootDirectory();
            if (root != string.Empty)
                HGTK.UpdateDialog(root);
            else
                PromptSolutionNotControlled();
        }

        private void Exec_icmdHgRevert(object sender, EventArgs e)
        {
            var array = GetSelectedFileNameArray(false);
            HgRevertFileDlg(array.ToArray());
        }

        public void HgRevertFileDlg(string[] array)
        {
            StoreSolution();

            var addList = new List<string>();
            foreach (var name in array)
            {
                var status = _sccService.GetFileStatus(name);
                if (status == HGFileStatus.scsModified ||
                    status == HGFileStatus.scsAdded ||
                    status == HGFileStatus.scsCopied ||
                    status == HGFileStatus.scsRemoved ||
                    status == HGFileStatus.scsRenamed)
                    addList.Add(name);
            }

            if (addList.Count > 0)
                RevertDialog(addList.ToArray());
        }

        private void Exec_icmdHgAnnotate(object sender, EventArgs e)
        {
            var fileName = GetSingleSelectedFileName();
            HgAnnotateDlg(fileName);
        }

        public void HgAnnotateDlg(string fileName)
        {
            StoreSolution();

            if (fileName != string.Empty)
            {
                var status = _sccService.GetFileStatus(fileName);
                if (status == HGFileStatus.scsRenamed)
                {
                    // get original filename
                    var orgName = HG.GetOriginalOfRenamedFile(fileName);
                    if (orgName != string.Empty)
                        HGTK.AnnotateDialog(orgName);
                }

                if (status != HGFileStatus.scsUncontrolled &&
                    status != HGFileStatus.scsIgnored)
                    HGTK.AnnotateDialog(fileName);
            }
        }

        // The function can be used to bring back the provider's toolwindow if it was previously closed
        private void Exec_icmdViewToolWindow(object sender, EventArgs e)
        {
            var window = FindToolWindow(typeof(HGPendingChangesToolWindow), 0, true);
            IVsWindowFrame windowFrame = null;
            if (window != null && window.Frame != null)
                windowFrame = (IVsWindowFrame) window.Frame;
            if (windowFrame != null)
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void Exec_icmdToolWindowToolbarCommand(object sender, EventArgs e)
        {
            var window = (HGPendingChangesToolWindow) FindToolWindow(typeof(HGPendingChangesToolWindow), 0, true);

            window?.ToolWindowToolbarCommand();
        }

        #endregion
    }
}
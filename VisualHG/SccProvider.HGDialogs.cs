using System.Threading;
using HGLib;

namespace VisualHG
{
    partial class SccProvider
    {
        // ------------------------------------------------------------------------
        // show an wait for exit of required dialog
        // update state for given files
        // ------------------------------------------------------------------------
        private void QueueDialog(string[] files, string command)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    HGTK.HGTKSelectedFilesDialog(files, command);
                    _sccService.StatusTracker.RebuildStatusCacheRequiredFlag = false;
                    _sccService.StatusTracker.AddWorkItem(new UpdateFileStatusCommand(files));
                }
                catch
                {
                }
            });
        }

        // ------------------------------------------------------------------------
        // commit selected files dialog
        // ------------------------------------------------------------------------
        public void CommitDialog(string[] files)
        {
            QueueDialog(files, " --nofork commit ");
        }

        // ------------------------------------------------------------------------
        // add files to repo dialog
        // ------------------------------------------------------------------------
        private void AddFilesDialog(string[] files)
        {
            QueueDialog(files, " --nofork add ");
        }

        // ------------------------------------------------------------------------
        // show an wait for exit of required dialog
        // update state for given files
        // ------------------------------------------------------------------------
        private void QueueDialog(string root, string command)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    var process = HGTK.HGTKDialog(root, command);
                    process?.WaitForExit();

                    _sccService.StatusTracker.RebuildStatusCacheRequiredFlag = false;
                    _sccService.StatusTracker.AddWorkItem(new UpdateRootStatusCommand(root));
                }
                catch
                {
                }
            });
        }

        // ------------------------------------------------------------------------
        // show TortoiseHG commit dialog
        // ------------------------------------------------------------------------
        private void CommitDialog(string directory)
        {
            QueueDialog(directory, "commit");
        }

        // ------------------------------------------------------------------------
        // show TortoiseHG revert dialog
        // ------------------------------------------------------------------------
        private void RevertDialog(string[] files)
        {
            QueueDialog(files, " --nofork revert ");
        }

        // ------------------------------------------------------------------------
        // show TortoiseHG repo browser dialog
        // ------------------------------------------------------------------------
        public void RepoBrowserDialog(string root)
        {
            QueueDialog(root, "log");
        }

        // ------------------------------------------------------------------------
        // show TortoiseHG file log dialog
        // ------------------------------------------------------------------------
        public void LogDialog(string file)
        {
            var root = HG.FindRootDirectory(file);
            if (root != string.Empty)
            {
                file = file.Substring(root.Length + 1);
                QueueDialog(root, "log \"" + file + "\"");
            }
        }

        // ------------------------------------------------------------------------
        // show file diff window
        // ------------------------------------------------------------------------
        private void DiffDialog(string sccFile, string file, string commandMask)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    var process = HGTK.DiffDialog(sccFile, file, commandMask);
                    process?.WaitForExit();

                    _sccService.StatusTracker.RebuildStatusCacheRequiredFlag = false;
                    _sccService.StatusTracker.AddWorkItem(new UpdateFileStatusCommand(new[] {file}));
                }
                catch
                {
                }
            });
        }


        // ------------------------------------------------------------------------
        // show TortoiseHG synchronize dialog
        // ------------------------------------------------------------------------
        public void SyncDialog(string directory)
        {
            QueueDialog(directory, "synch");
        }

        // ------------------------------------------------------------------------
        // show TortoiseHG status dialog
        // ------------------------------------------------------------------------
        public void StatusDialog(string directory)
        {
            QueueDialog(directory, "status");
        }
    }
}
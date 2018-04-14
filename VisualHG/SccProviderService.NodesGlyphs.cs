using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HGLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHG
{
    public partial class SccProviderService
    {
        //--------------------------------------------------------------------------------
        // IVsSccManager2 specific functions
        //--------------------------------------------------------------------------------

        #region IVsSccManager2 interface functions

        public int BrowseForProject(out string pbstrDirectory, out int pfOk)
        {
            // Obsolete method
            pbstrDirectory = null;
            pfOk = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int CancelAfterBrowseForProject()
        {
            // Obsolete method
            return VSConstants.E_NOTIMPL;
        }

        /// <summary>
        ///     Returns whether the source control provider is fully installed
        /// </summary>
        public int IsInstalled(out int pbInstalled)
        {
            // All source control packages should always return S_OK and set pbInstalled to nonzero
            pbInstalled = 1;
            return VSConstants.S_OK;
        }

        private readonly ImageMapper _statusImages = new ImageMapper();
        private uint _baseIndex;
        private ImageList _glyphList;

        /// <summary>
        ///     Called by the IDE to get a custom glyph image list for source control status.
        /// </summary>
        /// <param name="baseIndex">[in] Value to add when returning glyph index.</param>
        /// <param name="pdwImageListHandle">[out] Handle to the custom image list.</param>
        /// <returns>handle of an image list</returns>
        public int GetCustomGlyphList(uint baseIndex, out uint pdwImageListHandle)
        {
            // We give VS all our custom glyphs from baseindex upwards
            if (_glyphList == null)
            {
                _baseIndex = baseIndex;
                _glyphList = _statusImages.CreateStatusImageList();
            }
            pdwImageListHandle = (uint) _glyphList.Handle;

            return VSConstants.S_OK;
        }

        private class FileOrDirEntry
        {
            public string Path;

            public HGFileStatus Status;

            public int Index;

            public FileOrDirEntry(string path, HGFileStatus status, int index)
            {
                Path = path;
                Status = status;
                Index = index;
            }

            public void TryUpdateStaus(HGFileStatus status)
            {
                if(Status==HGFileStatus.scsModified)
                    return;


                if (status == HGFileStatus.scsAdded)
                {
                    Status = HGFileStatus.scsAdded;
                    return;
                }

                if (status == HGFileStatus.scsRenamed || (status == HGFileStatus.scsCopied || status == HGFileStatus.scsModified))
                {
                    Status = HGFileStatus.scsModified;
                    return;
                }

                if (Status == HGFileStatus.scsUncontrolled)
                {
                    if (status == HGFileStatus.scsClean)
                    {
                        Status = HGFileStatus.scsClean;
                    }
                }
            }

            public bool Contains(FileOrDirEntry fileOrDirEntry)
            {
                return fileOrDirEntry.Path.StartsWith(Path);
            }
        }

        /// <summary>
        ///     Provide source control icons for the specified files and returns scc status of files
        /// </summary>
        /// <returns>The method returns S_OK if at least one of the files is controlled, S_FALSE if none of them are</returns>
        public int GetSccGlyph([In] int cFiles, [In] string[] rgpszFullPaths, [Out] VsStateIcon[] rgsiGlyphs,
            [Out] uint[] rgdwSccStatus)
        {
            if (rgpszFullPaths[0] == null)
                return 0;

            if(rgdwSccStatus==null)
                rgdwSccStatus=new uint[rgpszFullPaths.Length];

            if (rgpszFullPaths.Length == 1) //optimiztion
            {
                var status = StatusTracker.GetFileStatus(rgpszFullPaths[0]);
                rgdwSccStatus[0] = (uint)status;
                rgsiGlyphs[0] = GetStatusIcon(status);
                return VSConstants.S_OK;
            }

            var directories = new List<FileOrDirEntry>(rgpszFullPaths.Length);
            var files = new List<FileOrDirEntry>(rgpszFullPaths.Length);

            for (int i = 0; i < rgpszFullPaths.Length; i++)
            {
                // Return the icons and the status. While the status is a combination a flags, we'll return just values 
                // with one bit set, to make life easier for GetSccGlyphsFromStatus
                var fullPath = rgpszFullPaths[i];
                if (fullPath[fullPath.Length - 1] == '\\')
                {
                    directories.Add(new FileOrDirEntry(fullPath, HGFileStatus.scsUncontrolled, i));
                    continue;
                }
                var status = StatusTracker.GetFileStatus(fullPath);
                files.Add(new FileOrDirEntry(fullPath, status, i));
            }

            foreach (var dirEntry in directories)
            {
                foreach (var fileEntry in files)
                    if (dirEntry.Contains(fileEntry))
                        dirEntry.TryUpdateStaus(fileEntry.Status);
            }


            foreach (var fileOrDirEntry in directories)
            {
                rgdwSccStatus[fileOrDirEntry.Index] = (uint)fileOrDirEntry.Status;
                rgsiGlyphs[fileOrDirEntry.Index] = GetStatusIcon(fileOrDirEntry.Status);
            }

            foreach (var fileOrDirEntry in files)
            {
                rgdwSccStatus[fileOrDirEntry.Index] = (uint)fileOrDirEntry.Status;
                rgsiGlyphs[fileOrDirEntry.Index] = GetStatusIcon(fileOrDirEntry.Status);
            }

            return VSConstants.S_OK;
        }

        private VsStateIcon GetStatusIcon(HGFileStatus status)
        {
            switch (status)
            {
                // STATEICON_CHECKEDIN schloss
                // STATEICON_CHECKEDOUT roter haken
                // STATEICON_CHECKEDOUTEXCLUSIVE roter haken
                // STATEICON_CHECKEDOUTSHAREDOTHER männchen
                // STATEICON_DISABLED roter ring / durchgestrichen
                //  STATEICON_EDITABLE bleistift
                // STATEICON_EXCLUDEDFROMSCC einbahnstrasse
                // STATEICON_MAXINDEX nix
                // STATEICON_NOSTATEICON nix
                // STATEICON_ORPHANED blaue flagge
                // STATEICON_READONLY schloss

                // my states
                case HGFileStatus.scsClean:
                    return (VsStateIcon)(_baseIndex + 0);

                case HGFileStatus.scsModified:
                    return (VsStateIcon)(_baseIndex + 1);

                case HGFileStatus.scsAdded:
                    return (VsStateIcon)(_baseIndex + 2);

                case HGFileStatus.scsRenamed:
                    return (VsStateIcon)(_baseIndex + 3);

                case HGFileStatus.scsCopied:
                    return (VsStateIcon)(_baseIndex + 3); // no better icon 

                case HGFileStatus.scsRemoved:
                    return (VsStateIcon)(_baseIndex + 1);

                case HGFileStatus.scsIgnored:
                    return VsStateIcon.STATEICON_BLANK;

                case HGFileStatus.scsUncontrolled:
                    return VsStateIcon.STATEICON_BLANK;

                default: throw new ArgumentException();
            }
        }

        /// <summary>
        ///     Determines the corresponding scc status glyph to display, given a combination of scc status flags
        /// </summary>
        public int GetSccGlyphFromStatus([In] uint dwSccStatus, [Out] VsStateIcon[] psiGlyph)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     One of the most important methods in a source control provider,
        ///     is called by projects that are under source control when they are first
        ///     opened to register project settings
        /// </summary>
        public int RegisterSccProject([In] IVsSccProject2 pscp2Project, [In] string pszSccProjectName,
            [In] string pszSccAuxPath, [In] string pszSccLocalPath, [In] string pszProvider)
        {
            Trace.WriteLine("RegisterSccProject");

            if (pscp2Project != null)
                StatusTracker.UpdateProject(pscp2Project);
            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Called by projects registered with the source control portion of the environment
        ///     before they are closed.
        /// </summary>
        public int UnregisterSccProject([In] IVsSccProject2 pscp2Project)
        {
            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsSccManagerTooltip specific functions
        //--------------------------------------------------------------------------------

        #region IVsSccManagerTooltip interface functions

        /// <summary>
        ///     Called by solution explorer to provide tooltips for items. Returns a text describing the source control status of
        ///     the item.
        /// </summary>
        public int GetGlyphTipText([In] IVsHierarchy phierHierarchy, [In] uint itemidNode, out string pbstrTooltipText)
        {
            // Initialize output parameters
            pbstrTooltipText = "";

            var files = SccProvider.GetNodeFiles(phierHierarchy, itemidNode);
            if (files.Count == 0)
                return VSConstants.S_OK;

            // Return the glyph text based on the first file of node (the master file)
            var status = StatusTracker.GetFileStatus(files[0]);
            switch (status)
            {
                // my states
                case HGFileStatus.scsClean:
                    pbstrTooltipText = "Clean";
                    break;

                case HGFileStatus.scsModified:
                    pbstrTooltipText = "Modified";
                    break;

                case HGFileStatus.scsAdded:
                    pbstrTooltipText = "Added";
                    break;

                case HGFileStatus.scsRenamed:
                    pbstrTooltipText = "Renamed";
                    break;

                case HGFileStatus.scsRemoved:
                    pbstrTooltipText = "Removed";
                    break;

                case HGFileStatus.scsCopied:
                    pbstrTooltipText = "Copied";
                    break;

                case HGFileStatus.scsIgnored:
                    pbstrTooltipText = "Ignored";
                    break;

                case HGFileStatus.scsUncontrolled:
                    pbstrTooltipText = "Uncontrolled";
                    break;

                default:
                    pbstrTooltipText = string.Empty;
                    break;
            }

            if (pbstrTooltipText != string.Empty)
            {
                var root = HG.FindRootDirectory(files[0]);
                //string branchName = HGLib.HG.GetCurrentBranchName(root);
                var branchName = StatusTracker.GetCurrentBranchOf(root);

                pbstrTooltipText += " [" + branchName + "]";
            }

            return VSConstants.S_OK;
        }

        #endregion
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using HGLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHG
{
    /// <summary>
    ///     SccProvider Utillity Function
    /// </summary>
    partial class SccProvider
    {
        #region Source Control Utility Functions

        public void StoreSolution()
        {
            // store project and solution files to disk
            var solution = (IVsSolution) GetService(typeof(IVsSolution));
            solution.SaveSolutionElement((uint) __VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, null, 0);
        }

        /// <summary>
        ///     Returns a list of controllable projects in the solution
        /// </summary>
        public List<IVsSccProject2> GetLoadedControllableProjects()
        {
            var list = new List<IVsSccProject2>();
            // Hashtable mapHierarchies = new Hashtable();

            var sol = (IVsSolution) GetService(typeof(SVsSolution));
            var rguidEnumOnlyThisType = new Guid();
            IEnumHierarchies ppenum = null;
            ErrorHandler.ThrowOnFailure(sol.GetProjectEnum((uint) __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
                ref rguidEnumOnlyThisType, out ppenum));

            var rgelt = new IVsHierarchy[1];
            while (ppenum.Next(1, rgelt, out var pceltFetched) == VSConstants.S_OK &&
                   pceltFetched == 1)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var sccProject2 = rgelt[0] as IVsSccProject2;
                if (sccProject2 != null)
                    list.Add(sccProject2);
            }

            return list;
        }

        public Hashtable GetLoadedControllableProjectsEnum()
        {
            var mapHierarchies = new Hashtable();

            var sol = (IVsSolution) GetService(typeof(SVsSolution));
            var rguidEnumOnlyThisType = new Guid();
            IEnumHierarchies ppenum = null;
            ErrorHandler.ThrowOnFailure(sol.GetProjectEnum((uint) __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
                ref rguidEnumOnlyThisType, out ppenum));

            var rgelt = new IVsHierarchy[1];
            while (ppenum.Next(1, rgelt, out var pceltFetched) == VSConstants.S_OK &&
                   pceltFetched == 1)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var sccProject2 = rgelt[0] as IVsSccProject2;
                if (sccProject2 != null)
                    mapHierarchies[rgelt[0]] = true;
            }

            return mapHierarchies;
        }

        /// <summary>
        ///     Checks whether a solution exist
        /// </summary>
        /// <returns>True if a solution was created.</returns>
        private bool IsThereASolution()
        {
            return GetSolutionFileName() != null;
        }

        /// <summary>
        ///     get the file name of the selected item/document
        /// </summary>
        /// <param name="project"></param>
        /// <param name="itemId"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool GetItemFileName(IVsProject project, uint itemId, out string filename)
        {
            if (project.GetMkDocument(itemId, out var bstrMkDocument) == VSConstants.S_OK
                && !string.IsNullOrEmpty(bstrMkDocument))
            {
                filename = bstrMkDocument;
                return true;
            }

            filename = null;
            return false;
        }

        /// <summary>
        ///     find out if the selection list contains the soloution itself
        /// </summary>
        /// <param name="sel"></param>
        /// <returns>isSolutionSelected</returns>
        private bool GetSolutionSelected(IList<VSITEMSELECTION> sel)
        {
            foreach (var vsItemSel in sel)
                if (vsItemSel.pHier == null ||
                    vsItemSel.pHier as IVsSolution != null)
                    return true;
            return false;
        }

        /// <summary>
        ///     Gets the list of selected controllable project hierarchies
        /// </summary>
        /// <returns>True if a solution was created.</returns>
        private Hashtable GetSelectedHierarchies(ref IList<VSITEMSELECTION> sel, out bool solutionSelected)
        {
            // Initialize output arguments
            solutionSelected = false;

            var mapHierarchies = new Hashtable();
            foreach (var vsItemSel in sel)
            {
                if (vsItemSel.pHier == null ||
                    vsItemSel.pHier as IVsSolution != null)
                    solutionSelected = true;

                // See if the selected hierarchy implements the IVsSccProject2 interface
                // Exclude from selection projects like FTP web projects that don't support SCC
                var sccProject2 = vsItemSel.pHier as IVsSccProject2;
                if (sccProject2 != null)
                    mapHierarchies[vsItemSel.pHier] = true;
            }

            return mapHierarchies;
        }

        /// <summary>
        ///     Gets the list of directly selected VSITEMSELECTION objects
        /// </summary>
        /// <returns>A list of VSITEMSELECTION objects</returns>
        private IList<VSITEMSELECTION> GetSelectedNodes()
        {
            // Retrieve shell interface in order to get current selection
            var monitorSelection = GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            Debug.Assert(monitorSelection != null,
                "Could not get the IVsMonitorSelection object from the services exposed by this project");
            if (monitorSelection == null)
                throw new InvalidOperationException();

            var selectedNodes = new List<VSITEMSELECTION>();
            var hierarchyPtr = IntPtr.Zero;
            var selectionContainer = IntPtr.Zero;
            try
            {
                // Get the current project hierarchy, project item, and selection container for the current selection
                // If the selection spans multiple hierachies, hierarchyPtr is Zero
                uint itemid;
                IVsMultiItemSelect multiItemSelect = null;
                ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid,
                    out multiItemSelect, out selectionContainer));

                if (itemid != VSConstants.VSITEMID_SELECTION)
                {
                    // We only care if there are nodes selected in the tree
                    if (itemid != VSConstants.VSITEMID_NIL)
                        if (hierarchyPtr == IntPtr.Zero)
                        {
                            // Solution is selected
                            VSITEMSELECTION vsItemSelection;
                            vsItemSelection.pHier = null;
                            vsItemSelection.itemid = itemid;
                            selectedNodes.Add(vsItemSelection);
                        }
                        else
                        {
                            var hierarchy = (IVsHierarchy) Marshal.GetObjectForIUnknown(hierarchyPtr);
                            // Single item selection
                            VSITEMSELECTION vsItemSelection;
                            vsItemSelection.pHier = hierarchy;
                            vsItemSelection.itemid = itemid;
                            selectedNodes.Add(vsItemSelection);
                        }
                }
                else
                {
                    if (multiItemSelect != null)
                    {
                        // This is a multiple item selection.

                        //Get number of items selected and also determine if the items are located in more than one hierarchy
                        uint numberOfSelectedItems;
                        int isSingleHierarchyInt;
                        ErrorHandler.ThrowOnFailure(
                            multiItemSelect.GetSelectionInfo(out numberOfSelectedItems, out isSingleHierarchyInt));
                        var isSingleHierarchy = isSingleHierarchyInt != 0;

                        // Now loop all selected items and add them to the list 
                        Debug.Assert(numberOfSelectedItems > 0, "Bad number of selected itemd");
                        if (numberOfSelectedItems > 0)
                        {
                            var vsItemSelections = new VSITEMSELECTION[numberOfSelectedItems];
                            ErrorHandler.ThrowOnFailure(
                                multiItemSelect.GetSelectedItems(0, numberOfSelectedItems, vsItemSelections));
                            foreach (var vsItemSelection in vsItemSelections)
                                selectedNodes.Add(vsItemSelection);
                        }
                    }
                }
            }
            finally
            {
                if (hierarchyPtr != IntPtr.Zero)
                    Marshal.Release(hierarchyPtr);
                if (selectionContainer != IntPtr.Zero)
                    Marshal.Release(selectionContainer);
            }

            return selectedNodes;
        }

        /// <summary>
        ///     Returns a list of source controllable files in the selection (recursive)
        /// </summary>
        private IList<string> GetSelectedFilesInControlledProjects()
        {
            IList<VSITEMSELECTION> selectedNodes = null;
            return GetSelectedFilesInControlledProjects(out selectedNodes);
        }

        /// <summary>
        ///     Returns a list of source controllable files in the selection
        /// </summary>
        private List<string> GetSelectedFiles(out IList<VSITEMSELECTION> selectedNodes)
        {
            var sccFiles = new List<string>();

            selectedNodes = GetSelectedNodes();

            // now look in the rest of selection and accumulate scc files
            foreach (var vsItemSel in selectedNodes)
            {
                var pscp2 = vsItemSel.pHier as IVsSccProject2;
                if (pscp2 == null)
                {
                    // solution case
                    sccFiles.Add(GetSolutionFileName());
                }
                else
                {
                    var nodefilesrec = GetProjectFiles(pscp2, vsItemSel.itemid);
                    foreach (var file in nodefilesrec)
                        sccFiles.Add(file);
                }
            }

            return sccFiles;
        }

        /// <summary>
        ///     Returns a list of source controllable files in the selection (recursive)
        /// </summary>
        private List<string> GetSelectedFilesInControlledProjects(out IList<VSITEMSELECTION> selectedNodes)
        {
            var sccFiles = new List<string>();

            selectedNodes = GetSelectedNodes();
            var isSolutionSelected = false;
            var hash = GetSelectedHierarchies(ref selectedNodes, out isSolutionSelected);
            if (isSolutionSelected)
            {
                // Replace the selection with the root items of all controlled projects
                selectedNodes.Clear();
                var hashControllable = GetLoadedControllableProjectsEnum();
                foreach (IVsHierarchy pHier in hashControllable.Keys)
                    if (_sccService.IsProjectControlled(pHier))
                    {
                        VSITEMSELECTION vsItemSelection;
                        vsItemSelection.pHier = pHier;
                        vsItemSelection.itemid = VSConstants.VSITEMID_ROOT;
                        selectedNodes.Add(vsItemSelection);
                    }

                // Add the solution file to the list
                if (_sccService.IsProjectControlled(null))
                {
                    var solHier = (IVsHierarchy) GetService(typeof(SVsSolution));
                    VSITEMSELECTION vsItemSelection;
                    vsItemSelection.pHier = solHier;
                    vsItemSelection.itemid = VSConstants.VSITEMID_ROOT;
                    selectedNodes.Add(vsItemSelection);
                }
            }

            // now look in the rest of selection and accumulate scc files
            foreach (var vsItemSel in selectedNodes)
            {
                var pscp2 = vsItemSel.pHier as IVsSccProject2;
                if (pscp2 == null)
                {
                    // solution case
                    sccFiles.Add(GetSolutionFileName());
                }
                else
                {
                    IList<string> nodefilesrec = GetProjectFiles(pscp2, vsItemSel.itemid);
                    foreach (var file in nodefilesrec)
                        sccFiles.Add(file);
                }
            }

            return sccFiles;
        }

        /// <summary>
        ///     Returns a list of file names associated with the specified pathStr
        /// </summary>
        private static string[] GetFileNamesFromOleBuffer(CALPOLESTR[] pathStr, bool free)
        {
            var nEls = (int) pathStr[0].cElems;
            var files = new string[nEls];

            for (var i = 0; i < nEls; i++)
            {
                var pathIntPtr = Marshal.ReadIntPtr(pathStr[0].pElems, i * IntPtr.Size);
                files[i] = Marshal.PtrToStringUni(pathIntPtr);

                if (free)
                    Marshal.FreeCoTaskMem(pathIntPtr);
            }
            if (free && pathStr[0].pElems != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pathStr[0].pElems);

            return files;
        }

        /// <summary>
        ///     Returns a list of source controllable files associated with the specified node
        /// </summary>
        public static IList<string> GetNodeFiles(IVsHierarchy hier, uint itemid)
        {
            var pscp2 = hier as IVsSccProject2;
            return GetNodeFiles(pscp2, itemid);
        }

        /// <summary>
        ///     Returns a list of source controllable files associated with the specified node
        /// </summary>
        private static IList<string> GetNodeFiles(IVsSccProject2 pscp2, uint itemid)
        {
            // NOTE: the function returns only a list of files, containing both regular files and special files
            // If you want to hide the special files (similar with solution explorer), you may need to return 
            // the special files in a hastable (key=master_file, values=special_file_list)

            // Initialize output parameters
            IList<string> sccFiles = new List<string>();
            if (pscp2 != null)
            {
                var pathStr = new CALPOLESTR[1];
                var flags = new CADWORD[1];

                if (pscp2.GetSccFiles(itemid, pathStr, flags) == 0)
                {
                    //#4  BugFix : Visual Studio Crashing when clicking on Web Reference
                    // The previus MS sample code used 'Marshal.PtrToStringAuto' which caused
                    // a chrash of the studio (2010 only) in some conditions. This also could
                    // be the reason for some further bugs e.g. in commit, update tasks.
                    var files = GetFileNamesFromOleBuffer(pathStr, true);
                    for (var elemIndex = 0; elemIndex < pathStr[0].cElems; elemIndex++)
                    {
                        var path = files[elemIndex];
                        sccFiles.Add(path);

                        // See if there are special files
                        if (flags.Length > 0 && flags[0].cElems > 0)
                        {
                            var flag = Marshal.ReadInt32(flags[0].pElems, elemIndex);

                            if (flag != 0)
                            {
                                // We have special files
                                var specialFiles = new CALPOLESTR[1];
                                var specialFlags = new CADWORD[1];

                                pscp2.GetSccSpecialFiles(itemid, path, specialFiles, specialFlags);
                                var specialFileNames = GetFileNamesFromOleBuffer(specialFiles, true);
                                foreach (var f in specialFileNames)
                                    sccFiles.Add(f);
                            }
                        }
                    }
                }
            }

            return sccFiles;
        }

        /// <summary>
        ///     Refreshes the glyphs of the specified hierarchy nodes
        /// </summary>
        public void RefreshNodesGlyphs(IList<VSITEMSELECTION> selectedNodes)
        {
            foreach (var vsItemSel in selectedNodes)
            {
                var sccProject2 = vsItemSel.pHier as IVsSccProject2;
                if (vsItemSel.itemid == VSConstants.VSITEMID_ROOT)
                {
                    if (sccProject2 == null)
                    {
                        // Note: The solution's hierarchy does not implement IVsSccProject2, IVsSccProject interfaces
                        // It may be a pain to treat the solution as special case everywhere; a possible workaround is 
                        // to implement a solution-wrapper class, that will implement IVsSccProject2, IVsSccProject and
                        // IVsHierarhcy interfaces, and that could be used in provider's code wherever a solution is needed.
                        // This approach could unify the treatment of solution and projects in the provider's code.

                        // Until then, solution is treated as special case
                        var rgpszFullPaths = new string[1];
                        rgpszFullPaths[0] = GetSolutionFileName();
                        var rgsiGlyphs = new VsStateIcon[1];
                        var rgdwSccStatus = new uint[1];
                        _sccService.GetSccGlyph(1, rgpszFullPaths, rgsiGlyphs, rgdwSccStatus);

                        // Set the solution's glyph directly in the hierarchy
                        var solHier = (IVsHierarchy) GetService(typeof(SVsSolution));
                        solHier.SetProperty(VSConstants.VSITEMID_ROOT, (int) __VSHPROPID.VSHPROPID_StateIconIndex,
                            rgsiGlyphs[0]);
                    }
                    else
                    {
                        // Refresh all the glyphs in the project; the project will call back GetSccGlyphs() 
                        // with the files for each node that will need new glyph
                        sccProject2.SccGlyphChanged(0, null, null, null);
                       

                        var projectFiles = GetProjectFiles(sccProject2, vsItemSel.itemid);

                          bool changed = false;
                          foreach (var projectFile in projectFiles)
                          {
                              var status = _sccService.GetFileStatus(projectFile);
                              if (status ==HGFileStatus.scsAdded
                                  ||status==HGFileStatus.scsModified
                                  ||status==HGFileStatus.scsRenamed
                                  ||status==HGFileStatus.scsCopied)
                              {
                                  changed = true;
                                  break;
                              }
                          }

                          if (changed)
                          {
                              var rgsiGlyphs = new[] { (VsStateIcon)13 };
                              var rgdwSccStatus = new[]{(uint)HGFileStatus.scsModified};
                              var rguiAffectedNodes = new[] { vsItemSel.itemid };
                              sccProject2.SccGlyphChanged(1, rguiAffectedNodes, rgsiGlyphs, rgdwSccStatus);
                          }
                          else
                          {
                              // It may be easier/faster to simply refresh all the nodes in the project, 
                           // and let the project call back on GetSccGlyphs, but just for the sake of the demo, 
                           // let's refresh ourselves only one node at a time
                              var sccFiles = GetNodeFiles(sccProject2, vsItemSel.itemid);

                              // We'll use for the node glyph just the Master file's status (ignoring special files of the node)
                              if (sccFiles.Count > 0)
                              {
                                  var rgpszFullPaths = new string[1];
                                  rgpszFullPaths[0] = sccFiles[0];
                                  var rgsiGlyphs = new VsStateIcon[1];
                                  var rgdwSccStatus = new uint[1];
                                  _sccService.GetSccGlyph(1, rgpszFullPaths, rgsiGlyphs, rgdwSccStatus);

                                  var rguiAffectedNodes = new uint[1];
                                  rguiAffectedNodes[0] = vsItemSel.itemid;
                                  sccProject2.SccGlyphChanged(1, rguiAffectedNodes, rgsiGlyphs, rgdwSccStatus);
                              }
                          }
                    }
                }
                /*else
                {
                    // It may be easier/faster to simply refresh all the nodes in the project, 
                    // and let the project call back on GetSccGlyphs, but just for the sake of the demo, 
                    // let's refresh ourselves only one node at a time
                    var sccFiles = GetNodeFiles(sccProject2, vsItemSel.itemid);

                    // We'll use for the node glyph just the Master file's status (ignoring special files of the node)
                    if (sccFiles.Count > 0)
                    {
                        var rgpszFullPaths = new string[1];
                        rgpszFullPaths[0] = sccFiles[0];
                        var rgsiGlyphs = new VsStateIcon[1];
                        var rgdwSccStatus = new uint[1];
                        _sccService.GetSccGlyph(1, rgpszFullPaths, rgsiGlyphs, rgdwSccStatus);

                        var rguiAffectedNodes = new uint[1];
                        rguiAffectedNodes[0] = vsItemSel.itemid;
                        sccProject2.SccGlyphChanged(1, rguiAffectedNodes, rgsiGlyphs, rgdwSccStatus);
                    }
                }*/
            }
        }


        /// <summary>
        ///     Returns the filename of the solution
        /// </summary>
        public string GetSolutionFileName()
        {
            var sol = (IVsSolution) GetService(typeof(SVsSolution));
            string solutionDirectory, solutionFile, solutionUserOptions;
            if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) ==
                VSConstants.S_OK)
                return solutionFile;
            return null;
        }

        /// <summary>
        ///     Returns the root directory of the solution or first project
        /// </summary>
        public string GetRootDirectory()
        {
            var root = string.Empty;

            var selectedNodes = GetSelectedNodes();
            if (selectedNodes.Count > 0)
            {
                var pscp = selectedNodes[0].pHier as IVsProject;
                if (pscp != null)
                {
                    string filename;
                    if (GetItemFileName(pscp, selectedNodes[0].itemid, out filename))
                        root = HG.FindRootDirectory(filename);
                }
            }

            if (root == string.Empty)
            {
                root = HG.FindRootDirectory(GetSolutionFileName());
                if (root == string.Empty)
                    if (LastSeenProjectDir != null)
                        root = HG.FindRootDirectory(LastSeenProjectDir);
            }

            return root;
        }

        /// <summary>
        ///     Returns the filename of the specified controllable project
        /// </summary>
        public static string GetProjectFileName(SccProvider provider, IVsSccProject2 pscp2Project)
        {
            // Note: Solution folders return currently a name like "NewFolder1{1DBFFC2F-6E27-465A-A16A-1AECEA0B2F7E}.storage"
            // Your provider may consider returning the solution file as the project name for the solution, if it has to persist some properties in the "project file"
            // UNDONE: What to return for web projects? They return a folder name, not a filename! Consider returning a pseudo-project filename instead of folder.

            var hierProject = (IVsHierarchy) pscp2Project;
            var project = (IVsProject) pscp2Project;

            // Attempt to get first the filename controlled by the root node 
            var sccFiles = GetNodeFiles(pscp2Project, VSConstants.VSITEMID_ROOT);
            if (sccFiles.Count > 0 && sccFiles[0] != null && sccFiles[0].Length > 0)
                return sccFiles[0];

            // If that failed, attempt to get a name from the IVsProject interface
            string bstrMKDocument;
            if (project.GetMkDocument(VSConstants.VSITEMID_ROOT, out bstrMKDocument) == VSConstants.S_OK &&
                bstrMKDocument != null && bstrMKDocument.Length > 0)
                return bstrMKDocument;

            // If that failes, attempt to get the filename from the solution
            var sol = (IVsSolution) provider.GetService(typeof(SVsSolution));
            string uniqueName;
            if (sol.GetUniqueNameOfProject(hierProject, out uniqueName) == VSConstants.S_OK &&
                uniqueName != null && uniqueName.Length > 0)
            {
                // uniqueName may be a full-path or may be relative to the solution's folder
                if (uniqueName.Length > 2 && uniqueName[1] == ':')
                    return uniqueName;

                // try to get the solution's folder and relativize the project name to it
                string solutionDirectory, solutionFile, solutionUserOptions;
                if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) ==
                    VSConstants.S_OK)
                {
                    uniqueName = solutionDirectory + "\\" + uniqueName;

                    // UNDONE: eliminate possible "..\\.." from path
                    return uniqueName;
                }
            }

            // If that failed, attempt to get the project name from 
            string bstrName;
            if (hierProject.GetCanonicalName(VSConstants.VSITEMID_ROOT, out bstrName) == VSConstants.S_OK)
                return bstrName;

            // if everything we tried fail, return null string
            return null;
        }

        private static void DebugWalkingNode(IVsHierarchy pHier, uint itemid)
        {
            object property = null;
            if (pHier != null && pHier.GetProperty(itemid, (int) __VSHPROPID.VSHPROPID_Name, out property) ==
                VSConstants.S_OK)
                Trace.WriteLine(string.Format(CultureInfo.CurrentUICulture, "Walking hierarchy node: {0}",
                    (string) property));
        }

        /// <summary>
        ///     Gets the list of ItemIDs that are nodes in the specified project
        /// </summary>
        private static IList<uint> GetProjectItems(IVsHierarchy pHier)
        {
            // Start with the project root and walk all expandable nodes in the project
            return GetProjectItems(pHier, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        ///     Gets the list of ItemIDs that are nodes in the specified project, starting with the specified item
        /// </summary>
        private static IList<uint> GetProjectItems(IVsHierarchy pHier, uint startItemid)
        {
            var projectNodes = new List<uint>();

            if (pHier == null)
                return projectNodes;

            // The method does a breadth-first traversal of the project's hierarchy tree
            var nodesToWalk = new Queue<uint>();
            nodesToWalk.Enqueue(startItemid);

            while (nodesToWalk.Count > 0)
            {
                var node = nodesToWalk.Dequeue();
                projectNodes.Add(node);

                DebugWalkingNode(pHier, node);

                object property = null;
                if (pHier.GetProperty(node, (int) __VSHPROPID.VSHPROPID_FirstChild, out property) == VSConstants.S_OK)
                {
                    var childnode = (uint) (int) property;
                    if (childnode == VSConstants.VSITEMID_NIL)
                        continue;

                    DebugWalkingNode(pHier, childnode);

                    if (pHier.GetProperty(childnode, (int) __VSHPROPID.VSHPROPID_Expandable, out property) ==
                        VSConstants.S_OK && IsTrue(property) ||
                        pHier.GetProperty(childnode, (int) __VSHPROPID2.VSHPROPID_Container, out property) ==
                        VSConstants.S_OK && IsTrue(property))
                        nodesToWalk.Enqueue(childnode);
                    else
                        projectNodes.Add(childnode);

                    while (pHier.GetProperty(childnode, (int) __VSHPROPID.VSHPROPID_NextSibling, out property) ==
                           VSConstants.S_OK)
                    {
                        childnode = (uint) (int) property;
                        if (childnode == VSConstants.VSITEMID_NIL)
                            break;

                        DebugWalkingNode(pHier, childnode);

                        if (pHier.GetProperty(childnode, (int) __VSHPROPID.VSHPROPID_Expandable, out property) ==
                            VSConstants.S_OK && IsTrue(property) ||
                            pHier.GetProperty(childnode, (int) __VSHPROPID2.VSHPROPID_Container, out property) ==
                            VSConstants.S_OK && IsTrue(property))
                            nodesToWalk.Enqueue(childnode);
                        else
                            projectNodes.Add(childnode);
                    }
                }
            }

            return projectNodes;
        }

        private static bool IsTrue(object property)
        {
            if (property is bool b)
                return b;

            if (property is int i)
                return (i != 0);

            return false;
        }

        /// <summary>
        ///     Gets the list of source controllable files in the specified project
        /// </summary>
        public static IList<string> GetProjectFiles(IVsSccProject2 pscp2Project)
        {
            return GetProjectFiles(pscp2Project, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        ///     get current single selected filename
        /// </summary>
        /// <returns></returns>
        public string GetSingleSelectedFileName()
        {
            var filename = string.Empty;
            var selectedNodes = GetSelectedNodes();
            if (selectedNodes.Count == 1)
            {
                var pscp = selectedNodes[0].pHier as IVsProject;
                if (pscp != null)
                {
                    GetItemFileName(pscp, selectedNodes[0].itemid, out filename);
                }
                else
                {
                    var dte = (_DTE) GetService(typeof(SDTE));

                    if (dte != null && dte.ActiveDocument != null)
                        filename = dte.ActiveDocument.FullName;
                    else
                        filename = GetSolutionFileName();
                }
            }

            return filename;
        }

        // ------------------------------------------------------------------------
        // find selected file state mask - for quick menu flags detection
        // ------------------------------------------------------------------------
        public bool FindSelectedFirstMask(bool includeChildItems, long stateMask)
        {
            var selectedNodes = GetSelectedNodes();
            foreach (var node in selectedNodes)
            {
                var pscp = node.pHier as IVsProject;

                var filename = string.Empty;
                if (pscp != null)
                    GetItemFileName(pscp, node.itemid, out filename);
                else
                    filename = GetSolutionFileName();
                if (filename != string.Empty)
                {
                    var status = _sccService.GetFileStatus(filename);
                    if ((stateMask & (long) status) != 0)
                        return true;
                }
            }

            if (includeChildItems)
                foreach (var node in selectedNodes)
                {
                    var pscp = node.pHier as IVsProject;

                    if (pscp != null)
                        if (FindProjectSelectedFileStateMask(node.pHier, node.itemid, stateMask))
                            return true;
                }
            return false;
        }

        // ------------------------------------------------------------------------
        // find selected file state mask 
        // ------------------------------------------------------------------------
        private bool FindProjectSelectedFileStateMask(IVsHierarchy pHier, uint startItemid, long stateMask)
        {
            if (pHier == null)
                return false;

            var pscp = pHier as IVsProject;

            if (pscp == null)
                return false;

            // The method does a breadth-first traversal of the project's hierarchy tree
            var nodesToWalk = new Queue<uint>();
            nodesToWalk.Enqueue(startItemid);

            while (nodesToWalk.Count > 0)
            {
                var node = nodesToWalk.Dequeue();
                if (CompareFileStateMask(pscp, node, stateMask))
                    return true;

                DebugWalkingNode(pHier, node);

                object property = null;
                if (pHier.GetProperty(node, (int) __VSHPROPID.VSHPROPID_FirstChild, out property) == VSConstants.S_OK)
                {
                    var childnode = (uint) (int) property;
                    if (childnode == VSConstants.VSITEMID_NIL)
                        continue;

                    DebugWalkingNode(pHier, childnode);

                    if (pHier.GetProperty(childnode, (int) __VSHPROPID.VSHPROPID_Expandable, out property) ==
                        VSConstants.S_OK && IsTrue(property) ||
                        pHier.GetProperty(childnode, (int) __VSHPROPID2.VSHPROPID_Container, out property) ==
                        VSConstants.S_OK && IsTrue(property))
                    {
                        nodesToWalk.Enqueue(childnode);
                    }
                    else
                    {
                        if (CompareFileStateMask(pscp, childnode, stateMask))
                            return true;
                    }

                    while (pHier.GetProperty(childnode, (int) __VSHPROPID.VSHPROPID_NextSibling, out property) ==
                           VSConstants.S_OK)
                    {
                        childnode = (uint) (int) property;
                        if (childnode == VSConstants.VSITEMID_NIL)
                            break;

                        DebugWalkingNode(pHier, childnode);

                        if (pHier.GetProperty(childnode, (int) __VSHPROPID.VSHPROPID_Expandable, out property) ==
                            VSConstants.S_OK && IsTrue(property) ||
                            pHier.GetProperty(childnode, (int) __VSHPROPID2.VSHPROPID_Container, out property) ==
                            VSConstants.S_OK && IsTrue(property))
                        {
                            nodesToWalk.Enqueue(childnode);
                        }
                        else
                        {
                            if (CompareFileStateMask(pscp, childnode, stateMask))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool CompareFileStateMask(IVsProject pscp, uint itemid, long stateMask)
        {
            var childFilename = string.Empty;
            if (GetItemFileName(pscp, itemid, out childFilename))
            {
                var status = _sccService.GetFileStatus(childFilename);
                if ((stateMask & (long) status) != 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     get current selected filenames
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectedFileNameArray(bool includeChildItems)
        {
            var array = new List<string>();

            var selectedNodes = GetSelectedNodes();
            foreach (var node in selectedNodes)
            {
                var pscp = node.pHier as IVsProject;

                if (includeChildItems)
                    if (pscp != null)
                    {
                        var childItems = GetProjectItems(node.pHier, node.itemid);
                        foreach (var itemid in childItems)
                        {
                            var childFilename = string.Empty;
                            if (GetItemFileName(pscp, itemid, out childFilename))
                                array.Add(childFilename);
                        }
                    }

                var filename = string.Empty;
                if (pscp != null)
                {
                    if (!includeChildItems)
                        GetItemFileName(pscp, node.itemid, out filename);
                }
                else
                {
                    filename = GetSolutionFileName();
                }
                if (filename != string.Empty)
                    array.Add(filename);
            }

            return array;
        }

        public List<string> GetItemFileNameArray()
        {
            var array = new List<string>();

            var selectedNodes = GetSelectedNodes();
            foreach (var node in selectedNodes)
            {
                var filename = string.Empty;
                var pscp = node.pHier as IVsProject;
                if (pscp != null)
                    GetItemFileName(pscp, node.itemid, out filename);
                else
                    filename = GetSolutionFileName();
                array.Add(filename);
            }

            return array;
        }

        /// <summary>
        ///     Gets the list of source controllable files in the specified project
        /// </summary>
        public static List<string> GetProjectFiles(IVsSccProject2 pscp2Project, uint startItemId)
        {
            var projectFiles = new List<string>();
            var hierProject = (IVsHierarchy) pscp2Project;
            var projectItems = GetProjectItems(hierProject, startItemId);

            foreach (var itemid in projectItems)
            {
                var sccFiles = GetNodeFiles(pscp2Project, itemid);
                foreach (var file in sccFiles)
                    projectFiles.Add(file);
            }

            return projectFiles;
        }

        /// <summary>
        ///     Checks whether the provider is invoked in command line mode
        /// </summary>
        public bool InCommandLineMode()
        {
            var shell = (IVsShell) GetService(typeof(SVsShell));
            object pvar;
            if (shell.GetProperty((int) __VSSPROPID.VSSPROPID_IsInCommandLineMode, out pvar) == VSConstants.S_OK &&
                (bool) pvar)
                return true;

            return false;
        }

        #endregion

        [DllImport("user32.dll")]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        /// <summary>
        ///     set current branch in application window title
        /// </summary>
        /// <returns></returns>
        public void UpdateMainWindowTitle(string branch)
        {
            var dte = (_DTE) GetService(typeof(SDTE));

            if (dte != null && dte.MainWindow != null)
            {
                var caption = dte.MainWindow.Caption;

                // strip prev branch name
                var param = caption.Split('-');
                if (param.Length > 1)
                {
                    var index = param[0].IndexOf('(');
                    if (index > 0)
                        param[0] = param[0].Substring(0, index);

                    // add new branch name
                    var newCaption = string.Empty;

                    foreach (var s in param)
                        if (newCaption == string.Empty && branch != string.Empty)
                            newCaption = s + "(" + branch + ") ";
                        else
                            newCaption += s;

                    if (caption != newCaption)
                    {
                        var hWnd = (IntPtr) dte.MainWindow.HWnd;
                        SetWindowText(hWnd, newCaption);
                    }
                }
            }
        }
    }
}
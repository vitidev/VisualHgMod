using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

//using IServiceProvider = System.IServiceProvider;
//using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace VisualHG
{
    /// <summary>
    ///     Summary description for SccProviderToolWindow.
    /// </summary>
    [Guid(GuidList.HgPendingChangesToolWindowGuid)]
    public class HGPendingChangesToolWindow : ToolWindowPane
    {
        private HGPendingChangesToolWindowControl control;

        public HGPendingChangesToolWindow() : base(null)
        {
            // set the window title
            Caption = Resources.ResourceManager.GetString("HGPendingChangesToolWindowCaption");

            // set the CommandID for the window ToolBar
            //this.ToolBar = new CommandID(GuidList.guidSccProviderCmdSet, CommandId.imnuToolWindowToolbarMenu);

            // set the icon for the frame
            BitmapResourceID = CommandId.IbmpToolWindowsImages; // bitmap strip resource ID
            BitmapIndex = CommandId.IconSccProviderToolWindow; // index in the bitmap strip

            control = new HGPendingChangesToolWindowControl();

            // update pending list
            var service = (SccProviderService) SccProvider.GetServiceEx(typeof(SccProviderService));
            if (service != null)
                UpdatePendingList(service.StatusTracker);
        }

        // route update pending changes call
        public void UpdatePendingList(HgStatusTracker tracker)
        {
            control.UpdatePendingList(tracker);
        }

        // ------------------------------------------------------------------------
        // returns the window handle
        // ------------------------------------------------------------------------
        public override IWin32Window Window => control;

        /// <include file='doc\WindowPane.uex' path='docs/doc[@for="WindowPane.Dispose1"]' />
        /// <devdoc>
        ///     Called when this tool window pane is being disposed.
        /// </devdoc>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (control != null)
                {
                    try
                    {
                        if (control is IDisposable)
                            control.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.Fail(string.Format("Failed to dispose {0} controls.\n{1}", GetType().FullName,
                            e.Message));
                    }
                    control = null;
                }

                var windowFrame = (IVsWindowFrame) Frame;
                if (windowFrame != null)
                    windowFrame.CloseFrame((uint) __FRAMECLOSE.FRAMECLOSE_SaveIfDirty);
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///     This function is only used to "do something noticeable" when the toolbar button is clicked.
        ///     It is called from the package.
        ///     A typical tool window may not need this function.
        ///     The current behavior change the background color of the control
        /// </summary>
        public void ToolWindowToolbarCommand()
        {
            if (control.BackColor == Color.Coral)
                control.BackColor = Color.White;
            else
                control.BackColor = Color.Coral;
        }
    }
}
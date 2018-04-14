using System;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace VisualHG
{
    public enum VisualHgToolWindow
    {
        None = 0,
        PendingChanges
    }

    // Register the VisualHG tool window visible only when the provider is active
    [MsVsShell.ProvideToolWindowAttribute(typeof(HGPendingChangesToolWindow))]
    [MsVsShell.ProvideToolWindowVisibilityAttribute(typeof(HGPendingChangesToolWindow), GuidList.ProviderGuid)]
    public partial class SccProvider
    {
        //public ToolWindowPane FindToolWindow(Type toolWindowType, int id, bool create);
        public void ShowToolWindow(VisualHgToolWindow window)
        {
            ShowToolWindow(window, 0, true);
        }

        private Type GetPaneType(VisualHgToolWindow toolWindow)
        {
            switch (toolWindow)
            {
                case VisualHgToolWindow.PendingChanges:
                    return typeof(HGPendingChangesToolWindow);
                default:
                    throw new ArgumentOutOfRangeException(nameof(toolWindow));
            }
        }

        public MsVsShell.ToolWindowPane FindToolWindow(VisualHgToolWindow toolWindow)
        {
            var pane = FindToolWindow(GetPaneType(toolWindow), 0, false);
            return pane;
        }

        public void ShowToolWindow(VisualHgToolWindow toolWindow, int id, bool create)
        {
            try
            {
                var pane = FindToolWindow(GetPaneType(toolWindow), id, create);

                var frame = pane.Frame as IVsWindowFrame;
                if (frame == null)
                    throw new InvalidOperationException("FindToolWindow failed");
                // Bring the tool window to the front and give it focus
                ErrorHandler.ThrowOnFailure(frame.Show());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, @"Error occured");
            }
        }
    }
}
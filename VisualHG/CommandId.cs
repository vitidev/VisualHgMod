namespace VisualHG
{
    /// <summary>
    ///     This class is used to expose the list of the IDs of the commands implemented
    ///     by the client package. This list of IDs must match the set of IDs defined inside the
    ///     VSCT file.
    /// </summary>
    public static class CommandId
    {
        // Define the list a set of public static members.

        // Define the list of menus (these include toolbars)
        public const int ImnuToolWindowToolbarMenu = 0x204;

        public const int IcmdHgCommitRoot = 0x100;
        public const int IcmdHgStatus = 0x101;
        public const int IcmdHgHistoryRoot = 0x102;
        public const int IcmdViewToolWindow = 0x103;
        public const int IcmdToolWindowToolbarCommand = 0x104;
        public const int IcmdHgSynchronize = 0x105;
        public const int IcmdHgUpdateToRevision = 0x106;
        public const int IcmdHgDiff = 0x107;
        public const int IcmdHgRevert = 0x108;
        public const int IcmdHgAnnotate = 0x109;
        public const int IcmdHgCommitSelected = 0x110;
        public const int IcmdHgHistorySelected = 0x111;
        public const int IcmdHgAddSelected = 0x112;
        public const int IcmdHgDiffExt = 0x113;

        // Define the list of icons (use decimal numbers here, to match the resource IDs)
        public const int IiconProductIcon = 400;

        // Define the list of bitmaps (use decimal numbers here, to match the resource IDs)
        public const int IbmpToolbarMenusImages = 500;

        public const int IbmpToolWindowsImages = 501;

        // Glyph indexes in the bitmap used for tolwindows (ibmpToolWindowsImages)
        public const int IconSccProviderToolWindow = 0;
    }
}
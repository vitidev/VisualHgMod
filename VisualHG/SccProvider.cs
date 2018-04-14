using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = System.IServiceProvider;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace VisualHG
{
    /////////////////////////////////////////////////////////////////////////////
    // SccProvider
    [MsVsShell.DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\15.0Exp")]
    // Register the package to have information displayed in Help/About dialog box
    [MsVsShell.InstalledProductRegistration("#100", "#101", "1.0", IconResourceID =
        CommandId.IiconProductIcon)]
    // Declare that resources for the package are to be found in the managed assembly resources, and not in a satellite dll
    [MsVsShell.PackageRegistration(UseManagedResourcesOnly = true)]
    // Register the resource ID of the CTMENU section (generated from compiling the VSCT file), so the IDE will know how to merge this package's menus with the rest of the IDE when "devenv /setup" is run
    // The menu resource ID needs to match the ResourceName number defined in the csproj project file in the VSCTCompile section
    // Everytime the version number changes VS will automatically update the menus on startup; if the version doesn't change, you will need to run manually "devenv /setup /rootsuffix:Exp" to see VSCT changes reflected in IDE
    [MsVsShell.ProvideMenuResource(1000, 1)]

    // Register the VisualHG options page visible as Tools/Options/SourceControl/VisualHG when the provider is active
    [MsVsShell.ProvideOptionPage(typeof(SccProviderOptions), "Source Control", "VisualHG", 106, 107, false)]
    [ProvideToolsOptionsPageVisibility("Source Control", "VisualHG", GuidList.ProviderGuid)]

    // Register the source control provider's service (implementing IVsScciProvider interface)
    [MsVsShell.ProvideService(typeof(SccProviderService), ServiceName = "VisualHG")]
    // Register the source control provider to be visible in Tools/Options/SourceControl/Plugin dropdown selector
    [ProvideSourceControlProvider("VisualHG", "#100")]
    // Pre-load the package when the command UI context is asserted (the provider will be automatically loaded after restarting the shell if it was active last time the shell was shutdown)
    [MsVsShell.ProvideAutoLoad(GuidList.ProviderGuid)]
    // Register the key used for persisting solution properties, so the IDE will know to load the source control package when opening a controlled solution containing properties written by this package
    [ProvideSolutionProps(StrSolutionPersistanceKey)]
    [MsVsShell.ProvideLoadKey(PLK.MinEdition, PLK.PackageVersion, PLK.PakageName, PLK.CompanyName, 104)]
    // Declare the package guid
    [Guid(PLK.PackageGuid)]
    public sealed partial class SccProvider : MsVsShell.Package, IOleCommandTarget
    {
        // The service provider implemented by the package
        private SccProviderService _sccService;

        // The name of this provider (to be written in solution and project files)
        // As a best practice, to be sure the provider has an unique name, a guid like the provider guid can be used as a part of the name
        private const string StrProviderName = "VisualHG:" + PLK.PackageGuid;

        // The name of the solution section used to persist provider options (should be unique)
        private const string StrSolutionPersistanceKey = "VisualHGProperties";

        // The name of the section in the solution user options file used to persist user-specific options (should be unique, shorter than 31 characters and without dots)
        private const string StrSolutionUserOptionsKey = "VisualHGSolution";

        // The names of the properties stored by the provider in the solution file
        private const string StrSolutionControlledProperty = "SolutionIsControlled";

        private const string StrSolutionBindingsProperty = "SolutionBindings";

        private readonly SccOnIdleEvent _onIdleEvent = new SccOnIdleEvent();

        public string LastSeenProjectDir = string.Empty;

        public SccProvider()
        {
            Provider = this;
            Trace.WriteLine(string.Format(CultureInfo.CurrentUICulture, "Entering constructor for: {0}", ToString()));
        }

        private void PromptSolutionNotControlled()
        {
            var solutionName = GetSolutionFileName();
            MessageBox.Show(@"Solution is not under Mercurial version contol\n\n" + solutionName, @"VisualHG",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /////////////////////////////////////////////////////////////////////////////
        // SccProvider Package Implementation

        #region Package Members

        public static object GetServiceEx(Type serviceType)
        {
            return Provider?.GetService(serviceType);
        }

        public new object GetService(Type serviceType)
        {
            return base.GetService(serviceType);
        }

        public static SccProvider Provider { get; private set; }

        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentUICulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            // Proffer the source control service implemented by the provider
            _sccService = new SccProviderService(this);
            ((IServiceContainer) this).AddService(typeof(SccProviderService), _sccService, true);
            ((IServiceContainer) this).AddService(typeof(IServiceProvider), this, true);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            InitVSCTMenuCommandHandler();

            // Register the provider with the source control manager
            // If the package is to become active, this will also callback on OnActiveStateChange and the menu commands will be enabled
            var rscp = (IVsRegisterScciProvider) GetService(typeof(IVsRegisterScciProvider));
            rscp.RegisterSourceControlProvider(GuidList.GuidSccProvider);

            _onIdleEvent.RegisterForIdleTimeCallbacks(
                GetGlobalService(typeof(SOleComponentManager)) as IOleComponentManager);
            _onIdleEvent.OnIdleEvent += _sccService.UpdateDirtyNodesGlyphs;

            //ShowToolWindow(VisualHGToolWindow.PendingChanges);
        }

        protected override void Dispose(bool disposing)
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentUICulture, "Entering Dispose() of: {0}", ToString()));

            _onIdleEvent.OnIdleEvent -= _sccService.UpdateDirtyNodesGlyphs;
            _onIdleEvent.UnRegisterForIdleTimeCallbacks();

            Provider = null;

            _sccService.Dispose();
            base.Dispose(disposing);
        }

        // Returns the name of the source control provider
        public string ProviderName => StrProviderName;

        #endregion
    }
}
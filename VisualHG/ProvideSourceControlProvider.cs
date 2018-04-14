using System;
using System.Globalization;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace VisualHG
{
    /// <summary>
    ///     This attribute registers the source control provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProvideSourceControlProvider : MsVsShell.RegistrationAttribute
    {
        /// <summary>
        /// </summary>
        public ProvideSourceControlProvider(string regName, string uiName)
        {
            RegName = regName;
            UiName = uiName;
        }

        /// <summary>
        ///     Get the friendly name of the provider (written in registry)
        /// </summary>
        public string RegName { get; }

        /// <summary>
        ///     Get the unique guid identifying the provider
        /// </summary>
        public Guid RegGuid => GuidList.GuidSccProvider;

        /// <summary>
        ///     Get the UI name of the provider (string resource ID)
        /// </summary>
        public string UiName { get; }

        /// <summary>
        ///     Get the package containing the UI name of the provider
        /// </summary>
        public Guid UiNamePkg => GuidList.GuidSccProviderPkg;

        /// <summary>
        ///     Get the guid of the provider's service
        /// </summary>
        public Guid SccProviderService => GuidList.GuidSccProviderService;

        /// <summary>
        ///     Called to register this attribute with the given context.  The context
        ///     contains the location where the registration inforomation should be placed.
        ///     It also contains other information such as the type being registered and path information.
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            // Write to the context's log what we are about to do
            context.Log.WriteLine(string.Format(CultureInfo.CurrentCulture,
                "VisualHG Mercurial Support for Visual Studio:\t\t{0}\n", RegName));

            // Declare the source control provider, its name, the provider's service 
            // and aditionally the packages implementing this provider
            using (var sccProviders = context.CreateKey("SourceControlProviders"))
            {
                using (var sccProviderKey = sccProviders.CreateSubkey(RegGuid.ToString("B")))
                {
                    sccProviderKey.SetValue("", RegName);
                    sccProviderKey.SetValue("Service", SccProviderService.ToString("B"));

                    using (var sccProviderNameKey = sccProviderKey.CreateSubkey("Name"))
                    {
                        sccProviderNameKey.SetValue("", UiName);
                        sccProviderNameKey.SetValue("Package", UiNamePkg.ToString("B"));

                        sccProviderNameKey.Close();
                    }

                    // Additionally, you can create a "Packages" subkey where you can enumerate the dll
                    // that are used by the source control provider, something like "Package1"="SccProvider.dll"
                    // but this is not a requirement.
                    sccProviderKey.Close();
                }

                sccProviders.Close();
            }
        }

        /// <summary>
        ///     Unregister the source control provider
        /// </summary>
        /// <param name="context"></param>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey("SourceControlProviders\\" + GuidList.GuidSccProviderPkg.ToString("B"));
        }
    }
}
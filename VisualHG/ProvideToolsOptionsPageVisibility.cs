using System;
using System.Globalization;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace VisualHG
{
    /// <summary>
    ///     This attribute registers the visibility of a Tools/Options property page.
    ///     While Microsoft.VisualStudio.Shell allow registering a tools options page
    ///     using the ProvideOptionPageAttribute attribute, currently there is no better way
    ///     of declaring the options page visibility, so a custom attribute needs to be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProvideToolsOptionsPageVisibility : MsVsShell.RegistrationAttribute
    {
        /// <summary>
        /// </summary>
        public ProvideToolsOptionsPageVisibility(string categoryName, string pageName, string commandUiGuid)
        {
            CategoryName = categoryName;
            PageName = pageName;
            CommandUiGuid = new Guid(commandUiGuid);
        }

        /// <summary>
        ///     The programmatic name for this category (non localized).
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        ///     The programmatic name for this page (non localized).
        /// </summary>
        public string PageName { get; }

        /// <summary>
        ///     Get the command UI guid controlling the visibility of the page.
        /// </summary>
        public Guid CommandUiGuid { get; }

        private string RegistryPath => string.Format(CultureInfo.InvariantCulture,
            "ToolsOptionsPages\\{0}\\{1}\\VisibilityCmdUIContexts", CategoryName, PageName);

        /// <summary>
        ///     Called to register this attribute with the given context.  The context
        ///     contains the location where the registration inforomation should be placed.
        ///     It also contains other information such as the type being registered and path information.
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            // Write to the context's log what we are about to do
            context.Log.WriteLine(string.Format(CultureInfo.CurrentCulture, "Opt.Page Visibility:\t{0}\\{1}, {2}\n",
                CategoryName, PageName, CommandUiGuid.ToString("B")));

            // Create the visibility key.
            using (var childKey = context.CreateKey(RegistryPath))
            {
                // Set the value for the command UI guid.
                childKey.SetValue(CommandUiGuid.ToString("B"), 1);
            }
        }

        /// <summary>
        ///     Unregister this visibility entry.
        /// </summary>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveValue(RegistryPath, CommandUiGuid.ToString("B"));
        }
    }
}
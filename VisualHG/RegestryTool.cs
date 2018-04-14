using System;
using System.ComponentModel;
using Microsoft.Win32;

namespace VisualHG
{
    /// <summary>
    ///     registry read/write helper tool
    /// </summary>
    public class RegestryTool
    {
        /// <summary>
        // Opens the requested key or returns null
        /// </summary>
        /// <param name="subKey"></param>
        /// <returns></returns>
        private static RegistryKey OpenRegKey(string subKey)
        {
            if (string.IsNullOrEmpty(subKey))
                throw new ArgumentNullException(nameof(subKey));

            return Registry.CurrentUser.OpenSubKey("SOFTWARE\\VisualHG\\" + subKey,
                RegistryKeyPermissionCheck.ReadSubTree);
        }

        /// <summary>
        ///     Opens or creates the requested key
        /// </summary>
        private static RegistryKey OpenCreateKey(string subKey)
        {
            if (string.IsNullOrEmpty(subKey))
                throw new ArgumentNullException(nameof(subKey));

            return Registry.CurrentUser.CreateSubKey("SOFTWARE\\VisualHG\\" + subKey);
        }

        /// <summary>
        ///     load object properties from registry
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="o"></param>
        public static void LoadProperties(string keyName, object o)
        {
            using (var reg = OpenRegKey(keyName))
            {
                if (reg != null)
                {
                    var pdc = TypeDescriptor.GetProperties(o);
                    foreach (PropertyDescriptor pd in pdc)
                    {
                        var value = reg.GetValue(pd.Name, null) as string;

                        if (value != null)
                            try
                            {
                                pd.SetValue(o, pd.Converter.ConvertFromInvariantString(value));
                            }
                            catch
                            {
                            }
                    }
                }
            }
        }

        /// <summary>
        ///     store object properties to registry
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="o"></param>
        public static void StoreProperties(string keyName, object o)
        {
            using (var reg = OpenCreateKey(keyName))
            {
                var pdc = TypeDescriptor.GetProperties(o);
                foreach (PropertyDescriptor pd in pdc)
                {
                    var value = pd.GetValue(o);
                    reg.SetValue(pd.Name, pd.Converter.ConvertToInvariantString(value));
                }
            }
        }
    }
}
using System;
using System.Text;

namespace VisualHG
{
    /// <summary>
    /// visualhg configuration properties container
    /// </summary>
    public class Configuration
    {
        public bool   _autoAddFiles = true;
        public bool   _autoActivatePlugin = true;
        public bool   _enableContextSearch = true;
        public bool   _observeOutOfStudioFileChanges = true;
        public bool   _useVisualStudioDiff = false;

        public string _externalDiffToolCommandMask = string.Empty; 

        public bool AutoActivatePlugin
        {
            get { return _autoActivatePlugin; }
            set { _autoActivatePlugin = value; }
        }

        public bool AutoAddFiles
        {
            get { return _autoAddFiles; }
            set { _autoAddFiles = value; }
        }

        public string ExternalDiffToolCommandMask
        {
            get { return _externalDiffToolCommandMask; }
            set { _externalDiffToolCommandMask = value; }
        }

        public bool EnableContextSearch
        {
            get { return _enableContextSearch; }
            set { _enableContextSearch = value; }
        }

        public bool ObserveOutOfStudioFileChanges
        {
            get => _observeOutOfStudioFileChanges;
            set => _observeOutOfStudioFileChanges = value;
        }

        public bool UseVisualStudioDiff
        {
            get => _useVisualStudioDiff;
            set => _useVisualStudioDiff = value;
        }
        
        /// <summary>
        /// global accessible settings object
        /// </summary>
        static Configuration _global = null;
        public static Configuration Global
        {
            get => _global ?? (_global = LoadConfiguration());
            set => _global = value;
        }
        
        /// <summary>
        /// read properties from regestry
        /// </summary>
        /// <returns></returns>
        public static Configuration LoadConfiguration()
        {
            Configuration configuration = new Configuration();
            RegestryTool.LoadProperties("Configuration", configuration);
            return configuration;
        }
        
        /// <summary>
        /// store properties to regestry
        /// </summary>
        public void StoreConfiguration()
        {
            RegestryTool.StoreProperties("Configuration", this);
        }

    }
}

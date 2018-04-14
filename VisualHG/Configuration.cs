namespace VisualHG
{
    /// <summary>
    ///     visualhg configuration properties container
    /// </summary>
    public class Configuration
    {
        public bool AutoActivatePlugin { get; set; } = true;

        public bool AutoAddFiles { get; set; } = true;

        public string ExternalDiffToolCommandMask { get; set; } = string.Empty;

        public bool EnableContextSearch { get; set; } = true;

        public bool ObserveOutOfStudioFileChanges { get; set; } = true;

        /// <summary>
        ///     global accessible settings object
        /// </summary>
        private static Configuration _global;

        public static Configuration Global
        {
            get => _global ?? (_global = LoadConfiguration());
            set => _global = value;
        }

        /// <summary>
        ///     read properties from regestry
        /// </summary>
        /// <returns></returns>
        public static Configuration LoadConfiguration()
        {
            var configuration = new Configuration();
            RegestryTool.LoadProperties("Configuration", configuration);
            return configuration;
        }

        /// <summary>
        ///     store properties to regestry
        /// </summary>
        public void StoreConfiguration()
        {
            RegestryTool.StoreProperties("Configuration", this);
        }
    }
}
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace VisualHG
{
    /// <summary>
    ///     Summary description for SccProviderOptionsControl.
    /// </summary>
    public class SccProviderOptionsControl : UserControl
    {
        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private readonly Container _components = null;

        private CheckBox _autoAddFiles;
        private CheckBox _autoActivatePlugin;
        private Button _editDiffToolButton;
        private TextBox _externalDiffToolCommandEdit;
        private Label _label1;
        private CheckBox _observeOutOfStudioFileChanges;

        private CheckBox _enableContextSearch;

        // The parent page, use to persist data
        private SccProviderOptions _customPage;

        public SccProviderOptionsControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call
        }

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _components?.Dispose();
                GC.SuppressFinalize(this);
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._autoAddFiles = new System.Windows.Forms.CheckBox();
            this._autoActivatePlugin = new System.Windows.Forms.CheckBox();
            this._editDiffToolButton = new System.Windows.Forms.Button();
            this._externalDiffToolCommandEdit = new System.Windows.Forms.TextBox();
            this._label1 = new System.Windows.Forms.Label();
            this._observeOutOfStudioFileChanges = new System.Windows.Forms.CheckBox();
            this._enableContextSearch = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // _autoAddFiles
            // 
            this._autoAddFiles.AutoSize = true;
            this._autoAddFiles.Checked = true;
            this._autoAddFiles.CheckState = System.Windows.Forms.CheckState.Checked;
            this._autoAddFiles.Location = new System.Drawing.Point(3, 26);
            this._autoAddFiles.Name = "_autoAddFiles";
            this._autoAddFiles.Size = new System.Drawing.Size(299, 17);
            this._autoAddFiles.TabIndex = 0;
            this._autoAddFiles.Text = "Add files automatically to Mercurial ( except ignored ones )";
            this._autoAddFiles.UseVisualStyleBackColor = true;
            // 
            // _autoActivatePlugin
            // 
            this._autoActivatePlugin.AutoSize = true;
            this._autoActivatePlugin.Checked = true;
            this._autoActivatePlugin.CheckState = System.Windows.Forms.CheckState.Checked;
            this._autoActivatePlugin.Location = new System.Drawing.Point(3, 3);
            this._autoActivatePlugin.Name = "_autoActivatePlugin";
            this._autoActivatePlugin.Size = new System.Drawing.Size(228, 17);
            this._autoActivatePlugin.TabIndex = 1;
            this._autoActivatePlugin.Text = "Autoselect VisualHG for Mercurial solutions";
            this._autoActivatePlugin.UseVisualStyleBackColor = true;
            // 
            // _editDiffToolButton
            // 
            this._editDiffToolButton.Location = new System.Drawing.Point(369, 106);
            this._editDiffToolButton.Name = "_editDiffToolButton";
            this._editDiffToolButton.Size = new System.Drawing.Size(28, 23);
            this._editDiffToolButton.TabIndex = 2;
            this._editDiffToolButton.Text = "...";
            this._editDiffToolButton.UseVisualStyleBackColor = true;
            this._editDiffToolButton.Click += new System.EventHandler(this.OnEditDiffToolButton);
            // 
            // _externalDiffToolCommandEdit
            // 
            this._externalDiffToolCommandEdit.Location = new System.Drawing.Point(3, 108);
            this._externalDiffToolCommandEdit.Name = "_externalDiffToolCommandEdit";
            this._externalDiffToolCommandEdit.Size = new System.Drawing.Size(360, 20);
            this._externalDiffToolCommandEdit.TabIndex = 3;
            // 
            // _label1
            // 
            this._label1.AutoSize = true;
            this._label1.Location = new System.Drawing.Point(0, 92);
            this._label1.Name = "_label1";
            this._label1.Size = new System.Drawing.Size(138, 13);
            this._label1.TabIndex = 4;
            this._label1.Text = "External Diff Tool Command";
            // 
            // _observeOutOfStudioFileChanges
            // 
            this._observeOutOfStudioFileChanges.AutoSize = true;
            this._observeOutOfStudioFileChanges.Checked = true;
            this._observeOutOfStudioFileChanges.CheckState = System.Windows.Forms.CheckState.Checked;
            this._observeOutOfStudioFileChanges.Location = new System.Drawing.Point(3, 49);
            this._observeOutOfStudioFileChanges.Name = "_observeOutOfStudioFileChanges";
            this._observeOutOfStudioFileChanges.Size = new System.Drawing.Size(189, 17);
            this._observeOutOfStudioFileChanges.TabIndex = 5;
            this._observeOutOfStudioFileChanges.Text = "Observe out of Studio file changes";
            this._observeOutOfStudioFileChanges.UseVisualStyleBackColor = true;
            // 
            // _enableContextSearch
            // 
            this._enableContextSearch.AutoSize = true;
            this._enableContextSearch.Checked = true;
            this._enableContextSearch.CheckState = System.Windows.Forms.CheckState.Checked;
            this._enableContextSearch.Location = new System.Drawing.Point(3, 72);
            this._enableContextSearch.Name = "_enableContextSearch";
            this._enableContextSearch.Size = new System.Drawing.Size(379, 17);
            this._enableContextSearch.TabIndex = 6;
            this._enableContextSearch.Text = "Context sensitive Add and Commit Menu (can become slow on huge repos)";
            this._enableContextSearch.UseVisualStyleBackColor = true;
            // 
            // SccProviderOptionsControl
            // 
            this.AllowDrop = true;
            this.Controls.Add(this._enableContextSearch);
            this.Controls.Add(this._observeOutOfStudioFileChanges);
            this.Controls.Add(this._label1);
            this.Controls.Add(this._externalDiffToolCommandEdit);
            this.Controls.Add(this._editDiffToolButton);
            this.Controls.Add(this._autoActivatePlugin);
            this.Controls.Add(this._autoAddFiles);
            this.Name = "SccProviderOptionsControl";
            this.Size = new System.Drawing.Size(400, 271);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public SccProviderOptions OptionsPage
        {
            set => _customPage = value;
        }

        public void StoreConfiguration(Configuration config)
        {
            config.AutoActivatePlugin = _autoActivatePlugin.Checked;
            config.AutoAddFiles = _autoAddFiles.Checked;
            config.EnableContextSearch = _enableContextSearch.Checked;
            config.ObserveOutOfStudioFileChanges = _observeOutOfStudioFileChanges.Checked;
            config.ExternalDiffToolCommandMask = _externalDiffToolCommandEdit.Text;
        }

        public void RestoreConfiguration(Configuration config)
        {
            _autoActivatePlugin.Checked = config.AutoActivatePlugin;
            _autoAddFiles.Checked = config.AutoAddFiles;
            _enableContextSearch.Checked = config.EnableContextSearch;
            _observeOutOfStudioFileChanges.Checked = config.ObserveOutOfStudioFileChanges;
            _externalDiffToolCommandEdit.Text = config.ExternalDiffToolCommandMask;
        }

        private void UpdateGlyphs_Click(object sender, EventArgs e)
        {
            var sccProviderService = (SccProviderService) GetService(typeof(SccProviderService));
            sccProviderService.RefreshNodesGlyphs();
        }

        private void OnEditDiffToolButton(object sender, EventArgs e)
        {
            var selectDiffToolTemplateDialog = new SelectDiffToolTemplateDialog();
            var result = selectDiffToolTemplateDialog.ShowDialog();
            if (result == DialogResult.OK)
                _externalDiffToolCommandEdit.Text = selectDiffToolTemplateDialog.selectedTemplate;
        }
    }
}
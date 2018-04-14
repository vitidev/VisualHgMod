using System.Windows.Forms;

namespace VisualHG
{
    /// <summary>
    ///     Command templates for external diff tools
    /// </summary>
    public partial class SelectDiffToolTemplateDialog : Form
    {
        public SelectDiffToolTemplateDialog()
        {
            ShowIcon = false;
            InitializeComponent();
        }

        public string selectedTemplate => diffToolTemplateListCtrl.Text;
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using static System.Windows.Forms.DataFormats;
using static UkrainianAnalyzer.Form1;

namespace UkrainianAnalyzer
{
    public partial class SettingsForm : Form
    {
        public event Action<string> StringChanged;
        public SettingsForm()
        {
            InitializeComponent();
            Hide();
        }

        private void SpecialButtonTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!IsValidModifierKey(e.KeyChar))
            {
                e.Handled = true;
            }
        }
        private void RegularButtonTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow only single keys
            if (!IsValidSingleKey(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private bool IsValidSingleKey(char key)
        {
            return (char.IsLetterOrDigit(key) || char.IsPunctuation(key) || char.IsSymbol(key));
        }
        private bool IsValidModifierKey(char key)
        {
            KeyModifier modifier;
            bool isEnumMember = Enum.TryParse(key.ToString(), out modifier);

            return isEnumMember;
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            StringChanged?.Invoke(checkedListBox1.Text);
            Hide();
        }
    }
}

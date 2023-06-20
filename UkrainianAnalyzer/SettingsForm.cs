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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static TextAnalyzer.Form1;

namespace TextAnalyzer
{
    public partial class SettingsForm : Form
    {
        public event Action<Color> MainColorChanged;
        public event Action<Color> FillColorChanged;
        public event Action<int> RegularKeyChanged;
        public event Action<int> SpecialKeyChanged;

        private int regularKey;
        public SettingsForm(Color mainColor, Color fillColor, int regularKey, int specialKey)
        {
            InitializeComponent();
            InitializeParameters(mainColor, fillColor, regularKey, specialKey);
            Hide();
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.BackColor = colorDialog1.Color;
                MainColorChanged?.Invoke(button1.BackColor);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (colorDialog2.ShowDialog() == DialogResult.OK)
            {
                button2.BackColor = colorDialog2.Color;
                FillColorChanged?.Invoke(button2.BackColor);
            }
        }
        private void RegularButtonTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar))
            {
                e.KeyChar = char.ToUpper(e.KeyChar);
            }

            bool isLetter = char.IsLetter(e.KeyChar);
            bool isDigit = char.IsDigit(e.KeyChar);
            bool isBackspaceOrEnter = e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Enter;

            bool isInvalidInput = !isLetter && !isDigit && !isBackspaceOrEnter;

            e.Handled = isInvalidInput;
            if (!isInvalidInput)
            {
                regularKey = e.KeyChar;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int specialKeyNumber = 0;
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    specialKeyNumber = 1;
                    break;
                case 1:
                    specialKeyNumber = 2;
                    break;
                case 2:
                    specialKeyNumber = 4;
                    break;
                case 3:
                    specialKeyNumber = 8;
                    break;
            }
            SpecialKeyChanged?.Invoke(specialKeyNumber);
        }

        private void RegularButtonTxt_TextChanged(object sender, EventArgs e)
        {
            RegularKeyChanged?.Invoke(regularKey);
        }

        private void InitializeParameters(Color mainColor, Color fillColor, int regularHotkey, int specialHotey)
        {
            button1.BackColor = mainColor;
            button2.BackColor = fillColor;
            RegularButtonTxt.Text = Convert.ToString(Convert.ToChar(regularHotkey));
            SpecialKeys selectedKey = (SpecialKeys)specialHotey;
            comboBox1.Text = selectedKey.ToString();
        }

        private enum SpecialKeys
        {
            ALT = 1,
            CTRL = 2,
            SHFT = 4,
            WIN = 8
        }
    }
}

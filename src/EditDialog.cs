using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DragonQuestLoc
{
    public partial class EditDialog : Form
    {
        public EditDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        public void Setup(string Key, string Value)
        {
            textBox1.Text = Key;
            textBox2.Text = Value;
        }

        public Tuple<string,string> UserInput()
        {
            string key = textBox1.Text;
            string val = textBox2.Text;

            return Tuple.Create<string, string>(key, val);
        }
    }
}

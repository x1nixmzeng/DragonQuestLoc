using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DragonQuestLoc
{
    public partial class Main : Form
    {
        MesDatatype LastMes;

        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LastMes = null;

            ResetUI();
            ResetTitle();
        }

        private void ResetTitle(string fn = "")
        {
            string default_name = "Dragon Quest Monsters: Joker 3";

            if( (LastMes != null ) && fn.Length > 0 )
            {
                Text = string.Format("{0} - {1}", Path.GetFileName(fn), default_name);
            }
            else
            {
                Text = default_name;
            }
        }

        private void ResetUI()
        {
            listView1.Items.Clear();

            if (LastMes != null)
            {
                var mes_items = LastMes.GetAllValues();

                foreach (Tuple<string, string> pair in mes_items)
                {
                    var new_item = listView1.Items.Add(pair.Item1).SubItems;
                    new_item.Add(pair.Item2);   // original value
                    new_item.Add("");           // modified value
                }

                exportToolStripMenuItem.Enabled = true;
            }
            else
            {
                exportToolStripMenuItem.Enabled = false;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if( openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                string fn1 = openFileDialog1.FileNames[0];

                if( File.Exists(fn1) )
                {
                    FileStream fs = File.OpenRead(fn1);

                    if( fs.CanRead )
                    {
                        var br = new BinaryReader(fs);

                        var mes = new MesDatatype();
                        mes.Read(br);

                        LastMes = mes;
                        ResetUI();
                        ResetTitle(fn1);
                    }

                    fs.Close();
                }
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if( listView1.SelectedIndices.Count == 1 )
            {
                int index = listView1.SelectedIndices[0];

                EditDialog ed = new EditDialog();

                var single = LastMes.GetSingleValue(index);

                ed.Setup(single.Item1, single.Item2);

                if( ed.ShowDialog() == System.Windows.Forms.DialogResult.OK )
                {
                    var result = ed.UserInput();

                    if( LastMes.UpdateValue(index, result.Item1, result.Item2) )
                    {
                        listView1.SelectedItems[0].SubItems[2].Text = result.Item2;
                    }
                }

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if( LastMes != null )
            {
                int edit_count = LastMes.NumEdits();

                if (edit_count > 0)
                {
                    string saver_str = string.Format("You have made changes to {0} line(s)\n\nAre you sure you want to quit?", edit_count);

                    if( MessageBox.Show(saver_str, "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.Yes)
                    {
                        e.Cancel = true;
                    }
                }
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if( LastMes != null )
            {
                if( saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if( saveFileDialog1.FileNames.Length == 1 )
                    {
                        string fn1 = saveFileDialog1.FileNames[0];

                        FileStream fs = File.Create(fn1);
                        if( fs != null )
                        {
                            var bw = new BinaryWriter(fs);

                            LastMes.Write(bw);

                            ResetUI();

                            fs.Close();
                        }
                    }
                }
            }
        }
    }
}

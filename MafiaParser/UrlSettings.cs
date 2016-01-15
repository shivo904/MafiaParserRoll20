using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

namespace MafiaParser
{
    public partial class UrlSettings : Form
    {
        public UrlSettings()
        {
            InitializeComponent();
        }

        private void UrlSettings_Load(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = File.ReadAllText("MafiaUrlConfig");
            }
            catch(Exception)
            {
                textBox1.Text = "";
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText("MafiaUrlConfig", textBox1.Text);
                this.Close();
            }
            catch (Exception err)
            {
                MessageBox.Show("The new url was not saved because of the following error: " + err);
            }
        }
    }
}

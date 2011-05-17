using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tax_AideFlashShare
{
    public partial class ProgessOverall : Form
    {
        public ProgessOverall()
        {
            InitializeComponent();
        }
        public void ProgShow()
        {
            this.ShowDialog();
        }
        public void AddTxtLine(string updateTxt)
        {
            this.statusText.Text += "\r\n" +updateTxt;
            this.Update();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }
    }
}

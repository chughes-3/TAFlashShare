using System;
using System.Windows.Forms;

namespace TaxAideFlashShare
{
    public partial class ProgessOverall : Form
    {
        delegate void EnableOKCallBack();
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
        public void EnableOk()
        {
            if (this.buttonOK.InvokeRequired) //see http://msdn.microsoft.com/en-us/library/ms171728(VS.90).aspx
            {
                EnableOKCallBack d = new EnableOKCallBack(EnableOk);
                this.Invoke(d);
            }
            else
            {
                buttonOK.Enabled = true;
                this.Update(); 
            }
        }
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}

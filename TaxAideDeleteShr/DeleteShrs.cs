using System;
using System.Management;
using System.Windows.Forms;

namespace TaxAideDeleteShr
{
    class DeleteShrs
    {
        public void DeleteShares()
        {
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == TaxAideFlashShare.ProjConst.shareName | shr.GetPropertyValue("Name").ToString() == TaxAideFlashShare.ProjConst.shareNameLegacy)
                {
                    try { shr.Delete(); }
                    catch (Exception e)
                    {
                        MessageBox.Show( "Exception while deleting share. The error was \r\n" + e.Message );
                        Environment.Exit(1);
                    }
                }
            }
            Environment.Exit(0);
        }
    }
}

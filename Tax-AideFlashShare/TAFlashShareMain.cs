using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TaxAideFlashShare
{
    static class TAFlashShareMain
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            new ProgOverallThread(); // initialize status window
            ProgramData thisProg = new ProgramData();
            Pdrive fold = new Pdrive(thisProg);
            if (args.Length == 0)
            {
                if (fold.TestPplusShareExistence() == 1)
                    return;
                if (thisProg.removable)
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "We have a removable drive" });
                else
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "We are running from a hard drive or network drive" });
                if (fold.SetSymbolicLink() != 0)    // symlink will not be done if desktop checked in method
                    return; //Error on creation of symlink should have been output so simply exit leaving message window up
                thisProg.CreateShortcuts();
                if (fold.ShareFolder() != 0)
                    return; // failed on folder sharing
                if (fold.MapDrive(thisProg.drvLetter) != 0)
                    return; //failed mapping
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "\r\n\r\n" + ProjConst.mapDriveName + "  Drive Created and Shared. \r\nIf no errors are listed above and this system is on a network the Workstations can be started now " });
                ProgOverallThread.progOverallWin.EnableOk();
            }
            else
                switch (args[0])
                {
                    case "/u":
                        fold.UnMapDrive();
                        fold.DeleteShares();
                        if (thisProg.removable)
                        {
                            fold.DeleteSymLink();
                            System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Delete TA FlashShare.lnk");  //Delete the shortcut file 
                        }
                        else
                            System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Delete TA Share.lnk");  //Delete the shortcut file 
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "\r\n\r\n" + "Process is complete, any errors will have been listed above" });
                        ProgOverallThread.progOverallWin.EnableOk();
                        break;
                    default:
                        MessageBox.Show("Error in calling argument\n\n   Exiting", ProgramData.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        Environment.Exit(1);
                        break;
                }
        }
    }
}

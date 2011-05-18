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
                if (thisProg.removable)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "We have a removable drive" });
                    //fold.CheckUsersPublicShares();  // check conditions are right for setting up shares
                    thisProg.CreateShortcuts();
                    if (fold.SetSymbolicLink() != 0)
                        return; //Error on creation of symlink should have been output so simply exit leaving message window up
                    if (fold.ShareFolder() != 0)
                        return; // failed on folder sharing
                    if (fold.MapDrive(thisProg.drvLetter) != 0)
                        return; //failed mapping
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "\r\n\r\n" + Pdrive.mapDriveName + "  Drive Created and Shared. \r\nIf on a network the Workstations can be started now " });
                    ProgOverallThread.EnableOKDel.Invoke();
                }
                else
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Not running from a removable drive. At this point the program will do nothing" });
            }
            else
                switch (args[0])
                {
                    case "/u":
                        fold.UnMapDrive();
                        fold.DeleteShares();
                        fold.DeleteSymLink();
                        System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Delete TA FlashShare.lnk");  //Delete the shortcut file
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "\r\n\r\n" + Pdrive.mapDriveName + "  Drive UnMapped, and Unshared" });
                        ProgOverallThread.EnableOKDel.Invoke();
                        break;
                    default:
                        MessageBox.Show("Error in calling argument\n\n   Exiting", ProgramData.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        Environment.Exit(1);
                        break;
                }
        }
    }
}

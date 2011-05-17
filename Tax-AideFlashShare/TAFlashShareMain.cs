using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tax_AideFlashShare
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
            if (thisProg.removable)
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "We have a removable drive" });
            else
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Not running from a removable drive. At this point the program will do nothing" });
            if (args.Length == 0)
            {
                if (thisProg.removable)
                {
                    fold.CheckUsersPublicShares();  // check conditions are right for setting up shares
                    fold.SymbolicLink(fold.folder2Share + "\\" + thisProg.drvLetter, thisProg.drvLetter + ":\\", true);
                    fold.ShareFolder(thisProg.drvLetter);
                    fold.MapDrive(thisProg.drvLetter);
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "\r\n" + Pdrive.mapDriveName + "  Drive Created and Shared. \r\nIf on a network the Workstations can be started now " });
                }
            }
            else
                switch (args[0])
                {
                    case "/u":
                        fold.UnMapDrive();
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.mapDriveName + " Drive Unmapped" });
                        fold.DeleteShares();
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.shareName + "Share Deleted" });
                        System.IO.Directory.Delete(fold.folder2Share + "\\" + thisProg.drvLetter);
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link \"" + fold.folder2Share + "\\" + thisProg.drvLetter + "\" Deleted" });
                        break;
                    default:
                        MessageBox.Show("Error in calling argument\n\n   Exiting", ProgramData.mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        break;
                }
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }
    }
}

using System;
using System.Windows.Forms;

namespace Tax_AideSymLink
{
    static class SymLinkMain
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //args[0] is symbolic link Like c:\users\public\TAsymlink, args[1] is target path like H:\ (a flash drive), args[2] is removable for removable drive,desktop for desktop
            if (args[2] == "Removable")
            {
                int r = SymLink.SymbolicLink(args[0], args[1], true); // setup for directories only is the true
                if (r == 1)
                    Environment.Exit(1); // error in symlink therefore error exit
            }    
            //Do not need to check OS because this only called if v or W7
            // we have vista/W7 therefore must share folder under elevated permissions
            if (new ShareFolder().ShareCreate(TaxAideFlashShare.ProjConst.shareName, args[0], "Tax-Aide Share") == 1) //share the symlink symlinkpath is set up appropriately for desktop and removable by calling program
                    Environment.Exit(1);
            Environment.Exit(0);
        }
    }
}

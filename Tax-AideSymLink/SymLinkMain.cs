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
            //args[0] is symbolic link Like c:\users\public\TAsymlink, args[1] is target path like H:\ (a flash drive)
            int r = SymLink.SymbolicLink(args[0], args[1], true); // setup for directories only
            if (r == 1)
                Environment.Exit(1); // error in symlink therefore error exit
            if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0)
            {// we have vista therefore must share folder under elevated permissions
                if (new ShareFolder().SharFolder(args[0]) == 1)
                    Environment.Exit(1);
            }
            Environment.Exit(0);
        }
    }
}

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
            //args[0] is symbolic link  args[1] is target
            int r = SymLink.SymbolicLink(args[0], args[1], true); // setup for directories only
            if (r == 0)
                Environment.Exit(0);
            else
                Environment.Exit(1);
        }
    }
}

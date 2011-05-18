using System;
using System.Runtime.InteropServices;

namespace Tax_AideSymLink
{
    class SymLink
    {
        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]// NEEDS ADMIN RIGHTS!!!
        public static extern Boolean CreateSymbolicLink([In] string lpSymlinkFileName, [In] string lpTargetFileName, int dwFlags);
        /// <summary>
        /// create Symbolic link symLinkPath is linked to the targetPath
        /// </summary>
        /// <param name="symLinkPath"></param>
        /// <param name="targetPath"></param>
        /// <param name="directory"></param>
        internal static int SymbolicLink(string symLinkPath, string targetPath, bool directory)
        {
            const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
            int dwFlags = 0; //SYMBLOC_LINK_FLAG_FILE          
            if (directory) dwFlags = SYMBOLIC_LINK_FLAG_DIRECTORY;
            try
            {
                if (!CreateSymbolicLink(symLinkPath, targetPath, dwFlags))
                    throw new System.ComponentModel.Win32Exception(); // automatically gets the last error on thread if attribute flag set and raises exception
            }
            catch (System.ComponentModel.Win32Exception w)
            {
                System.Exception e = w.GetBaseException();
                System.Windows.Forms.MessageBox.Show(String.Format("Error on Creating Symbolic Link\n\nMessage = {0}\n\nError Code = 0x{1:x}, Native Error Code = 0x{2:x}\nStack Trace = {3}\nSource = {4}\nBase Exception = {5}\n\nSymLink Path = {6}\nTargetPath = {7}", w.Message, w.ErrorCode, w.NativeErrorCode, w.StackTrace, w.Source, e.Message, symLinkPath, targetPath), "Tax-Aide Symbolic Link Setup");
                return 1;
            }
            return 0;
        }

    }
}

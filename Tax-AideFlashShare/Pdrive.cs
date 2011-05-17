using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Security.Principal;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace Tax_AideFlashShare
{
    class Pdrive
    {
        public const string mapDriveLetter = "P";
        public const string mapDriveName = mapDriveLetter + ":\\";
        public const string shareName = "TaxWiseServer_" + mapDriveLetter;
        const string shareNameLegacy = "TWSRVR_" + mapDriveLetter; 
        public string folder2Share; // NO SLASHES AT END
        ProgramData thisProginst;
        NetDrive pDrv = new NetDrive();
        enum shareCreateErrorCodes { Success = 0, AccessDenied = 2, UnknownFailure = 8, InvalidName = 9, InvalidLevel = 10, InvalidParameter = 21, DuplicateShare = 22, RedirectedPath = 23, UnknownDeviceorDirectory = 24, NetNameNotFound = 25 }
        public Pdrive(ProgramData thisProg)
        {
            thisProginst = thisProg;
            if (ProgramData.osVer == 6)
            {
                //setup public folders
                folder2Share = Environment.GetEnvironmentVariable("PUBLIC");
            }
            else
            {
                if (!Directory.Exists(Environment.GetEnvironmentVariable("HOMEDRIVE") + "\\PUBLIC"))
                    Directory.CreateDirectory(Environment.GetEnvironmentVariable("HOMEDRIVE") + "\\PUBLIC"); //Permissions????
                folder2Share = Environment.GetEnvironmentVariable("HOMEDRIVE") + "\\PUBLIC";
            }
        }
        internal void MapDrive(string scriptDrvLetter)
        {
            pDrv.LocalDrive = mapDriveName;    //slashes are removed
            pDrv.ShareName = "\\\\" + Environment.GetEnvironmentVariable("COMPUTERNAME") + "\\" + shareName; //removes drive letter
            pDrv.MapDrive();
            //put chekc in that mapping took place
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.mapDriveName + " Drive Mapped" });
        }
        internal void UnMapDrive()
        {
            if (Directory.Exists(mapDriveName))
            {
                pDrv.LocalDrive = mapDriveName;    //slashes are removed in called method
                pDrv.UnMapDrive(); 
            }
        }
        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]// NEEDS ADMIN RIGHTS!!!
            public static extern Boolean CreateSymbolicLink([In] string lpSymlinkFileName, [In] string lpTargetFileName, int dwFlags);
        /// <summary>
        /// create Symbolic link symLinkPath is linked to the targetPath
        /// </summary>
        /// <param name="symLinkPath"></param>
        /// <param name="targetPath"></param>
        /// <param name="directory"></param>
        internal void SymbolicLink(string symLinkPath, string targetPath, bool directory)
        {
            const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
            int dwFlags = 0; //SYMBLOC_LINK_FLAG_FILE          
            if (directory) dwFlags = SYMBOLIC_LINK_FLAG_DIRECTORY;
            try
            {
                if (!CreateSymbolicLink(symLinkPath, targetPath, dwFlags))
                    throw new System.ComponentModel.Win32Exception(); // automatically gets the last error on thread if attribute flag set and raises exception
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link \"" + folder2Share + "\\" + thisProginst.drvLetter + "\" Created" });
            }
            catch (System.ComponentModel.Win32Exception w)
            {
                System.Exception e = w.GetBaseException();
                System.Windows.Forms.MessageBox.Show(String.Format("Error on Creating Symbolic Link\n\nMessage = {0}\n\nError Code = 0x{1:x}, Native Error Code = 0x{2:x}\nStack Trace = {3}\nSource = {4}\nBase Exception = {5}\n\nSymLink Path = {6}\nTargetPath = {7}", w.Message, w.ErrorCode, w.NativeErrorCode, w.StackTrace, w.Source, e.Message, symLinkPath, targetPath), ProgramData.mbCaption);
            }

        }


        #region Methods for Sharing and unsharing a folder
        internal void ShareFolder(string scriptDrvLetter)
        {
            ShareCreate(shareName, folder2Share + "\\" + scriptDrvLetter, "Tax-Aide Share");
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.shareName + " Share Created" });
        }
        private void ShareCreate(string ShareName, string FolderPath, string Description)
        {
            ManagementClass mgmtClass = new ManagementClass("Win32_Share");
            ManagementBaseObject inParams = mgmtClass.GetMethodParameters("Create");
            ManagementBaseObject outParams;
            inParams["Description"] = Description;
            inParams["Name"] = ShareName;
            inParams["Path"] = FolderPath;
            inParams["Type"] = 0x0; //disk drive
            inParams["Access"] = SecurityDescriptor();  //for Everyone full perms
            outParams = mgmtClass.InvokeMethod("Create", inParams, null);
            if ((uint)(outParams.Properties["ReturnValue"].Value) != 0)
            {
                string errCode = Enum.GetName(typeof(shareCreateErrorCodes), outParams.Properties["ReturnValue"].Value);
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] {String.Format("Unable to create a network share. The error message was {0}\n\nShareName = {1}\nFolderPath = {2}", errCode, ShareName, FolderPath)});
                Environment.Exit(1);
            }
        }
        private ManagementObject SecurityDescriptor()//creates Everyone,Full, inherit security descriptor
        {
            SecurityIdentifier sec = new SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
            byte[] sidArray = new byte[sec.BinaryLength];
            sec.GetBinaryForm(sidArray, 0);
            ManagementObject Trustee = new ManagementClass(new ManagementPath("Win32_Trustee"), null);
            Trustee["Domain"] = "NT Authority";
            Trustee["Name"] = "Everyone";
            Trustee["SID"] = sidArray;
            ManagementObject ACE = new ManagementClass(new ManagementPath("Win32_Ace"), null);
            ACE["AccessMask"] = 2032127; // 0x1f01ff Full Access
            ACE["AceFlags"] = 3;    //Non-container and container child objects to inherit ace
            ACE["AceType"] = 0;     //defines access allowed (1 would be defining access denied
            ACE["Trustee"] = Trustee;
            ManagementObject SecDesc = new ManagementClass(new ManagementPath("Win32_SecurityDescriptor"), null);
            SecDesc["ControlFlags"] = 4;        //SE_DACL_present
            SecDesc["DACL"] = new object[] { ACE };
            return SecDesc;
        }

        public void DeleteShares()
        {
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == shareName | shr.GetPropertyValue("Name").ToString() == shareNameLegacy)
                {
                    try { shr.Delete(); }
                    catch (Exception) { continue; }
                }
            }
        }
        internal void CheckUsersPublicShares()
        {
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Checking that Users and Public folders are shared, fixing if necessary." });
            bool usersExist = false;
            bool publicExist = false;
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == "Users")
                    usersExist = true;
                if (shr.GetPropertyValue("Name").ToString() == "Public")
                    publicExist = true;
            }
            if (!usersExist)
                ShareCreate("Users", folder2Share.Remove(folder2Share.Length - 8), "");
            if (!publicExist)
                ShareCreate("Public", folder2Share, "");
        }
        
        #endregion

    }
}

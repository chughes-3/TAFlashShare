using System;
using System.Management;
using System.Security.Principal;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TaxAideFlashShare
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
        internal int MapDrive(string scriptDrvLetter)
        {
            pDrv.LocalDrive = mapDriveName;    //slashes are removed
            pDrv.ShareName = "\\\\" + Environment.GetEnvironmentVariable("COMPUTERNAME") + "\\" + shareName; //removes drive letter
            if (pDrv.MapDrive() == 0)
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.mapDriveName + " Drive Mapped" });
                return 0;
            }
            else return 1;
        }
        internal int UnMapDrive()
        {
            if (Directory.Exists(mapDriveName))
            {
                pDrv.LocalDrive = mapDriveName;    //slashes are removed in called method
                if (pDrv.UnMapDrive() == 0)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.mapDriveName + " Drive UnMapped" });
                    return 0;
                }
                else return 1;
            }
            else
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.mapDriveName + " Does not Exist, Unmapping not done" });
                return 1;
            }
        }
        internal int SetSymbolicLink(string symLinkPath, string targetPath)
        {
            if (Directory.Exists(symLinkPath))
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link already exists - will not attempt creation" });
                return 0;
            }
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Getting Admin permission to create Symbolic Link" });
            //get exe file out of assembly
            if (thisProginst.CopyFileFromThisAssembly("Tax-AideSymLink.exe",Environment.GetEnvironmentVariable("temp")) != 0) return 1;
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("temp") + "\\Tax-AideSymLink.exe", symLinkPath + " " + targetPath);
            p.Start();
            p.WaitForExit();
            if (p.ExitCode == 0)
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link Created" });
                return 0;
            }
            else
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "ERROR - Cannot create Symbolic Link" });
                return 1;
            }
        }


        #region Methods for Sharing and unsharing a folder
        internal int ShareFolder(string scriptDrvLetter)
        {
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == shareName)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "The" + shareName + " share already exists, trying to delete it in order to add the share correctly" });
                    try { shr.Delete(); }
                    catch (Exception e) 
                    {
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Exception when creating the " + shareName + " share. The message was \r\n " + e.Message });
                        return 1 ;
                    }
                }
            }

            if (ShareCreate(shareName, folder2Share + "\\" + ProgramData.symLinkName, "Tax-Aide Share") == 0)
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.shareName + " Share Created" });
                return 0;
            }
            else
                return 1;
        }
        private int ShareCreate(string ShareName, string FolderPath, string Description)
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
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { String.Format("Unable to create a network share. The error message was {0}\n\nShareName = {1}\nFolderPath = {2}", errCode, ShareName, FolderPath) });
                return 1;
            }
            else return 0;
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
                    catch (Exception e) 
                    {
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Exception while deleting share. The error was \r\n" + e.Message });
                    }
                }
            }
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { Pdrive.shareName + " Share Deleted" });

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


        internal void DeleteSymLink()
        {
            if ( Directory.Exists(folder2Share + "\\" + ProgramData.symLinkName))
                System.IO.Directory.Delete(folder2Share + "\\" + ProgramData.symLinkName);
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link \"" + folder2Share + "\\" + thisProginst.drvLetter + "\" Deleted" });
        }
    }
}

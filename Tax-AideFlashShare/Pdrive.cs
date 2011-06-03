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
        public string folder2Share; // NO SLASHES AT END
        public string symLinkPath = "";
        ProgramData thisProginst;
        NetDrive pDrv = new NetDrive();
        enum shareCreateErrorCodes { Success = 0, AccessDenied = 2, UnknownFailure = 8, InvalidName = 9, InvalidLevel = 10, InvalidParameter = 21, DuplicateShare = 22, RedirectedPath = 23, UnknownDeviceorDirectory = 24, NetNameNotFound = 25 }
        public Pdrive(ProgramData thisProg)
        {
            thisProginst = thisProg;
            if (ProgramData.osVerMaj == 6 && thisProginst.removable)
            {
                //setup public folders
                folder2Share = Environment.GetEnvironmentVariable("PUBLIC");
            }
            else
            {
                folder2Share = Environment.GetEnvironmentVariable("HOMEDRIVE");
            }
            if (thisProginst.removable) //Desktop no symlink but all routines still work because symlinkpath = folder2share
                symLinkPath = folder2Share + "\\" + ProgramData.symLinkName;
            else
                if (folder2Share.EndsWith(":")) // Fixes idiosyncracy of net share will not allow \ on regulare folders but must have it on drives
                    symLinkPath = folder2Share + "\\";
                else
                    symLinkPath = folder2Share;
        }
        internal int TestPplusShareExistence()
        {
            //Test if pdrive or share is already used
            //Needs reworking to msbox user offering cancel choice of user removable or programmatic removal and reboot don't forget persistent mapping as well as regular
            if (Directory.Exists(ProjConst.mapDriveName))
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "P drive exists, will try unmapping" });
                pDrv.Persistent = true; // these are set to attempt to force a more direct unmap and registry update
                pDrv.Force = true;
                int ret = UnMapDrive();
                pDrv.Persistent = false;    //set up for our pdrive which is temporary
                pDrv.Force = false;
                if (ret == 1)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Unmapping failed, Please remove the P drive manually then restart the program" });
                    return 1;
                }
               //Need registry test and deletion here
                else
                     ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Unmapping apparently successfully, if errors occur later the unmapping may have to be done manually" });
            }
            if (Directory.Exists(ProjConst.mapDriveName))
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Attempted Unmapping failed,P drive still exists, Please remove the P drive manually then restart the program" });
                return 1;
            }
            //Find out if share exists
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == ProjConst.shareName)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "The" + ProjConst.shareName + " share already exists, trying to delete it in order to add the share correctly" });
                    if (DeleteShares() == 1)
                        return 1;
                }
            }
            return 0;
        }

        internal int MapDrive(string scriptDrvLetter)
        {
            if (Directory.Exists(ProjConst.mapDriveName))
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { ProjConst.mapDriveName + " Drive Exists. Either disconnect it manually or by running this programs delete option - then rerun this program" });
                return 1;
            }
            pDrv.LocalDrive = ProjConst.mapDriveName;    //slashes are removed by called method
            pDrv.ShareName = "\\\\" + Environment.GetEnvironmentVariable("COMPUTERNAME") + "\\" + ProjConst.shareName; 
            if (pDrv.MapDrive() == 0)
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { ProjConst.mapDriveName + " Drive Mapped" });
                return 0;
            }
            else return 1;
        }
        internal int UnMapDrive()
        {
            if (Directory.Exists(ProjConst.mapDriveName))
            {
                pDrv.LocalDrive = ProjConst.mapDriveName;    //slashes are removed in called method
                if (pDrv.UnMapDrive() == 0)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { ProjConst.mapDriveName + " Drive UnMapped" });
                    return 0;
                }
                else return 1;
            }
            else
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { ProjConst.mapDriveName + " Does not Exist, Unmapping not done" });
                return 1;
            }
        }

        internal int SetSymbolicLink()
        {
            if (thisProginst.removable && Directory.Exists(symLinkPath))
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link already exists - will not attempt creation" });
                return 0;
            }
            if (ProgramData.osVerMaj > 5)
            {//We have vista or W7
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Getting Admin permission to create Symbolic Link" });
                //get exe file out of assembly
                if (thisProginst.CopyFileFromThisAssembly("Tax-AideSymLink.exe", Environment.GetEnvironmentVariable("temp")) != 0) return 1;
                Process p = new Process();
                if (thisProginst.removable == true)
                    p.StartInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("temp") + "\\Tax-AideSymLink.exe", symLinkPath + " " + thisProginst.drvLetter + ":\\" + " Removable");
                else
                    p.StartInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("temp") + "\\Tax-AideSymLink.exe", symLinkPath + " " + thisProginst.drvLetter + ":\\" + " Desktop");  // drvletter not used for desktop but left in there          
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
            else
            {
                if (thisProginst.removable)
                {
                    if (thisProginst.CopyFileFromThisAssembly("junction.exe", Environment.GetEnvironmentVariable("temp")) != 0) return 1;
                    Process p = new Process();
                    p.StartInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("temp") + "\\junction.exe", symLinkPath + " " + thisProginst.drvLetter + ":\\" + " /accepteula");
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                    {
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Windows-XP junction created" });
                        DirectoryInfo dir = new DirectoryInfo(symLinkPath);
                        dir.Attributes |= FileAttributes.Hidden;
                        return 0;
                    }
                    else
                    {
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "ERROR - Cannot create Windows-XP junction" });
                        return 1;
                    } 
                }
                return 0;
            }
        }
        internal void DeleteSymLink()
        {
            if (ProgramData.osVerMaj > 5)
            {// Vista or Win 7
                if (Directory.Exists(symLinkPath))
                    System.IO.Directory.Delete(symLinkPath);
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Symbolic Link \"" + symLinkPath + "\" Deleted" }); 
            }
            else
            {
                //if (thisProginst.CopyFileFromThisAssembly("junction.exe", Environment.GetEnvironmentVariable("temp")) != 0) return 1;
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("temp") + "\\junction.exe", symLinkPath + " -d");
                p.Start();
                p.WaitForExit();
                if (p.ExitCode == 0)
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Windows-XP junction Deleted" });
                else
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "ERROR - Cannot delete Windows-XP junction" });
            }
        }


        #region Methods for Sharing and unsharing a folder
        internal int ShareFolder()
        {
            if (ProgramData.osVerMaj == 6 ) // Vista/Windows 7 will have been done by elevated symlink
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] {"Vista/Windows 7 " + ProjConst.shareName + " Share Created" });
                return 0;
            }
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == ProjConst.shareName)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "The" + ProjConst.shareName + " share already exists, trying to delete it in order to add the share correctly" });
                    try { shr.Delete(); }
                    catch (Exception e) 
                    {
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Exception when creating the " + ProjConst.shareName + " share. The message was \r\n " + e.Message });
                        return 1 ;
                    }
                }
            }
            if (ShareCreate(ProjConst.shareName, symLinkPath, "Tax-Aide Share") == 0)
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { ProjConst.shareName + " Share Created" });
                return 0;
            }
            else
                return 1;
        }
        private int ShareCreate(string shareName, string FolderPath, string Description)
        {
            ManagementClass mgmtClass = new ManagementClass("Win32_Share");
            ManagementBaseObject inParams = mgmtClass.GetMethodParameters("Create");
            ManagementBaseObject outParams;
            inParams["Description"] = Description;
            inParams["Name"] = shareName;
            inParams["Path"] = FolderPath;
            inParams["Type"] = 0x0; //disk drive
            inParams["Access"] = SecurityDescriptor();  //for Everyone full perms
            outParams = mgmtClass.InvokeMethod("Create", inParams, null);
            if ((uint)(outParams.Properties["ReturnValue"].Value) != 0)
            {
                string errCode = Enum.GetName(typeof(shareCreateErrorCodes), outParams.Properties["ReturnValue"].Value);
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { String.Format("Unable to create a network share. The error message was {0}\r\n\r\nshareName = {1}\r\nFolderPath = {2}", errCode, shareName, FolderPath) });
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

        public int DeleteShares()
        {
            if (ProgramData.osVerMaj > 5) //Vista or Win 7
            {//Vista delete share requires elevated permissions
                //get exe file out of assembly
                if (thisProginst.CopyFileFromThisAssembly("TaxAideDeleteShr.exe", Environment.GetEnvironmentVariable("temp")) != 0) return 1;
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("temp") + "\\TaxAideDeleteShr.exe");
                p.Start();
                p.WaitForExit();
                if (p.ExitCode == 0)
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Tax-Aide Vista/Windows 7 Share deleted" });
                    return 0;
                }
                else
                {
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "ERROR - Unable to delete Vista  Share" });
                    return 1;
                }
            }
            ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
            foreach (ManagementObject shr in shares)
            {
                if (shr.GetPropertyValue("Name").ToString() == ProjConst.shareName | shr.GetPropertyValue("Name").ToString() == ProjConst.shareNameLegacy)
                {
                    try { shr.Delete(); }
                    catch (Exception e) 
                    {
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Exception while deleting share. The error was \r\n" + e.Message });
                        return 1;
                    }
                }
            }
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { ProjConst.shareName + " Share Deleted" });
            return 0;
        }
        //internal void CheckUsersPublicShares()
        //{
        //    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Checking that Users and Public folders are shared, fixing if necessary." });
        //    bool usersExist = false;
        //    bool publicExist = false;
        //    ManagementObjectCollection shares = new ManagementClass("Win32_Share").GetInstances();
        //    foreach (ManagementObject shr in shares)
        //    {
        //        if (shr.GetPropertyValue("Name").ToString() == "Users")
        //            usersExist = true;
        //        if (shr.GetPropertyValue("Name").ToString() == "Public")
        //            publicExist = true;
        //    }
        //    if (!usersExist)
        //        ShareCreate("Users", folder2Share.Remove(folder2Share.Length - 8), "");
        //    if (!publicExist)
        //        ShareCreate("Public", folder2Share, "");
        //}
        
        #endregion


    }
}

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;


namespace TaxAideFlashShare
{
    class ProgramData
    {
        public static readonly string mbCaption = "AARP Tax-Aide P Drive Share";
        public const string symLinkName = "TASymLink";
        public static int osVerMaj;    //5=xp 6=vista or win7
        public static int osVerMin;    // vista = 0 win 7 = 1
        public string drvLetter; //Holds letter of drive on Script exe path
        public bool removable = false;  //will be set later if program running from usb drive
        public string scriptExePath = Assembly.GetEntryAssembly().CodeBase;    // format is, file:///D:/blah/blah.exe so 3 slashes then next 2 to get drive
        string thisProgName;
        string thisProgPath;
        string shrtCutName;
        [DllImport("kernel32.dll")]
        private static extern int GetModuleFileName(int handle, StringBuilder fileNameOut, uint nSize);

        public ProgramData()
        {
            thisProgName = Assembly.GetExecutingAssembly().GetName().Name;
            thisProgPath = Assembly.GetEntryAssembly().Location;   // has to be done here does not get assigned in XP if done earlier
            StringBuilder exeFileFullPath = new StringBuilder(256);
            GetModuleFileName(0, exeFileFullPath, (uint)256);
            shrtCutName = exeFileFullPath.ToString().Substring(exeFileFullPath.ToString().LastIndexOf("\\") + 1);
            scriptExePath = exeFileFullPath.ToString().Substring(0, exeFileFullPath.ToString().LastIndexOf("\\"));
            drvLetter = scriptExePath.Substring(0, 1);
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Testing for removable drive" });
            GetUSBDrivesSetRemovable();
            osVerMaj = Environment.OSVersion.Version.Major;
            osVerMin = Environment.OSVersion.Version.Minor;
            switch (osVerMaj)
            {
                case 5:
                    ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Running on Windows-XP" });
                    break;
                case 6:
                    if (osVerMin == 0)
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Running on Windows Vista" }); 
                    else
                        ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Running on Windows 7" });
                    break;
                default:
                    MessageBox.Show("Unknown OS, Exiting", mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    Environment.Exit(1);
                    break;
            }
        }

        private void GetUSBDrivesSetRemovable() 
        {
            ManagementClass logicalToPartition = new ManagementClass("Win32_LogicalDiskToPartition");
            ManagementClass partitionToDiskDrv = new ManagementClass("Win32_DiskDriveToDiskPartition");
            ManagementClass diskDrvs = new ManagementClass("Win32_DiskDrive");
            List<ManagementObject> usbDrvs = new List<ManagementObject>();
            List<ManagementObject> partitions = new List<ManagementObject>();
            //List<ManagementObject> logicalDrvs = new List<ManagementObject>();
            foreach (ManagementObject udrv in diskDrvs.GetInstances())
            {
                if (udrv.GetPropertyValue("PNPDeviceID").ToString().StartsWith("USBSTOR"))
                {
                    usbDrvs.Add(udrv);
                }
            }
            foreach (ManagementObject ud in usbDrvs)
            {
                foreach (ManagementObject parti in partitionToDiskDrv.GetInstances())
                {
                    if (parti.GetPropertyValue("Antecedent").ToString().Contains(ud.GetPropertyValue("DeviceID").ToString().Replace(@"\", @"\\")))
                    {
                        partitions.Add(parti);
                        break; //make sure only get one partition not 2
                    }
                }
            }
            foreach (ManagementObject partit in partitions)
            {
                foreach (ManagementObject logDrv in logicalToPartition.GetInstances())
                {
                    if (partit.GetPropertyValue("Dependent").ToString() == logDrv.GetPropertyValue("Antecedent").ToString())
                    {
                        //logicalDrvs.Add(logDrv);
                        //DrvInfo mydrive = new DrvInfo();
                        if (drvLetter == logDrv.GetPropertyValue("Dependent").ToString().Substring(logDrv.GetPropertyValue("Dependent").ToString().Length - 3, 1))
                        {
                            removable = true;
                        }
                        //DriveInfo drvInf = new DriveInfo(mydrive.drvName);
                        //mydrive.volName = drvInf.VolumeLabel;
                        //mydrive.combo = mydrive.drvName + " (" + mydrive.volName + ")";
                        //mydrive.tcFilePoss = string.Empty;
                        //travUSBDrv.Add(mydrive);
                    }
                }
            }
        }
        /// <summary> Intellisense for CopyFileFromThisAssembly
        /// Copies embedded file from this assembly
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="destPath"></param>
        /// <returns></returns>
        internal int CopyFileFromThisAssembly(string filename, string destPath)
        {
            string assemblyResourceLocation = thisProgName + ".Embedded.";
            //create a buffer originally had 2k but 64k speeded things up in utility
            int n = 0x800; //2048
            byte[] dataBuffer = new byte[n];
            try
            {
                string[] fss = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                using (Stream fsSource = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyResourceLocation + filename))
                {
                    using (FileStream fsNew = new FileStream(destPath + "\\" + filename, FileMode.Create, FileAccess.Write))
                    {
                        BinaryReader fsSourceBin = new BinaryReader(fsSource);
                        BinaryWriter fsNewBin = new BinaryWriter(fsNew);
                        while (n > 0)
                        {
                            n = fsSourceBin.Read(dataBuffer, 0, n);
                            fsNewBin.Write(dataBuffer, 0, n);
                        }
                        fsNewBin.Flush();// I would have thought using fixed this but one error on xp was fixed by this.
                    }
                }
            }
            catch (Exception e)
            {
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] {"Exception in Reflection File Copying is " + e.ToString()});
                return 1;
            }
            return 0;
        }
        internal void CreateShortcuts()
        {
            CopyFileFromThisAssembly("StopShare.ico", Environment.GetEnvironmentVariable("temp"));
            shelllink.ShellLink desktopShortcut = new shelllink.ShellLink();
            if (removable)
                desktopShortcut.ShortCutFile = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Delete TA FlashShare.lnk"; 
            else
                desktopShortcut.ShortCutFile = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Delete TA Share.lnk";
            desktopShortcut.Target = scriptExePath + "\\" + shrtCutName;
            desktopShortcut.Arguments = "/u";
            desktopShortcut.IconPath = Environment.GetEnvironmentVariable("temp") + "\\" + "StopShare.ico";
            desktopShortcut.Save();
            desktopShortcut.Dispose();
            if (removable)
            {
                shelllink.ShellLink flashShortcut = new shelllink.ShellLink();
                flashShortcut.ShortCutFile = scriptExePath + "\\Delete TA FlashShare.lnk";
                flashShortcut.Target = thisProgPath;
                flashShortcut.Arguments = "/u";
                flashShortcut.IconPath = thisProgPath;
                flashShortcut.Save();
                flashShortcut.Dispose(); 
            }
        }
    }
}

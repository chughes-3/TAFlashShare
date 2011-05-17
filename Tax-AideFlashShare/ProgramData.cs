using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Management;
using System.Reflection;
using System.Windows.Forms;

namespace Tax_AideFlashShare
{
    class ProgramData
    {
        public static readonly string mbCaption = "AARP Tax-Aide P Drive Share";
        public static int osVer;    //5=xp 6=vista or win7
        public string drvLetter; //Holds letter of drive on Script exe path
        public bool removable = false;  //will be set later if program running from usb drive
        public string scriptExePath = Assembly.GetEntryAssembly().CodeBase;    // format is, file:///D:/blah/blah.exe so 3 slashes then next 2 to get drive
        string patternPath = "(?<=///).+(?=/.*$)";  //matches d:/trav in file:///d:/trav/abc.exe
        //string pattern = "(?<=//)[a-zA-Z](?=:)";    //matches // followed by a letter followed by :
        public ProgramData()
        {
            Regex r = new Regex(patternPath);
            Match m = r.Match(scriptExePath);
            scriptExePath = m.Value.Replace("/", "\\");  //sets path to script directory
            drvLetter = scriptExePath.Substring(0, 1);
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "Testing for removable drive" });
            GetUSBDrivesSetRemovable();
            osVer = Environment.OSVersion.Version.Major;
            if (osVer != 5 && osVer != 6)
            {
                MessageBox.Show("Unknown OS, Exiting", mbCaption, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
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
    }
}

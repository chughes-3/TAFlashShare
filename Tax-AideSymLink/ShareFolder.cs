using System;
using System.Management;
using System.Security.Principal;
using System.Windows.Forms;

namespace Tax_AideSymLink
{
    class ShareFolder
    {
        enum shareCreateErrorCodes { Success = 0, AccessDenied = 2, UnknownFailure = 8, InvalidName = 9, InvalidLevel = 10, InvalidParameter = 21, DuplicateShare = 22, RedirectedPath = 23, UnknownDeviceorDirectory = 24, NetNameNotFound = 25 }

        #region Methods for Sharing and unsharing a folder
        internal int ShareCreate(string ShareName, string FolderPath, string Description)
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
                MessageBox.Show(String.Format("Unable to create a network share. The error message was {0}\n\nShareName = {1}\nFolderPath = {2}", errCode, ShareName, FolderPath) );
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

        #endregion
    }
}

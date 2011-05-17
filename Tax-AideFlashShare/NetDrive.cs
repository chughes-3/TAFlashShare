using System;
using System.Runtime.InteropServices;

namespace Tax_AideFlashShare
{
    enum WinNetConst : uint
    {
        //
        //  Network Resources.
        //  We are doing Unicode only - I hope!!!!

        RESOURCE_CONNECTED = 0x00000001,
        RESOURCE_GLOBALNET = 0x00000002,
        RESOURCE_REMEMBERED = 0x00000003,
        RESOURCE_RECENT = 0x00000004,
        RESOURCE_CONTEXT = 0x00000005,

        RESOURCETYPE_ANY = 0x00000000,
        RESOURCETYPE_DISK = 0x00000001,
        RESOURCETYPE_PRINT = 0x00000002,
        RESOURCETYPE_RESERVED = 0x00000008,
        RESOURCETYPE_UNKNOWN = 0xFFFFFFFF,

        RESOURCEUSAGE_CONNECTABLE = 0x00000001,
        RESOURCEUSAGE_CONTAINER = 0x00000002,
        RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
        RESOURCEUSAGE_SIBLING = 0x00000008,
        RESOURCEUSAGE_ATTACHED = 0x00000010,
        RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED),
        RESOURCEUSAGE_RESERVED = 0x80000000,

        RESOURCEDISPLAYTYPE_GENERIC = 0x00000000,
        RESOURCEDISPLAYTYPE_DOMAIN = 0x00000001,
        RESOURCEDISPLAYTYPE_SERVER = 0x00000002,
        RESOURCEDISPLAYTYPE_SHARE = 0x00000003,
        RESOURCEDISPLAYTYPE_FILE = 0x00000004,
        RESOURCEDISPLAYTYPE_GROUP = 0x00000005,
        RESOURCEDISPLAYTYPE_NETWORK = 0x00000006,
        RESOURCEDISPLAYTYPE_ROOT = 0x00000007,
        RESOURCEDISPLAYTYPE_SHAREADMIN = 0x00000008,
        RESOURCEDISPLAYTYPE_DIRECTORY = 0x00000009,
        RESOURCEDISPLAYTYPE_TREE = 0x0000000A,
        RESOURCEDISPLAYTYPE_NDSCONTAINER = 0x0000000B,

        //
        //  Network Connections.
        //

        NETPROPERTY_PERSISTENT = 1,

        CONNECT_UPDATE_PROFILE = 0x00000001,
        CONNECT_UPDATE_RECENT = 0x00000002,
        CONNECT_TEMPORARY = 0x00000004,
        CONNECT_INTERACTIVE = 0x00000008,
        CONNECT_PROMPT = 0x00000010,
        CONNECT_NEED_DRIVE = 0x00000020,
        CONNECT_REFCOUNT = 0x00000040,
        CONNECT_REDIRECT = 0x00000080,
        CONNECT_LOCALDRIVE = 0x00000100,
        CONNECT_CURRENT_MEDIA = 0x00000200,
        CONNECT_DEFERRED = 0x00000400,
        CONNECT_RESERVED = 0xFF000000,
        CONNECT_COMMANDLINE = 0x00000800,
        CONNECT_CMD_SAVECRED = 0x00001000,
        CONNECT_CRED_RESET = 0x00002000

    }

    class NetDrive
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct  netResourceStruct {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            public string sLocalName;  // may ned to be stringbuilder or a pointer unclear
            public string sRemoteName;
            public string sComment ;
            public string sProvider;
        }
        [DllImport("mpr.dll")] private static extern int WNetAddConnection2(ref netResourceStruct pstNetRes, string psPassword, string psUsername, uint piFlags);
		[DllImport("mpr.dll")] private static extern int WNetCancelConnection2(string psName, uint piFlags, int pfForce);
		[DllImport("mpr.dll")] private static extern int WNetConnectionDialog(int phWnd, uint piType);
		[DllImport("mpr.dll")] private static extern int WNetDisconnectDialog(int phWnd, uint piType);
		[DllImport("mpr.dll")] private static extern int WNetRestoreConnectionW(int phWnd, string psLocalDrive);

        #region Properties for setup prior to running net drive methods
		/// <summary>
		/// Option to save credentials are reconnection...
		/// </summary>
        private bool lf_SaveCredentials = false;
		public bool SaveCredentials{
			get{return(lf_SaveCredentials);}
			set{lf_SaveCredentials=value;}
		}
		private bool lf_Persistent = false;
		/// <summary>
		/// Option to reconnect drive after log off / reboot ...
		/// </summary>
		public bool Persistent{
			get{return(lf_Persistent);}
			set{lf_Persistent=value;}
		}
		private bool lf_Force = false;
		/// <summary>
		/// Option to force connection if drive is already mapped...
		/// or force disconnection if network path is not responding...
		/// </summary>
		public bool Force{
			get{return(lf_Force);}
			set{lf_Force=value;}
		}
		private bool ls_PromptForCredentials = false;
		/// <summary>
		/// Option to prompt for user credintals when mapping a drive
		/// </summary>
		public bool PromptForCredentials{
			get{return(ls_PromptForCredentials);}
			set{ls_PromptForCredentials=value;}
		}

		private string ls_Drive = "p:"; //Drive for mapping
		/// <summary>
		/// Drive to be used in mapping / unmapping...
		/// </summary>
		public string LocalDrive{
			get{return(ls_Drive);}
			set{
				if(value.Length>=1) {
					ls_Drive=value.Substring(0,1)+":";
				}else{
					ls_Drive="";
				}
			}
		}
		private string ls_ShareName = "\\\\Computer\\C$";
		/// <summary>
		/// Share address to map drive to - of form "\\\\ComputerName\\C$".
		/// </summary>
		public string ShareName{
			get{return(ls_ShareName);}
			set{ls_ShareName=value;}
		}

        #endregion

        #region External to class Methods and mapping to internal to class methods
        /// <summary>
        /// Map network drive
        /// </summary>
        public void MapDrive() { zMapDrive(null, null); }
        /// <summary>
        /// Map network drive (using supplied Password)
        /// </summary>
        public void MapDrive(string Password) { zMapDrive(null, Password); }
        /// <summary>
        /// Map network drive (using supplied Username and Password)
        /// </summary>
        public void MapDrive(string Username, string Password) { zMapDrive(Username, Password); }
        /// <summary>
        /// Unmap network drive
        /// </summary>
        public void UnMapDrive() { zUnMapDrive(this.lf_Force); }
        /// <summary>
        /// Check / restore persistent network drive
        /// </summary>
        public void RestoreDrives() { zRestoreDrive(); }
        /// <summary>
        /// Display windows dialog for mapping a network drive
        /// </summary>
        //public void ShowConnectDialog(Form ParentForm) { zDisplayDialog(ParentForm, 1); }
        /// <summary>
        /// Display windows dialog for disconnecting a network drive
        /// </summary>
        //public void ShowDisconnectDialog(Form ParentForm) { zDisplayDialog(ParentForm, 2); }

        #endregion


        #region Internal to Class Core functions

        // Map network drive
        private void zMapDrive(string psUsername, string psPassword)
        {
            //create struct data
            netResourceStruct stNetRes = new netResourceStruct();
            stNetRes.dwScope = (uint) WinNetConst.RESOURCE_GLOBALNET;
            stNetRes.dwType = (uint) WinNetConst.RESOURCETYPE_DISK;
            stNetRes.dwDisplayType = (uint) WinNetConst.RESOURCEDISPLAYTYPE_SHARE;
            stNetRes.dwUsage = (uint) WinNetConst.RESOURCEUSAGE_CONNECTABLE;
            stNetRes.sRemoteName = ls_ShareName;
            stNetRes.sLocalName = ls_Drive;
            //prepare params
            uint iFlags = 0;
            if (lf_SaveCredentials) { iFlags += (uint) WinNetConst.CONNECT_CMD_SAVECRED; }
            if (lf_Persistent) { iFlags += (uint) WinNetConst.CONNECT_UPDATE_PROFILE; }
            if (ls_PromptForCredentials) { iFlags += (uint) WinNetConst.CONNECT_INTERACTIVE + (uint) WinNetConst.CONNECT_PROMPT; }
            if (psUsername == "") { psUsername = null; }
            if (psPassword == "") { psPassword = null; }
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "shareName=" + ls_ShareName + ", lsDrive =" + ls_Drive+"," });
            //if force, unmap ready for new connection
            if (lf_Force) { try { zUnMapDrive(true); } catch { } }
            //call and return
            try
            {
                int i = WNetAddConnection2(ref stNetRes, psPassword, psUsername, iFlags);
                ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] { "i is = " + i.ToString() });
                if (i > 0) { throw new System.ComponentModel.Win32Exception(i); }
            }
            catch (System.ComponentModel.Win32Exception w)
            {
                GenMsg4Win32Ex(w);
                Environment.Exit(1);
            }
        }

        void GenMsg4Win32Ex(System.ComponentModel.Win32Exception w)
        {
            System.Exception e = w.GetBaseException();
            ProgOverallThread.progOverallWin.Invoke(ProgOverallThread.progressUpdate, new object[] {String.Format("Error on Mapping or UnMapping Drive\n\nMessage = {0}\n\nError Code = 0x{1:x}, Native Error Code = 0x{2:x}\nStack Trace = {3}\nSource = {4}\nBase Exception = {5}\n\nNetwork Share = {6}\nMap Drive = {7}", w.Message, w.ErrorCode, w.NativeErrorCode, w.StackTrace, w.Source, e.Message, ls_ShareName, ls_Drive)});
            System.Threading.Thread.Sleep(10000);
        }


        // Unmap network drive	
        private void zUnMapDrive(bool pfForce)
        {
            //call unmap and return
            uint iFlags = 0;
            if (lf_Persistent) { iFlags += (uint) WinNetConst.CONNECT_UPDATE_PROFILE; }
            int i = WNetCancelConnection2(ls_Drive, iFlags, Convert.ToInt32(pfForce));
            try
            {
                if (i != 0) i = WNetCancelConnection2(ls_ShareName, iFlags, Convert.ToInt32(pfForce));  //disconnect if localname was null
                if (i > 0) { throw new System.ComponentModel.Win32Exception(i); }
            }
            catch (System.ComponentModel.Win32Exception w)
            {
                GenMsg4Win32Ex(w); //If unmapping we want to continue to do rest of deletions
                //Environment.Exit(1);
            }
        }


        // Check / Restore a network drive
        private void zRestoreDrive()
        {
            //call restore and return
            int i = WNetRestoreConnectionW(0, null);
            if (i > 0) { throw new System.ComponentModel.Win32Exception(i); }
        }

        // Display windows dialog
        //private void zDisplayDialog(Form poParentForm, int piDialog)
        //{
        //    int i = -1;
        //    int iHandle = 0;
        //    //get parent handle
        //    if (poParentForm != null)
        //    {
        //        iHandle = poParentForm.Handle.ToInt32();
        //    }
        //    //show dialog
        //    if (piDialog == 1)
        //    {
        //        i = WNetConnectionDialog(iHandle, (uint) WinNetConst.RESOURCETYPE_DISK);
        //    }
        //    else if (piDialog == 2)
        //    {
        //        i = WNetDisconnectDialog(iHandle, (uint) WinNetConst.RESOURCETYPE_DISK);
        //    }
        //    if (i > 0) { throw new System.ComponentModel.Win32Exception(i); }
        //    //set focus on parent form
        //    poParentForm.BringToFront();
        //}


        #endregion

    }
}

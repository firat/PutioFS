using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PutioFS.Windows
{
    public partial class MainWindow : Form
    {
        public NotifyIcon notify_icon;
        public Dictionary<String, MenuItem> context_menu_items = new Dictionary<String, MenuItem>();
        public readonly ApplicationContext AppContext;

        //dbt.h and winuser.h
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVTYP_VOLUME = 0x0002;

        struct DEV_BROADCAST_HDR
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
        }

        struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size;
            public int dbcv_devicetype;
            public int dbcv_reserved;
            public int dbcv_unitmask;
            public int dbcv_flags;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.AppContext = new ApplicationContext();
        }


        public void GoInvisible()
        {
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(-2000, -2000);
            this.Size = new System.Drawing.Size(1, 1);
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.Visible = false;
        }

        private char GetDriveLetterFromMask(int unitmask)
        {
            int i =0;
            for (; i < 26; i++)
            {
                if ((unitmask & 1) != 0)
                    break;
                unitmask >>= 1;
            }

            return (char)((int)'A' + i);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE && m.WParam.ToInt32() == DBT_DEVICEARRIVAL)
            {
                DEV_BROADCAST_HDR DeviceInfo = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));
                if (DeviceInfo.dbch_devicetype == DBT_DEVTYP_VOLUME)
                {
                    DEV_BROADCAST_VOLUME Volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
                    char DriveLetter = GetDriveLetterFromMask(Volume.dbcv_unitmask);
                    if (DriveLetter == WinMount.drive_letter)
                    {
                        if (WinMount.Silent)
                            WinMount.Silent = false;
                        else
                            WinMount.ExploreDrive(null, null);
                    }
                }
            }

            base.WndProc(ref m);
        }


        public void InitializeTrayIcon()
        {
            System.IO.Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
                                 "WinMount.Resources.putio.ico");
            Icon putio_icon = new Icon(s);

            this.notify_icon = new NotifyIcon();
            this.notify_icon.Icon = putio_icon;
            this.notify_icon.ContextMenu = new ContextMenu();

            this.CreateMenuItem("Settings", WinMount.InvokeSettings);
            this.CreateMenuItem("Mount", WinMount.TryMount);
            this.CreateMenuItem("Unmount", WinMount.TryUnmount);
            this.CreateMenuItem("Purge cache", WinMount.PurgeCache);
            this.notify_icon.ContextMenu.MenuItems.Add(new MenuItem("-"));
            this.CreateMenuItem("Launch website", WinMount.LaunchWebsite);
            this.CreateMenuItem("Explore drive", WinMount.ExploreDrive);
            this.CreateMenuItem("Check for updates", WinMount.CheckForUpdates);
            this.notify_icon.ContextMenu.MenuItems.Add(new MenuItem("-"));
            this.CreateMenuItem("Exit", WinMount.ExitApplication);

            this.notify_icon.MouseClick += new MouseEventHandler(WinMount.RightClickHandler);
            this.notify_icon.Text = "Put.io Disk";
            this.Icon = putio_icon;
        }

        public void ShowLink(String label)
        {
            this.context_menu_items[label].Enabled = true;
        }

        public void HideLink(String label)
        {
            this.context_menu_items[label].Enabled = false;
        }

        public void SetCacheDisplay(long size)
        {
            this.context_menu_items["Purge cache"].Text = String.Format("Purge cache ({0} MB)", size);
        }

        public void CreateMenuItem(String label, EventHandler f)
        {
            if (this.context_menu_items.ContainsKey(label))
                throw new Exception("Menu item already exists!");
            MenuItem menu_item = new MenuItem();
            menu_item.Text = label;
            menu_item.Click += new EventHandler(f);
            this.context_menu_items.Add(label, menu_item);
            this.notify_icon.ContextMenu.MenuItems.Add(menu_item);
        }

        public void ToggleMountUnmount(bool Mounted)
        {
            if (Mounted)
            {
                this.HideLink("Mount");
                this.ShowLink("Unmount");
                this.ShowLink("Explore drive");
                this.HideLink("Purge cache");
            }
            else
            {
                this.ShowLink("Mount");
                this.HideLink("Unmount");
                this.HideLink("Explore drive");
                this.ShowLink("Purge cache");
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WinMount.Properties;

namespace PutioFS.Windows
{
    public partial class AuthForm : Form
    {
        public NotifyIcon notify_icon;
        public Dictionary<String, MenuItem> context_menu_items = new Dictionary<String, MenuItem>();        

        public AuthForm()
        {
            InitializeComponent();
            this.LoadUserInfo();
            this.InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            Icon putio_icon = new Icon(@"putio.ico");

            this.notify_icon = new NotifyIcon();
            this.notify_icon.Icon = putio_icon;
            this.notify_icon.ContextMenu = new ContextMenu();

            this.CreateMenuItem("Settings", this.InvokeSettings);
            this.CreateMenuItem("Mount", WinMount.TryMount);
            this.CreateMenuItem("Unmount", WinMount.TryUnmount);
            this.CreateMenuItem("Purge Cache", WinMount.PurgeCache);
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
            this.context_menu_items["Purge Cache"].Text = String.Format("Purge Cache ({0} MB)", size);
        }

        public void CreateMenuItem(String label,  EventHandler f)
        {
            if (this.context_menu_items.ContainsKey(label))
                throw new Exception("Menu item already exists!");
            MenuItem menu_item = new MenuItem();
            menu_item.Text = label;
            menu_item.Click += new EventHandler(f);
            this.context_menu_items.Add(label, menu_item);
            this.notify_icon.ContextMenu.MenuItems.Add(menu_item);
        }

        public void InvokeSettings(object sender, EventArgs e)
        {
            if (this.ShowDialog() == DialogResult.OK)
            {
                this.SaveUserInfo();
                if (!WinMount.Mounted)
                    WinMount.TryMount(null, null);
            }
        }

        public void SaveUserInfo()
        {
            Settings.Default.APIKey = this.LoginApiKey.Text;
            Settings.Default.Secret = this.LoginPutioSecret.Text;
            Settings.Default.Save();
        }

        public void LoadUserInfo()
        {
            this.LoginApiKey.Text = Settings.Default.APIKey;
            this.LoginPutioSecret.Text = Settings.Default.Secret;
        }

        public void ToggleMountUnmount(bool Mounted)
        {
            if (Mounted)
            {
                this.HideLink("Mount");
                this.ShowLink("Unmount");
                this.HideLink("Purge Cache");
            }
            else
            {
                this.ShowLink("Mount");
                this.HideLink("Unmount");
                this.ShowLink("Purge Cache");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void LoginPutioSecret_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

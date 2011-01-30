using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WinMount.Properties;
using Microsoft.Win32;

namespace PutioFS.Windows
{
    public partial class SettingsForm : Form
    {

        public static readonly String ExecPath = String.Format("{0} /silent", Application.ExecutablePath.ToString());
        public SettingsForm()
        {
            InitializeComponent();
            this.LoadUserInfo();
        }

        public void InvokeSettings()
        {
            if (this.ShowDialog() == DialogResult.OK)
            {
                this.SaveUserInfo();
                if (!WinMount.Mounted)
                    WinMount.TryMount(null, null);
            }
            else
                this.LoadUserInfo();
        }

        public void SaveUserInfo()
        {
            Settings.Default.APIKey = this.LoginApiKey.Text;
            Settings.Default.Secret = this.LoginPutioSecret.Text;
            RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            k.SetValue("PutioFS Windows Mounter", SettingsForm.ExecPath);
            if (!this.OpenAtLogin.Checked)
                k.DeleteValue("PutioFS Windows Mounter");

            Settings.Default.Save();
        }

        public void LoadUserInfo()
        {
            Settings.Default.Upgrade();
            this.LoginApiKey.Text = Settings.Default.APIKey;
            this.LoginPutioSecret.Text = Settings.Default.Secret;
            RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            object value = k.GetValue("PutioFS Windows Mounter");
            if (value != null && value.ToString() == SettingsForm.ExecPath)
                this.OpenAtLogin.Checked = true;
            else
                this.OpenAtLogin.Checked = false;
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


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;
using System.Xml;
using System.Reflection;
using System.Net;


using Putio;
using PutioFS.Core;
using WinMount.Properties;

namespace PutioFS.Windows
{
    
    class WinMount
    {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static SettingsForm settings_form;
        public static MainWindow main_window;
        public static Char drive_letter;
        public static bool Silent = true;
        public static bool Mounted = false;

        public static void CheckForUpdates(object Sender, EventArgs e)
        {
            XmlTextReader xml_reader = null;
            try
            {
                xml_reader = new XmlTextReader("http://versioncheck.putiofs.put.io/version.xml");

                xml_reader.MoveToContent();
                string element_name = null;
                string url;
                Version new_version = null;

                if (xml_reader.NodeType == XmlNodeType.Element && xml_reader.Name == "PutioFS")
                {
                    while (xml_reader.Read())
                    {
                        if (xml_reader.NodeType == XmlNodeType.Element)
                            element_name = xml_reader.Name;
                        if ((xml_reader.NodeType == XmlNodeType.Text) && xml_reader.HasValue)
                        {
                            switch (element_name)
                            {
                                case "version":
                                    new_version = new Version(xml_reader.Value);
                                    break;
                                case "url":
                                    url = xml_reader.Value;
                                    break;
                            }
                        }
                    }
                }

                Version current_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (current_version.CompareTo(new_version) < 0)
                {
                    MessageBox.Show("There is a new version");
                }
                else
                {
                    MessageBox.Show("No new updates.");
                }
            }
            catch
            {
                MessageBox.Show("There was a problem checking or updates.");
            }
            finally
            {
                if (xml_reader != null)
                    xml_reader.Close();
            }

        }

        public static void TryMount(object Sender, EventArgs e)
        {

            try
            {
                Api api = WinMount.GetApiInfoFromSettings();
                api.GetUserInfo();
                PutioFileSystem.CreateInstance(api);

                Thread dokan_thread = new Thread(PutioDokanOperations._DokanMount);
                dokan_thread.Start(drive_letter);
                Mounted = true;
                main_window.ToggleMountUnmount(Mounted);
            }
            catch (PutioException)
            {
                MessageBox.Show("Put.io user not found.");
                settings_form.InvokeSettings();
            }
            catch (Exception)
            {
                MessageBox.Show("Can not connect to Put.io");
                settings_form.InvokeSettings();
            }
            
        }

        public static void TryUnmount(object Sender, EventArgs e)
        {
            PutioDokanOperations._DokanUnmount(drive_letter);
            try
            {
                PutioFileSystem.GetInstance().CleanUp();
                PutioFileSystem.PurgeInstance();
            }
            catch { }
            Mounted = false;
            main_window.ToggleMountUnmount(Mounted);
        }

        public static void ExitApplication(object Sender, EventArgs e)
        {
            TryUnmount(null, null);
            Application.Exit();
        }

        public static void RightClickHandler(object Sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RefreshCacheSize();
            }
        }

        public static void PurgeCache(object Sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to purge the cached data?", "Confirm delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bool problem = false;
                foreach (DirectoryInfo di in new DirectoryInfo(Constants.LocalStoragePath).GetDirectories())
                {
                    foreach (FileInfo fi in di.GetFiles("*.pcd"))
                    {
                        try
                        {
                            fi.Delete();
                        }
                        catch (Exception)
                        {
                            problem = true;
                        }
                    }

                    foreach (FileInfo fi in di.GetFiles("*.pci"))
                    {
                        try
                        {
                            fi.Delete();
                        }
                        catch (Exception)
                        {
                            problem = true;
                        }
                    }
                }
                RefreshCacheSize();
                if (problem)
                    MessageBox.Show("Unable to delete some of the files. Please restart the Put.io virtual drive and try again.");
                else
                    MessageBox.Show("Cache is empty.");
            }
        }

        public static void LaunchWebsite(object Sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://put.io");
        }

        public static void ExploreDrive(object Sender, EventArgs e)
        {
            if (WinMount.Mounted)
                System.Diagnostics.Process.Start("explorer.exe", String.Format(@"{0}:\", WinMount.drive_letter));
        }

        public static void RefreshCacheSize()
        {
            main_window.SetCacheDisplay(GetCacheSize() / (1024 * 1024));
        }

        public static void InvokeSettings(object sender, EventArgs e)
        {
            WinMount.settings_form.InvokeSettings();
        }

        static long GetCacheSize()
        {
            if (!Directory.Exists(Constants.LocalStoragePath))
                return 0;

            long size = 0;
            foreach (DirectoryInfo di in new DirectoryInfo(Constants.LocalStoragePath).GetDirectories())
            {
                foreach (FileInfo fi in di.GetFiles("*.pcd"))
                {
                    size += fi.Length;
                }
            }
            return size;
        }

        static void CrashLogger(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                logger.Fatal("FATAL ERROR: {0}", ex.Message);
                logger.Fatal("Here is the stacktrace:");
                logger.Fatal(ex.StackTrace);
                if (MessageBox.Show("Would you like to send anonymous debug information to Put.io?", "PutioFS crashed.", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://api.put.io/v1/mounter");

                        ASCIIEncoding encoding = new ASCIIEncoding();
                        string postData = String.Format("method=log&api_key={0}&api_secret={1}&msg={2}", Settings.Default.APIKey, Settings.Default.Secret, ex.StackTrace);
                        byte[] data = encoding.GetBytes(postData);
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = data.Length;
                        Stream newStream = request.GetRequestStream();
                        newStream.Write(data, 0, data.Length);
                        newStream.Close();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("There was a problem sending the data.");
                        return;
                    }
                    MessageBox.Show("Debug information sent.");
                }

            }
            finally
            {
                Application.Exit();
            }
        }

        static Api GetApiInfoFromSettings()
        {
            return new Api(Settings.Default.APIKey, Settings.Default.Secret);
        }

        static Char GetAvailableDriveLetter(char Preferred)
        {
            Char[] drives = System.Environment.GetLogicalDrives().Select(x => x[0]).ToArray();
            if (!drives.Contains(Preferred))
                return Preferred;

            Preferred = 'A';
            while (drives.Contains(Preferred) && Preferred <= 'Z')
            {
                Preferred++;
            }
            if (Preferred <= 'Z')
                return Preferred;
            throw new Exception("Can not find available drive letter to mount.");
        }

        static void Main(string[] args)
        {

            if (Environment.Version.Major < 4)
            {
                MessageBox.Show("You need to install Microsoft .NET Framework Version 4 in order to use put.io mounter.");
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashLogger);
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            WinMount.settings_form = new SettingsForm();
            WinMount.main_window = new MainWindow();
            WinMount.main_window.InitializeTrayIcon();
            WinMount.main_window.GoInvisible();
            WinMount.drive_letter = GetAvailableDriveLetter('P');
            WinMount.Silent = args.Contains(@"/silent");

            try
            {
                TryMount(null, null);
                RefreshCacheSize();
                WinMount.main_window.notify_icon.Visible = true;
                Application.Run();
                WinMount.main_window.notify_icon.Visible = false;
            }
            finally
            {
                PutioDokanOperations._DokanUnmount(WinMount.drive_letter);
            }
        }
    }
}
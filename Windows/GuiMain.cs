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


using Putio;
using PutioFS.Core;
using WinMount.Properties;
namespace PutioFS.Windows
{
    class WinMount
    {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static AuthForm auth_form;
        public static bool Mounted = false;

        public static void TryMount(object Sender, EventArgs e)
        {

            try
            {
                Api api = WinMount.GetApiInfoFromSettings();
                api.GetUserInfo();
                PutioFileSystem.CreateInstance(api);

                Thread dokan_thread = new Thread(PutioDokanOperations._DokanMount);
                dokan_thread.Start();
                Mounted = true;
                auth_form.ToggleMountUnmount(Mounted);
            }
            catch (PutioException)
            {
                MessageBox.Show("Put.io user not found.");
                auth_form.InvokeSettings(null, null);
            }
            
        }

        public static void TryUnmount(object Sender, EventArgs e)
        {
            PutioDokanOperations._DokanUnmount();
            try
            {
                PutioFileSystem.GetInstance().CleanUp();
                PutioFileSystem.PurgeInstance();
            }
            catch { }
            Mounted = false;
            auth_form.ToggleMountUnmount(Mounted);
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

        public static void RefreshCacheSize()
        {
            auth_form.SetCacheDisplay(GetCacheSize() / (1024 * 1024));
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

        static void Main(string[] args)
        {

            if (Environment.Version.Major < 4)
            {
                MessageBox.Show("You need to install Microsoft .NET Framework Version 4 in order to use put.io mounter.");
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashLogger);
            WinMount.auth_form = new AuthForm();
            try
            {
                TryMount(null, null);
                RefreshCacheSize();
                auth_form.notify_icon.Visible = true;
                
                Application.Run();
                auth_form.notify_icon.Visible = false;
            }
            finally
            {
                PutioDokanOperations._DokanUnmount();
            }
        }
    }
}
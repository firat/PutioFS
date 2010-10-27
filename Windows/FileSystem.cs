using Dokan;
using Putio;
using PutioFS.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.IO.IsolatedStorage;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using WinMount.Properties;
using System.Diagnostics;

namespace PutioFS.Windows
{

    public class PutioFileSystem : DokanOperations
    {
        private PutioFolder root;
        private Api Api;

        public static List<String> GetPathElements(String path)
        {
            List<String> elements = new List<String>();
            String filename;
            while (path != null)
            {
                filename = Path.GetFileName(path);
                if (filename != "")
                    elements.Insert(0, filename);
                path = Path.GetDirectoryName(path);
            }
            return elements;
        }

        public PutioFileSystem(Api putio_api)
        {
            this.Api = putio_api;
            Item item = new Item();
            item.Name = @"\";
            item.Id = "0";
            item.IsDirectory = true;

            this.root = new PutioFolder(this.Api, item, null);
            this.root.GetFolders();
            
        }      

        private PutioFSItem FindPutioFSItem(String path)
        {
            PutioFolder result = this.root;
            foreach(String dir_name in PutioFileSystem.GetPathElements(Path.GetDirectoryName(path)))
            {
                result = result.GetFolder(dir_name);
                if (result == null)
                    return null;
            }

            String filename = Path.GetFileName(path);
            if (filename != "")
                return result.GetItem(filename);
            return (PutioFSItem)result;
        }

        private Dictionary<String, PutioFileHandle> GetFileHandles(DokanFileInfo info)
        {
            if (info.Context == null)
            {
                lock (info)
                {
                    if (info.Context == null)
                        info.Context = new Dictionary<String, PutioFileHandle>();
                }
            }

            return (Dictionary<String, PutioFileHandle>)info.Context;
        }
        public int CreateFile(String filename, FileAccess access, FileShare share,
            FileMode mode, FileOptions options, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("CreateFile: {0}, {1} by {2}", filename, access, info.ProcessId));
            if (access != FileAccess.Read)
            {
                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            PutioFSItem fs_item = this.FindPutioFSItem(filename);


            if (fs_item == null)
            {
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }

            if (fs_item.IsDirectory)
                info.IsDirectory = true;
            else
            {
                PutioFile putio_file = (PutioFile)fs_item;
                Dictionary<String, PutioFileHandle> file_handles = this.GetFileHandles(info);
                

                lock (info.Context)
                {
                    if (file_handles.ContainsKey(filename))
                    {
                        Debug.WriteLine("This handle is already opened.");
                        return -1;
                    }
                    file_handles.Add(filename, putio_file.Open());
                }
            }
            
            return 0;
        }

        public int OpenDirectory(String filename, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("OpenDirectory on {0} by {1}", filename, info.ProcessId));
            PutioFSItem folder = this.FindPutioFSItem(filename);
            if (folder != null && folder.IsDirectory)
                return 0;
            else
                return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int CreateDirectory(String filename, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("CreateDirectory on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int Cleanup(String filename, DokanFileInfo info)
        {
            lock (info)
            {
                IEnumerator<KeyValuePair<String, PutioFileHandle>> ie = this.GetFileHandles(info).GetEnumerator();
                while(ie.MoveNext())
                {
                    ie.Current.Value.Close();
                }

            }
            return 0;
        }

        public int CloseFile(String filename, DokanFileInfo info)
        {
            PutioFSItem item = this.FindPutioFSItem(filename);
            if (item == null)
                return DokanNet.ERROR_FILE_NOT_FOUND;
            if (item.IsDirectory)
                return 0;

            lock (info.Context)
            {
                Dictionary<String, PutioFileHandle> file_handles = this.GetFileHandles(info);
                if (!file_handles.ContainsKey(filename))
                {
                    Debug.WriteLine(String.Format("Trying to close a file that was not open: {0}", filename));
                    return -1;
                }

                file_handles[filename].Close();
                file_handles.Remove(filename);
            }
            return 0;
        }

        public int ReadFile(String filename, Byte[] buffer, ref uint readBytes,
           long offset, DokanFileInfo info)
        {
            try
            {
                readBytes = (uint)this.GetFileHandles(info)[filename].Read(buffer, offset, buffer.Length);
            }
            catch (KeyNotFoundException)
            {
                Debug.WriteLine("Trying to read from a closed file.");
                return -1;
            }
            return 0;
        }

        public int WriteFile(String filename, Byte[] buffer,
            ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("WriteFile on {0} by {1}", filename, info.ProcessId));
            return -DokanNet.ERROR_ACCESS_DENIED;
        }

        public int FlushFileBuffers(String filename, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("FlushFileBuffers on {0} by {1}", filename, info.ProcessId));
            return -DokanNet.ERROR_ACCESS_DENIED;
        }

        public int GetFileInformation(String filename, FileInformation fileinfo, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("GetFileInformation on {0} by {1}", filename, info.ProcessId));
            PutioFSItem item = this.FindPutioFSItem(filename);

            if (item == null)
            {
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }
            else
            {
                fileinfo.Attributes = FileAttributes.Offline | FileAttributes.NotContentIndexed | FileAttributes.ReadOnly;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                
            }

            if (item.IsDirectory)
            {
                fileinfo.Attributes = fileinfo.Attributes | FileAttributes.Directory;
                fileinfo.Length = 0;
            }
            else
            {
                fileinfo.Length = ((PutioFile)item).Size;
            }

            return 0;
        }

        public int FindFiles(String filename, ArrayList files, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("FindFiles on {0} by {1}", filename, info.ProcessId));
            PutioFSItem item = this.FindPutioFSItem(filename);

            if (item == null || !item.IsDirectory)
                return DokanNet.ERROR_PATH_NOT_FOUND;

            PutioFolder folder = (PutioFolder)item;
            foreach (PutioFolder f in folder.GetFolders())
            {
                FileInformation fi = new FileInformation();
                fi.FileName = f.Name;
                fi.Attributes = FileAttributes.Offline | FileAttributes.NotContentIndexed | FileAttributes.ReadOnly | FileAttributes.Directory;
                fi.CreationTime = DateTime.Now;
                fi.LastAccessTime = DateTime.Now;
                fi.LastWriteTime = DateTime.Now;
                fi.Length = 0;
                files.Add(fi);
            }

            foreach (PutioFile file in folder.GetFiles())
            {
                FileInformation fi = new FileInformation();
                fi.FileName = file.Name;
                fi.Attributes = FileAttributes.Offline | FileAttributes.NotContentIndexed | FileAttributes.ReadOnly;
                fi.CreationTime = DateTime.Now;
                fi.LastAccessTime = DateTime.Now;
                fi.LastWriteTime = DateTime.Now;
                fi.Length = file.Size;
                files.Add(fi);
            }

            return 0;
        }

        public int SetFileAttributes(String filename, FileAttributes attr, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("SetFileAttributes on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int SetFileTime(String filename, DateTime ctime,
                DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("SetFileTime on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int DeleteFile(String filename, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("DeleteFile on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int DeleteDirectory(String filename, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("DeleteDirectory on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int MoveFile(String filename, String newname, bool replace, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("MoveFile on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int SetEndOfFile(String filename, long length, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("SetEndOfFile on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int SetAllocationSize(String filename, long length, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("SetAllocationSize on {0} by {1}", filename, info.ProcessId));
            return -1;
        }

        public int LockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("LockFile on {0} by {1}", filename, info.ProcessId));
            return 0;
        }

        public int UnlockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("UnlockFile on {0} by {1}", filename, info.ProcessId));
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes,
            ref ulong totalFreeBytes, DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("GetDiskFreeSpace by {0}", info.ProcessId));
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            // Debug.WriteLine(String.Format("Unmount by {0}", info.ProcessId));
            return 0;
        }
    }

    class WinMount
    {
        public static Api putio_api;
        public static NotifyIcon notify_icon;
        public static Dictionary<String, MenuItem> context_menu_items = new Dictionary<String, MenuItem>();
        public static String log_file = Path.Combine(Constants.LocalStoragePath, "..", "put.io.log");

        static void _DokanMount()
        {
            DokanOptions opt = new DokanOptions();
            opt.DebugMode = false;
            opt.DriveLetter = 'p';
            opt.ThreadCount = 5;
            opt.VolumeLabel = "put.io";
            // opt.NetworkDrive = true;
            DokanNet.DokanMain(opt, new PutioFileSystem(WinMount.putio_api));
        }

        static void _Navigator()
        {
            while (!Directory.Exists(@"P:\"))
            {
                Thread.Sleep(50);
            }
            System.Diagnostics.Process.Start("explorer.exe", "P:\\");
        }

        static void TryMount(object Sender, EventArgs e)
        {
            Thread dokan_thread = new Thread(_DokanMount);
            dokan_thread.Start();
            context_menu_items["mount_link"].Visible = false;
            context_menu_items["unmount_link"].Visible = true;
            context_menu_items["purge_cache"].Enabled = false;
            new Thread(_Navigator).Start();
        }

        static void TryUnmount(object Sender, EventArgs e)
        {
            DokanNet.DokanUnmount('p');
            context_menu_items["mount_link"].Visible = true;
            context_menu_items["unmount_link"].Visible = false;
            context_menu_items["purge_cache"].Enabled = true;
        }

        static void ExitApplication(object Sender, EventArgs e)
        {
            WinMount.TryUnmount(null, null);
            Application.Exit();
        }

        static void RightClickHandler(object Sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                WinMount.RefreshCacheSize();
            }
        }

        static void PurgeCache(object Sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to purge the cached data?", "Confirm delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                WinMount.TryUnmount(null, null);
                foreach (FileInfo fi in new DirectoryInfo(Constants.LocalStoragePath).GetFiles("*.ptc"))
                {
                    fi.Delete();
                }
                RefreshCacheSize();
            }
        }

        static void RefreshCacheSize()
        {
            context_menu_items["purge_cache"].Text = String.Format("Purge Cache ({0} MB)", WinMount.GetCacheSize() / (1024 * 1024));
        }

        static long GetCacheSize()
        {
            long size = 0;
            foreach (FileInfo fi in new DirectoryInfo(Constants.LocalStoragePath).GetFiles("*.ptc"))
            {
                size += fi.Length;
            }
            return size;
        }
        
        static void Main(string[] args)
        {
            if (Environment.Version.Major < 4)
            {
                MessageBox.Show("You need to install Microsoft .NET Framework Version 4 in order to use put.io mounter.");
                return;
            }

            Debug.WriteLine(Constants.LocalStoragePath);
            if (!Directory.Exists(Constants.LocalStoragePath))
            {
                Directory.CreateDirectory(Constants.LocalStoragePath);
            }

            AuthForm f = new AuthForm();
            bool logged_in = false;

            while (!logged_in)
            {
                try
                {
                    Icon putio_icon = new Icon(@"putio.ico");
                    f.Icon = putio_icon;
                    f.LoginApiKey.Text = Settings.Default.APIKey;
                    if (f.LoginApiKey.Text != "")
                    {
                        f.ActiveControl = f.LoginPutioSecret;
                    }
                    if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        Settings.Default.APIKey = f.LoginApiKey.Text;
                        Settings.Default.Save();
                        putio_api = new Api(Settings.Default.APIKey, f.LoginPutioSecret.Text);

                        putio_api.GetUserInfo();
                        logged_in = true;


                        notify_icon = new NotifyIcon();
                        notify_icon.Icon = putio_icon;
                        notify_icon.ContextMenu = new ContextMenu();
                        MenuItem mount_link = new MenuItem();
                        mount_link.Text = "Mount";
                        mount_link.Click += new EventHandler(WinMount.TryMount);

                        MenuItem unmount_link = new MenuItem();
                        unmount_link.Text = "Unmount";
                        unmount_link.Click += new EventHandler(WinMount.TryUnmount);
                        unmount_link.Visible = false;

                        MenuItem quit_link = new MenuItem();
                        quit_link.Text = "Exit";
                        quit_link.Click += new EventHandler(WinMount.ExitApplication);

                        MenuItem purge_cache = new MenuItem();
                        purge_cache.Click += new EventHandler(WinMount.PurgeCache);

                        context_menu_items.Add("mount_link", mount_link);
                        context_menu_items.Add("unmount_link", unmount_link);
                        context_menu_items.Add("purge_cache", purge_cache);
                        WinMount.RefreshCacheSize();
                        context_menu_items.Add("quit_link", quit_link);

                        notify_icon.ContextMenu.MenuItems.Add(mount_link);
                        notify_icon.ContextMenu.MenuItems.Add(unmount_link);
                        notify_icon.ContextMenu.MenuItems.Add(purge_cache);
                        notify_icon.ContextMenu.MenuItems.Add(new MenuItem("-"));
                        notify_icon.ContextMenu.MenuItems.Add(quit_link);

                        notify_icon.MouseClick += new MouseEventHandler(WinMount.RightClickHandler);


                        notify_icon.Text = "Put.io Disk";
                        notify_icon.Visible = true;
                        WinMount.TryMount(null, null);
                        Application.Run();
                        notify_icon.Visible = false;

                    }
                    else
                    {
                        break;
                    }
                }
                catch (PutioException)
                {
                    MessageBox.Show("Put.io user not found.");
                }
                finally
                {
                    DokanNet.DokanUnmount('p');
                }
            }
        }
    }
}

using Dokan;
using Putio;
using PutioFS.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Threading;
using System.Diagnostics;

using WinMounter.Properties;

namespace PutioFS.Windows
{

    public class PutioDokanOperations : DokanOperations
    {
        public static String log_file = Path.Combine(Constants.LocalStoragePath, "..", "put.io.log");
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly WinMounter Mounter;

        public static void _DokanMount(object _wm)
        {
            WinMounter wm = (WinMounter)_wm;
            DokanOptions opt = new DokanOptions();
            PutioDokanOperations ops = new PutioDokanOperations(wm);
            opt.DebugMode = false;
            opt.DriveLetter = ops.Mounter.DriveLetter;
            opt.ThreadCount = 5;
            opt.VolumeLabel = "put.io";
            // opt.NetworkDrive = true;
            DokanNet.DokanMain(opt, ops);
        }

        public static void _DokanUnmount(Char drive_letter)
        {
            DokanNet.DokanUnmount(drive_letter);
        }

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

        public PutioDokanOperations(WinMounter wm)
        {
            this.Mounter = wm;
        }      

        private PutioFsItem FindPutioFSItem(String path)
        {
            // TODO: Remove this workaround:
            if (Path.GetFileName(path) == "desktop.ini")
                return null;

            PutioFolder result = this.Mounter.PutioFileSystem.Root;
            foreach (String dir_name in PutioDokanOperations.GetPathElements(Path.GetDirectoryName(path)))
            {
                result = result.GetFolder(dir_name);
                if (result == null)
                    return null;
            }

            String filename = Path.GetFileName(path);
            if (filename != "")
                return result.GetItem(filename);
            return (PutioFsItem)result;
        }

        public int CreateFile(String filename, FileAccess access, FileShare share,
            FileMode mode, FileOptions options, DokanFileInfo info)
        {
            lock (info)
            {
                if (access != FileAccess.Read)
                {
                    return -DokanNet.ERROR_ACCESS_DENIED;
                }
                

                PutioFsItem fs_item = this.FindPutioFSItem(filename);

                if (fs_item == null)
                {
                    return -DokanNet.ERROR_FILE_NOT_FOUND;
                }

                if (fs_item.IsDirectory)
                {
                    info.IsDirectory = true;
                }
                else
                {
                    PutioFile putio_file = (PutioFile)fs_item;
                    // if (putio_file.ReachedHandleLimit())
                    //    return DokanNet.ERROR_SHARING_VIOLATION;
                    PutioFileHandle handle = putio_file.Open();
                    Guid handle_ref = Guid.NewGuid();
                    this.Mounter.PutioFileSystem.AddHandle(handle_ref, putio_file.Open());
                    info.Context = handle_ref;
                    logger.Debug("CreateFile: {0} - {1}", filename, handle_ref);
                }

                return 0;
            }
        }

        public int OpenDirectory(String filename, DokanFileInfo info)
        {
            logger.Debug("OpenDirectory on {0} by {1}", filename, info.ProcessId);
            PutioFsItem folder = this.FindPutioFSItem(filename);
            if (folder != null && folder.IsDirectory)
                return 0;
            else
                return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int CreateDirectory(String filename, DokanFileInfo info)
        {
            logger.Debug("CreateDirectory on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int Cleanup(String filename, DokanFileInfo info)
        {
            //logger.Debug("CleanUp on {0} by {1}", filename, info.ProcessId);
            return 0;
        }

        public int CloseFile(String filename, DokanFileInfo info)
        {
            if (info.IsDirectory)
                return 0;

            lock (info)
            {
                PutioFsItem item = this.FindPutioFSItem(filename);
                if (item == null)
                    return DokanNet.ERROR_FILE_NOT_FOUND;
                if (item.IsDirectory)
                    return 0;


                Guid handle_ref = (Guid)info.Context;
                this.Mounter.PutioFileSystem.GetHandle(handle_ref).Close();
                this.Mounter.PutioFileSystem.RemoveHandle(handle_ref);
                logger.Debug("CloseFile on {0} - {1}", filename, handle_ref);
                return 0;
            }
        }

        public int ReadFile(String filename, Byte[] buffer, ref uint readBytes,
           long offset, DokanFileInfo info)
        {
            lock (info)
            {
                Guid handle_ref = (Guid)info.Context;
                logger.Debug("ReadFile {0} bytes from {1}", buffer.Length, offset);
                try
                {
                    PutioFileHandle handle = this.Mounter.PutioFileSystem.GetHandle(handle_ref);
                    if (this.FindPutioFSItem(filename) == null)
                        return DokanNet.ERROR_FILE_NOT_FOUND;
                    if (offset != handle.Position)
                        handle.Seek(offset);
                    readBytes = (uint)handle.Read(buffer, 0, buffer.Length);
                }
                catch (KeyNotFoundException)
                {
                    logger.Debug("Trying to read from a closed file.");
                    return -1;
                }
                return 0;
            }
        }

        public int WriteFile(String filename, Byte[] buffer,
            ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            logger.Debug("WriteFile on {0} by {1}", filename, info.ProcessId);
            return -DokanNet.ERROR_ACCESS_DENIED;
        }

        public int FlushFileBuffers(String filename, DokanFileInfo info)
        {
            logger.Debug("FlushFileBuffers on {0} by {1}", filename, info.ProcessId);
            return -DokanNet.ERROR_ACCESS_DENIED;
        }

        public int GetFileInformation(String filename, FileInformation fileinfo, DokanFileInfo info)
        {
            // logger.Debug("GetFileInformation on {0} by {1}", filename, info.ProcessId);
            PutioFsItem item = this.FindPutioFSItem(filename);

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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            logger.Debug("FindFiles on {0} by {1}", filename, info.ProcessId);
            PutioFsItem item = this.FindPutioFSItem(filename);

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

            sw.Stop();
            logger.Debug("FindFiles took {0} ms. on {1}", sw.ElapsedMilliseconds, filename);
            return 0;
        }

        public int SetFileAttributes(String filename, FileAttributes attr, DokanFileInfo info)
        {
            logger.Debug("SetFileAttributes on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int SetFileTime(String filename, DateTime ctime,
                DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            logger.Debug("SetFileTime on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int DeleteFile(String filename, DokanFileInfo info)
        {
            logger.Debug("DeleteFile on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int DeleteDirectory(String filename, DokanFileInfo info)
        {
            logger.Debug("DeleteDirectory on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int MoveFile(String filename, String newname, bool replace, DokanFileInfo info)
        {
            logger.Debug("MoveFile on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int SetEndOfFile(String filename, long length, DokanFileInfo info)
        {
            logger.Debug("SetEndOfFile on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int SetAllocationSize(String filename, long length, DokanFileInfo info)
        {
            logger.Debug("SetAllocationSize on {0} by {1}", filename, info.ProcessId);
            return -1;
        }

        public int LockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            logger.Debug("LockFile on {0} by {1}", filename, info.ProcessId);
            return 0;
        }

        public int UnlockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            logger.Debug("UnlockFile on {0} by {1}", filename, info.ProcessId);
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes,
            ref ulong totalFreeBytes, DokanFileInfo info)
        {
            logger.Debug(String.Format("GetDiskFreeSpace by {0}", info.ProcessId));
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            logger.Debug("Unmount by {0}", info.ProcessId);
            return 0;
        }
    }
}

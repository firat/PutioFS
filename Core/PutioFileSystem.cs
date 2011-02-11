using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Putio;

namespace PutioFS.Core
{
    public class PutioFileSystem
    {
        public readonly Api PutioApi;
        private Dictionary<Guid, PutioFileHandle> OpenHandles;
        public readonly DownloadManager DownloadManager;
        public readonly PutioFolder Root;

        public int NumOpenHandles { get { return this.OpenHandles.Count; } }

        public PutioFileSystem(Api putio_api)
        {
            this.PutioApi = putio_api;
            this.OpenHandles = new Dictionary<Guid, PutioFileHandle>();
            this.Root = PutioFolder.GetRootFolder(this);
            this.DownloadManager = new DownloadManager();
        }

        public void AddHandle(Guid handle_ref, PutioFileHandle handle)
        {
            this.OpenHandles.Add(handle_ref, handle);
        }

        public PutioFileHandle GetHandle(Guid handle_ref)
        {
            return this.OpenHandles[handle_ref];
        }

        public void RemoveHandle(Guid handle_ref)
        {
            this.OpenHandles.Remove(handle_ref);
        }

        public void CleanUp()
        {
            foreach(PutioFileHandle h in this.OpenHandles.Values)
            {
                h.Close();
            }
        }      
    }
}

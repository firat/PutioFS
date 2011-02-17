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
            this.DownloadManager = new DownloadManager(Constants.MAX_CONNECTIONS);
        }

        public void AddHandle(PutioFileHandle handle)
        {
            this.OpenHandles.Add(handle.Guid, handle);
        }

        public PutioFileHandle GetHandleByGuid(Guid handle_guid)
        {
            return this.OpenHandles[handle_guid];
        }

        public void RemoveHandle(PutioFileHandle handle)
        {
            this.OpenHandles.Remove(handle.Guid);
        }

        public Boolean IsValidHandle(PutioFileHandle handle)
        {
            return this.OpenHandles.ContainsKey(handle.Guid);
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

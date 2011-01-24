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
        private static PutioFileSystem Instance;
        private static object InstanceLock = new object();

        public static void CreateInstance(Api putio_api)
        {
            lock (PutioFileSystem.InstanceLock)
            {
                if (Instance == null)
                    Instance = new PutioFileSystem(putio_api);
                else
                    throw new Exception("FileSystem already created.");
            }
        }

        public static void PurgeInstance()
        {
            lock (PutioFileSystem.InstanceLock)
            {
                if (Instance == null)
                    return;
                Instance = null;
            }
        }

        public static PutioFileSystem GetInstance()
        {
            if (Instance == null)
                throw new Exception("Create a PutioFileSystem Instance with a valid API.");
            return Instance;
        }


        public readonly Api PutioApi;
        private Dictionary<Guid, PutioFileHandle> OpenHandles;
        private PutioFolder Root;

        public int NumOpenHandles { get { return this.OpenHandles.Count; } }
        private PutioFileSystem(Api putio_api)
        {
            this.PutioApi = putio_api;
            this.OpenHandles = new Dictionary<Guid, PutioFileHandle>();
            Item item = new Item();
            item.Id = "0";
            item.Name = "";
            item.IsDirectory = true;
            this.Root = new PutioFolder(new PutioFsApiDataProvider(item), null);            
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

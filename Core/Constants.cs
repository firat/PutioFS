using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PutioFS.Core
{
    public class Constants
    {
        public const int CHUNK_READ_SIZE = 4096 * 32;
        public const int CHUNK_TOLERANCE = CHUNK_READ_SIZE * 2;
        public static readonly char[] ILLEGAL_CHARACTERS = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        public static readonly String LocalStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "put.io", "Cache");
    }
}

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PutioFS.Core;

namespace PutioFSTest
{
    [TestClass]
    public class RangeBufferTest
    {
        public static string test_local_file = @"C:/Users/firat/ApacheRoot/660.avi";

        private FileInfo Finfo;
        private PutioFile RemoteFile;
        private LocalFileCache Cache;

        [TestInitialize]
        public void Initialize()
        {
            this.Finfo = new FileInfo(test_local_file);
            this.RemoteFile = new PutioFile(new PutioFsTestDataProvider(this.Finfo.Name, this.Finfo.FullName, 660, this.Finfo.Length, false, Constants.LocalStoragePath), null);
            this.Cache = new LocalFileCache(this.RemoteFile);
        }

        [TestCleanup]
        public void Cleanup()
        {
        }
        
        [TestMethod]
        public void GetSimpleBufferRangeTest()
        {
            this.Cache.RangeCollection.AddRange(0, 500);
            LongRange range_actual = this.Cache.GetNextBufferRange(0);
            LongRange range_expected = new LongRange(500, this.RemoteFile.Size);
            Assert.AreEqual(0, range_actual.CompareTo(range_expected));
        }

        [TestMethod]
        public void GetBufferRangeTest()
        {
            this.Cache.RangeCollection.AddRange(0, 500);
            this.Cache.RangeCollection.AddRange(750, 1500);
            this.Cache.RangeCollection.AddRange(14323, 15630);

            LongRange range_actual;
            LongRange range_expected;

            range_actual = this.Cache.GetNextBufferRange(500);
            range_expected = new LongRange(500, 750);
            Assert.AreEqual(0, range_actual.CompareTo(range_expected));

            range_actual = this.Cache.GetNextBufferRange(560);
            range_expected = new LongRange(560, 750);
            Assert.AreEqual(0, range_actual.CompareTo(range_expected));

            range_actual = this.Cache.GetNextBufferRange(1120);
            range_expected = new LongRange(1500, 14323);
            Assert.AreEqual(0, range_actual.CompareTo(range_expected));

            range_actual = this.Cache.GetNextBufferRange(14859);
            range_expected = new LongRange(15630, this.RemoteFile.Size);
            Assert.AreEqual(0, range_actual.CompareTo(range_expected));

            range_actual = this.Cache.GetNextBufferRange(20000);
            range_expected = new LongRange(20000, this.RemoteFile.Size);
            Assert.AreEqual(0, range_actual.CompareTo(range_expected));
        }


    }
}

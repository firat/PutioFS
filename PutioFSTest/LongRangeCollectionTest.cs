using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PutioFS.Core;

namespace PutioFSTest
{
    [TestClass]
    public class LongRangeCollectionTest
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RangeConstructorTest()
        {
            LongRange range = new LongRange(697080622, 697080622 + 1);
            range.End = 697080622;

        }

        [TestMethod]
        public void BisectEmpty()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 500);
            Assert.AreEqual(0, lrc.Bisect(120));
        }

        [TestMethod]
        public void BisectSingle()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 500);
            lrc.AddRange(0, 100);
            Assert.AreEqual(1, lrc.Bisect(120));
        }

        [TestMethod]
        public void MergeConsequent()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 10000);
            lrc.AddRange(50, 100);
            lrc.AddRange(100, 200);
            Assert.AreEqual(lrc.RangeSet.Count, 1);
            Assert.AreEqual(lrc.RangeSet.ElementAt(0).Start, 50);
            Assert.AreEqual(lrc.RangeSet.ElementAt(0).End, 200);
        }

        [TestMethod]
        public void BisectMultiple()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 500000);
            lrc.AddRange(50, 100);
            lrc.AddRange(150, 200);
            Assert.AreEqual(0, lrc.Bisect(25));
            Assert.AreEqual(1, lrc.Bisect(120));
            Assert.AreEqual(2, lrc.Bisect(300));
        }

        [TestMethod]
        public void SingleAdd()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 5454545);
            lrc.AddRange(0, 100);
            Assert.AreEqual(0, lrc.RangeSet.ElementAt(0).Start);
            Assert.AreEqual(100, lrc.RangeSet.ElementAt(0).End);
        }

        [TestMethod]
        public void MultipleAdd()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 13123123);
            lrc.AddRange(150, 200);
            lrc.AddRange(0, 100);
            Assert.AreEqual(0, lrc.RangeSet.ElementAt(0).Start);
            Assert.AreEqual(100, lrc.RangeSet.ElementAt(0).End);
        }

        [TestMethod]
        public void Overlap()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 123213123);
            lrc.AddRange(150, 200);
            lrc.AddRange(0, 100);
            lrc.AddRange(80, 170);
            lrc.AddRange(500, 1403);
            lrc.AddRange(1000, 1500);
            Assert.AreEqual(2, lrc.RangeSet.Count);
            Assert.AreEqual(0, lrc.RangeSet.ElementAt(0).Start);
            Assert.AreEqual(200, lrc.RangeSet.ElementAt(0).End);
            Assert.AreEqual(500, lrc.RangeSet.ElementAt(1).Start);
            Assert.AreEqual(1500, lrc.RangeSet.ElementAt(1).End);
        }

        [TestMethod]
        public void MergeAll()
        {
            LongRangeCollection lrc = new LongRangeCollection(0, 34243);
            lrc.AddRange(150, 200);
            lrc.AddRange(0, 100);
            lrc.AddRange(80, 170);
            lrc.AddRange(500, 1403);
            lrc.AddRange(0, 2000);
            Assert.AreEqual(lrc.RangeSet.Count, 1);
            Assert.AreEqual(lrc.RangeSet.ElementAt(0).Start, 0);
            Assert.AreEqual(lrc.RangeSet.ElementAt(0).End, 2000);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PutioFS.Core
{
    public class LongRangeCollection
    {
        public SortedSet<LongRange> RangeSet;
        public readonly long Min;
        public readonly long Max;

        public LongRangeCollection(long min, long max)
        {
            this.Min = min;
            this.Max = max;
            this.RangeSet = new SortedSet<LongRange>();
        }

        public LongRangeCollection Clone()
        {
            LongRangeCollection lrc = new LongRangeCollection(this.Min, this.Max);
            lock (this.RangeSet)
            {
                foreach (LongRange lr in this.RangeSet)
                    lrc.AddRange(lr.Start, lr.End);
            }
            return lrc;
        }

        /// <summary>
        /// Add the given range to the current collection.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void AddRange(long start, long end)
        {
            lock (this.RangeSet)
            {
                int start_index = this.BinaryIndexSearch(start);
                if (start_index < 0)
                    start_index = this.Bisect(start);

                int end_index = this.BinaryIndexSearch(end);
                if (end_index < 0)
                    end_index = this.Bisect(end);

                this.RangeSet.Add(new LongRange(start, end));

                start_index = Math.Max(0, start_index - 1);
                end_index = Math.Min(end_index + 1, this.RangeSet.Count - 1);

                for (int i = start_index; i < end_index; i++)
                {
                    if (this.RangeSet.ElementAt(i).TryMerge(this.RangeSet.ElementAt(i + 1)))
                    {
                        this.RangeSet.Remove(this.RangeSet.ElementAt(i + 1));
                        i--;
                        end_index--;
                    }
                }
            }

        }

        /// <summary>
        /// Find the index where this value would fit in if it
        /// was in the range collection. The value should not already
        /// be in the collection.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int Bisect(long value)
        {
            lock (this.RangeSet)
            {
                int left = 0;
                int right = this.RangeSet.Count;
                int current = current = (left + right) / 2;
                while (right > left)
                {
                    if (this.RangeSet.ElementAt(current).Contains(value))
                        throw new Exception("The value is already in the collection.");

                    if (this.RangeSet.ElementAt(current).Start > value)
                        right = current;
                    else if (this.RangeSet.ElementAt(current).End <= value)
                        left = current + 1;
                    current = (left + right) / 2;
                }

                return current;
            }
        }

        /// <summary>
        /// Do a binary search, returning the index of the LongRange
        /// containing the value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int BinaryIndexSearch(long value)
        {
            lock (this.RangeSet)
            {
                int left = 0;
                int right = this.RangeSet.Count;
                int current;
                while (right > left)
                {
                    current = (left + right) / 2;
                    if (this.RangeSet.ElementAt(current).Contains(value))
                        return current;

                    if (this.RangeSet.ElementAt(current).Start > value)
                        right = current;
                    else if (this.RangeSet.ElementAt(current).End <= value)
                        left = current + 1;
                }

                return -1;
            }
        }

        /// <summary>
        ///  Find and return the Range that includes the given value.
        ///  Returns null no range includes the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LongRange BinarySearch(long value)
        {
            lock (this.RangeSet)
            {
                int left = 0;
                int right = this.RangeSet.Count;
                int current;
                while (right > left)
                {
                    current = (left + right) / 2;
                    if (this.RangeSet.ElementAt(current).Contains(value))
                        return this.RangeSet.ElementAt(current);

                    if (this.RangeSet.ElementAt(current).Start > value)
                        right = current;
                    else if (this.RangeSet.ElementAt(current).End <= value)
                        left = current + 1;
                }

                return null;
            }
        }

        /// <summary>
        /// Checks if the given range is in the collection.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Boolean Contains(long start, long end)
        {
            LongRange r = this.BinarySearch(start);

            if (r == null)
                return false;

            return end <= r.End;
        }
    }
}

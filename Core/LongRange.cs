using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PutioFS.Core
{
    /// <summary>
    /// A class to represent a range of long values. Start is
    /// inclusive, End is exclusive.
    /// </summary>
    public class LongRange : IComparable
    {
        private long _Start;
        private long _End;

        public long Start
        {
            get
            {
                return this._Start;
            }

            set
            {
                if (value >= this.End)
                    throw new Exception("Start value of the Range can not be greater than the end value.");
                this._Start = value;
            }
        }

        public long End
        {
            get
            {
                return this._End;
            }

            set
            {
                if (value <= this.Start)
                    throw new Exception("End value of the Range can't be smaller than the start value.");
                this._End = value;
            }
        }

        /// <summary>
        ///  Never ever let a LongRange with an end value that
        ///  is smaller than or equal to its start value
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public LongRange(long start, long end)
        {
            if (end <= start)
                throw new Exception("Can't create a LongRange with an end value that is smaller than or equal to its start value.");
            this._Start = start;
            this._End = end;
        }

        /// <summary>
        /// First compare the Start values, if they are equal,
        /// compare End values.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            LongRange other_range;
            try
            {
                other_range = (LongRange)obj;
            }
            catch
            {
                throw new Exception("Unable to compare a LongRange to another type.");
            }

            if (this.Start == other_range.Start)
            {
                return this.End.CompareTo(other_range.End);
            }
            else
                return this.Start.CompareTo(other_range.Start);
        }

        /// <summary>
        ///  Check if the range contains the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Contains(long value)
        {
            return this.Start <= value && this.End > value;
        }

        /// <summary>
        ///  Check if this object intersects with the given range.
        ///  Start positions are inclusive and End positions are
        ///  exclusive.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Boolean Intersects(LongRange range)
        {
            if (this.Start == range.Start || this.End == range.End)
                return true;

            if (this.Start < range.Start)
                return this.End > range.Start;
            else
                return this.Start < range.End;
        }

        /// <summary>
        ///  Check if this objects comes RIGHT AFTER or RIGHT
        ///  BEFORE the given range. That is. this.Start is equal
        ///  to range.End or vice versa.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Boolean IsConsequent(LongRange range)
        {
            return this.End == range.Start || this.Start == range.End;
        }

        /// <summary>
        ///  Merge this LongRange with a one that intersects it.
        /// </summary>
        /// <param name="range"></param>
        public void Merge(LongRange range)
        {
            if (!this.Intersects(range) && !this.IsConsequent(range))
                throw new Exception("Can't merge two LongRange objects that don't intersect");
            this.Start = Math.Min(this.Start, range.Start);
            this.End = Math.Max(this.End, range.End);
        }

        /// <summary>
        ///  Try to merge two Ranges. Do nothing if they are
        ///  not consecutive or intersecting ranges. Return true
        ///  if merged, false otherwise.
        /// </summary>
        /// <param name="range"></param>
        public Boolean TryMerge(LongRange range)
        {
            if (this.Intersects(range) || this.IsConsequent(range))
            {
                this.Merge(range);
                return true;
            }

            return false;
        }
    }
}

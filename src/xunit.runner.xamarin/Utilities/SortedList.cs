using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xunit.Runners.Utilities
{
    internal class SortedList<T> : IList<T>
    {
        private readonly IComparer<T> comparer;
        private readonly List<T> list = new List<T>();

        public SortedList(IComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException("comparer");
            this.comparer = comparer;
        }

        public int IndexOf(T item)
        {
            // PERF hint: this is a O(n) algorithm but could be rewritten as a O(log n) one.
            if (this.Count == 0)
            {
                return ~0;
            }

            for (int i = 0; i < this.Count; i++)
            {
                T existing = this[i];
                int compare = this.comparer.Compare(item, existing);
                if (compare == 0)
                {
                    return i;
                }
                else if (compare < 0)
                {
                    return ~i;
                }
            }

            return ~this.Count;
        }

        public void Insert(int index, T item)
        {
            // We trust our caller to be passing in a sorted index.
            this.list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this.list.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return this.list[index]; }
            set { throw new NotSupportedException(); }
        }

        public void Add(T item)
        {
            int index = this.IndexOf(item);
            if (index < 0)
            {
                index = ~index;
            }

            this.list.Insert(index, item);
        }

        public void Clear()
        {
            this.list.Clear();
        }

        public bool Contains(T item)
        {
            return this.IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            this.RemoveAt(index);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}

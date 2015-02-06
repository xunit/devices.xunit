using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Runners.Utilities
{

    internal class FilteredCollectionView<T, TFilterArg> : IList<T>, IList, INotifyCollectionChanged, IDisposable
    {
        private readonly ObservableCollection<T> dataSource;
        private readonly SortedList<T> filteredList;
        private readonly Func<T, TFilterArg, bool> filter;

        public FilteredCollectionView(ObservableCollection<T> dataSource, Func<T, TFilterArg, bool> filter, TFilterArg filterArgument, IComparer<T> sort)
        {
            if (dataSource == null) throw new ArgumentNullException("dataSource");
            if (filter == null) throw new ArgumentNullException("filter");
            if (sort == null) throw new ArgumentNullException("sort");

            this.dataSource = dataSource;
            this.filter = filter;
            this.filterArgument = filterArgument;
            this.filteredList = new SortedList<T>(sort);

            this.dataSource.CollectionChanged += this.dataSource_CollectionChanged;

            foreach (var item in this.dataSource)
            {
                this.OnAdded(item);
            }
        }

        /// <summary>
        /// Raised when one of the items selected by the filter is changed.
        /// </summary>
        /// <remarks>
        /// The sender is reported to be the item changed.
        /// </remarks>
        public event EventHandler<PropertyChangedEventArgs> ItemChanged;

        protected virtual void OnItemChanged(T sender, PropertyChangedEventArgs args)
        {
            var itemChanged = this.ItemChanged;
            if (itemChanged != null)
            {
                itemChanged(sender, args);
            }
        }

        private TFilterArg filterArgument;
        public TFilterArg FilterArgument
        {
            get { return this.filterArgument; }
            set
            {
                this.filterArgument = value;
                this.RefreshFilter();
            }
        }

        public void Reset()
        {
            this.RefreshFilter();
        }

        private void RefreshFilter()
        {
            this.filteredList.Clear();

            foreach (var item in this.dataSource)
            {
                if (this.filter(item, this.filterArgument))
                {
                    this.filteredList.Add(item);
                }
            }

            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void dataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (T item in e.NewItems)
                    {
                        this.OnAdded(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems)
                    {
                        this.OnRemoved(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (T item in e.OldItems)
                    {
                        this.OnRemoved(item);
                    }

                    foreach (T item in e.NewItems)
                    {
                        this.OnAdded(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
                default:
                    break;
            }
        }

        private void OnAdded(T item)
        {
            if (this.filter(item, this.filterArgument))
            {
                int index = this.filteredList.IndexOf(item);
                if (index < 0)
                {
                    this.filteredList.Insert(~index, item);
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, ~index));
                }
            }

            var observable = item as INotifyPropertyChanged;
            if (observable != null)
            {
                observable.PropertyChanged += this.dataSource_ItemChanged;
            }
        }

        private void OnRemoved(T item)
        {
            var observable = item as INotifyPropertyChanged;
            if (observable != null)
            {
                observable.PropertyChanged -= this.dataSource_ItemChanged;
            }

            int index = this.filteredList.IndexOf(item);
            if (index >= 0)
            {
                this.filteredList.RemoveAt(index);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }
        }

        private void dataSource_ItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = (T)sender;
            int index = this.filteredList.IndexOf(item);
            if (this.filter(item, this.FilterArgument))
            {
                if (index < 0)
                {
                    this.filteredList.Insert(~index, item);
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, ~index));
                }
            }
            else if (index >= 0)
            {
                this.filteredList.RemoveAt(index);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }

            this.OnItemChanged(item, e);
        }

        public void Dispose()
        {
            this.dataSource.CollectionChanged -= this.dataSource_CollectionChanged;

            foreach (var item in this.dataSource.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= this.dataSource_ItemChanged;
            }

            this.filteredList.Clear();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var collectionChanged = this.CollectionChanged;
            if (collectionChanged != null)
            {
                collectionChanged(this, args);
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return this.filteredList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.filteredList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.filteredList.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.filteredList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return this.filteredList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get
            {
                return this.filteredList[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value)
        {
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.filteredList.CopyTo((T[])array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }
    }

}

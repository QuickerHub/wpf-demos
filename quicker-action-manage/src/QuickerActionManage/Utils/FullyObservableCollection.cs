using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Item property changed event args
    /// </summary>
    public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public int CollectionIndex { get; }

        public ItemPropertyChangedEventArgs(int index, string name)
            : base(name)
            => this.CollectionIndex = index;

        public ItemPropertyChangedEventArgs(int index, PropertyChangedEventArgs args)
            : this(index, args.PropertyName)
        {
        }
    }

    /// <summary>
    /// Fully observable collection that notifies when items' properties change
    /// </summary>
    public class FullyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        public event EventHandler<ItemPropertyChangedEventArgs>? ItemPropertyChanged;

        public FullyObservableCollection() { }
        public FullyObservableCollection(List<T> list) : base(list) => ObserveAll();
        public FullyObservableCollection(IEnumerable<T> enumerable) : base(enumerable) => ObserveAll();

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    foreach (T oldItem in e.OldItems)
                        oldItem.PropertyChanged -= ChildPropertyChanged;
                    break;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                    foreach (T newItem in e.NewItems)
                        newItem.PropertyChanged += ChildPropertyChanged;
                    break;
            }

            base.OnCollectionChanged(e);
        }

        protected void OnItemPropertyChanged(int index, PropertyChangedEventArgs e) => ItemPropertyChanged?.Invoke(this, new ItemPropertyChangedEventArgs(index, e));

        protected override void ClearItems()
        {
            foreach (var obj in Items)
                obj.PropertyChanged -= ChildPropertyChanged;
            base.ClearItems();
        }

        private void ObserveAll() => ObserveAll(Items);

        private void ObserveAll(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                item.PropertyChanged += ChildPropertyChanged;
            }
        }

        private void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            int index = this.Items.IndexOf((T)sender);
            if (index < 0)
                throw new ArgumentException("Received property notification from item not in collection");
            OnItemPropertyChanged(index, e);
        }

        public void Reset(IEnumerable<T> range)
        {
            ClearItems();
            AddRangeInternal(range);
            RaiseChanges();
        }

        public void AddRange(IEnumerable<T> range)
        {
            AddRangeInternal(range);
            RaiseChanges();
        }

        public void InsertRange(int index, IEnumerable<T> range)
        {
            foreach (T obj in range.Reverse())
            {
                this.Items.Insert(index, obj);
            }
            ObserveAll(range);
            RaiseChanges();
        }

        public void RemoveItems(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                this.Items.Remove(item);
            }
            RaiseChanges();
        }

        private void AddRangeInternal(IEnumerable<T> range)
        {
            foreach (T obj in range)
            {
                this.Items.Add(obj);
            }
            ObserveAll(range);
        }

        private void RaiseChanges()
        {
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null) => OnPropertyChanged(new PropertyChangedEventArgs(name));
    }
}


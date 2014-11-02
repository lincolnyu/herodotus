using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace HerodotusTests
{
    public class MockSet<T> : ISet<T>, INotifyCollectionChanged
    {
        #region Fields

        private readonly HashSet<T> _hashSet = new HashSet<T>();

        #endregion

        #region Properties

        public int Count
        {
            get
            {
                return _hashSet.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Events

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Methods

        #region ISet<T> members

        public IEnumerator<T> GetEnumerator()
        {
            return _hashSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _hashSet.ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _hashSet.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _hashSet.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _hashSet.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _hashSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _hashSet.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _hashSet.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _hashSet.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _hashSet.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _hashSet.UnionWith(other);
        }

        public bool Add(T item)
        {
            var r = _hashSet.Add(item);
            if (r)
            {
                RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new[] { item }));
            }
            return r;
        }

        public void Clear()
        {
            _hashSet.Clear();
            RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _hashSet.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var r =  _hashSet.Remove(item);
            if (r)
            {
                RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, new[] { item }));
            }
            return r;
        }

        #endregion

        public void CopyTo(ISet<T> other)
        {
            other.Clear();
            foreach (var item in _hashSet)
            {
                other.Add(item);
            }
        }

        protected void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, args);
            }
        }

        #endregion
    }
}

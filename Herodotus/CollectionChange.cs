using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Herodotus
{
    public class CollectionChange<T> : ITrackedChange
    {
        #region Properties

        public ICollection<T> Collection { get; set; }

        public NotifyCollectionChangedAction Action { get; set; }

        public IList OldItems { get; set; } 

        public IList NewItems { get; set; }

        public int NewStartingIndex { get; set; }

        public int OldStartingIndex { get; set; }

        #endregion

        #region Methods

        #region ITrackedChange Members

        /// <summary>
        ///  Redoes the property change presumably from where the property hasn't changed
        /// </summary>
        public void Redo()
        {
            switch (Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Collection.Clear();
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (Collection is IList<T>)
                    {
                        var list = (IList<T>) Collection;
                        var index = NewStartingIndex;
                        foreach (var item in NewItems)
                        {
                            list.Insert(index++, (T)item);                            
                        }
                    }
                    else
                    {
                        foreach (var item in NewItems)
                        {
                            Collection.Add((T)item);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in OldItems)
                    {
                        Collection.Remove((T)item);
                    }                        
                    break;
            }
        }

        /// <summary>
        ///  Undoes the property change presumably from where the property has changed
        /// </summary>
        public void Undo()
        {
            switch (Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in NewItems)
                    {
                        Collection.Remove((T)item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    // TODO review and test this
                    if (Collection is IList<T>)
                    {
                        var list = (IList<T>)Collection;
                        var index = OldStartingIndex;
                        foreach (var item in OldItems)
                        {
                            list.Insert(index++, (T)item);
                        }
                    }
                    else
                    {
                        foreach (var item in OldItems)
                        {
                            Collection.Add((T)item);
                        }
                    }
                    break;
            }
        }

        #endregion

        #endregion
    }
}

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
                case NotifyCollectionChangedAction.Add:
                {
                    var list = Collection as IList<T>;
                    if (list != null)
                    {
                        var index = NewStartingIndex;
                        foreach (var item in NewItems)
                        {
                            list.Insert(index++, (T) item);
                        }
                    }
                    else
                    {
                        foreach (var item in NewItems)
                        {
                            Collection.Add((T) item);
                        }
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    foreach (var item in OldItems)
                    {
                        Collection.Remove((T) item);
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                {
                    System.Diagnostics.Debug.Assert(OldItems.Count == NewItems.Count);
                    var list = Collection as IList<T>;
                    if (list != null && OldStartingIndex == NewStartingIndex)
                    {
                        for (var i = 0; i < OldItems.Count; i++)
                        {
                            list[NewStartingIndex + i] = (T) NewItems[i];
                        }
                    }
                    else
                    {
                        foreach (var item in OldItems)
                        {
                            Collection.Remove((T) item);
                        }
                        foreach (var item in NewItems)
                        {
                            Collection.Add((T) item);
                        }
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Move:
                {
                    System.Diagnostics.Debug.Assert(OldItems.Count == NewItems.Count);
                    var list = Collection as IList<T>;
                    // Only works on list
                    if (list != null)
                    {
                        MoveList(list, OldStartingIndex, NewStartingIndex, OldItems.Count);
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                    // So actually this is non-undoable
                    Collection.Clear();
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
                        Collection.Remove((T) item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (Collection is IList<T>)
                    {
                        var list = (IList<T>) Collection;
                        var index = OldStartingIndex;
                        if (index >= 0 && index <= list.Count)
                        {
                            foreach (var item in OldItems)
                            {
                                list.Insert(index++, (T) item);
                            }
                        }
                        else
                        {
                            foreach (var item in OldItems)
                            {
                                list.Add((T)item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in OldItems)
                        {
                            Collection.Add((T) item);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                {
                    System.Diagnostics.Debug.Assert(OldItems.Count == NewItems.Count);
                    var list = Collection as IList<T>;
                    if (list != null && OldStartingIndex == NewStartingIndex)
                    {
                        for (var i = 0; i < OldItems.Count; i++)
                        {
                            list[OldStartingIndex + i] = (T) OldItems[i];
                        }
                    }
                    else
                    {
                        foreach (var item in NewItems)
                        {
                            Collection.Remove((T) item);
                        }
                        foreach (var item in OldItems)
                        {
                            Collection.Add((T) item);
                        }

                    }
                    break;
                }
                case NotifyCollectionChangedAction.Move:
                {
                    System.Diagnostics.Debug.Assert(OldItems.Count == NewItems.Count);
                    var list = Collection as IList<T>;
                    // Only works on list
                    if (list != null)
                    {
                        MoveList(list, NewStartingIndex, OldStartingIndex, NewItems.Count);
                    }
                    break;
                }
            }
        }

        #endregion

        private static void MoveList(IList<T> list, int sourceStartingIndex, int targetStartingIndex, int count)
        {
            int i, j;
            if (sourceStartingIndex < targetStartingIndex)
            {
                for (i = sourceStartingIndex + count - 1, j = targetStartingIndex + count - 1;
                    i >= targetStartingIndex + count;
                    i--, j--)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }
            }
            else
            {
                for (i = sourceStartingIndex, j = targetStartingIndex; i < sourceStartingIndex + count; i++, j++)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }
            }
        }

        #endregion
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace Herodotus
{
    public class Changeset 
    {
        #region Fields

        /// <summary>
        ///  The backing field for the property 'Changes', all changes the changeset contains
        /// </summary>
        private List<ITrackedChange> _changes;

        #endregion

        #region Constructors

        /// <summary>
        ///  Creates a changeset
        /// </summary>
        /// <param name="manager">The changeset manager that owns and manages this changeset</param>
        /// <param name="descriptor">An optional Descriptor of the changeset</param>
        public Changeset(ITrackingManager manager, object descriptor)
        {
            TrackingManager = manager;

            Descriptor = descriptor;
        }

        #endregion

        #region Properties

        /// <summary>
        ///  All changes the changeset contains and performs in order
        /// </summary>
        public IList<ITrackedChange> Changes
        {
            get { return _changes ?? (_changes = new List<ITrackedChange>()); }
        }

        /// <summary>
        ///  An object that describes the changeset
        /// </summary>
        public object Descriptor { get; private set; }

        public ITrackingManager TrackingManager
        {
            get; private set;
        }

        #endregion

        #region Methods

        public virtual void AddPropertyChange(object owner, PropertyInfo property, object oldValue, object newValue)
        {
            var change = new PropertyChange
            {
                Owner = owner,
                Property = property,
                NewValue = newValue,
                OldValue = oldValue
            };

            AddChange(change);
        }

        public virtual void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
        {
            var change = new CollectionChange<T>
            {
                Collection = (ICollection<T>)sender,
                Action = e.Action,
                NewItems = e.NewItems,
                OldItems = e.OldItems,
                NewStartingIndex = e.NewStartingIndex,
                OldStartingIndex = e.OldStartingIndex
            };

            AddChange(change);
        }

        /// <summary>
        ///  This allows clearing operation to be routed to Remove if the collection can intervene
        ///  the operation
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection</typeparam>
        /// <param name="collection">The collection to work on</param>
        public virtual void OnCollectionClearing<T>(ObservableCollection<T> collection)
        {
            var oldItems = new List<T>();
            oldItems.AddRange(collection);
            var change = new CollectionChange<T>
            {
                Collection = collection,
                Action = NotifyCollectionChangedAction.Remove,
                NewItems = null,
                OldItems = oldItems,
                NewStartingIndex = -1,
                OldStartingIndex = 0
            };

            AddChange(change);
        }

        protected void AddChange(ITrackedChange change)
        {
            Changes.Add(change);
            if (TrackingManager.MergeOnTheGo)
            {
                Merge();
            }
        }

        /// <summary>
        ///  Undoes the entire changeset, assuming starting from where the changeset has done completely
        /// </summary>
        public void Undo()
        {
            foreach (var change in Changes.Reverse())
            {
                change.Undo();
            }
        }

        /// <summary>
        ///  Redoes the entire changeset, assuming starting from where the changeset hasn't done at all
        /// </summary>
        public void Redo()
        {
            foreach (var change in Changes)
            {
                change.Redo();
            }
        }

        /// <summary>
        ///  Merges changes to the same properties on the same object
        /// </summary>
        public void Merge()
        {
            var changeMap = new Dictionary<object, Dictionary<string, ITrackedChange>>();
            var collectionMap = new Dictionary<object, ICollectionChange>();
            var count = Changes.Count;
            for (var i = 0; i < count; i++)
            {
                var propertyChange = _changes[i] as PropertyChange;
                if (propertyChange != null)
                {
                    var ownerContained = changeMap.ContainsKey(propertyChange.Owner);
                    var propertyContained = false;
                    if (ownerContained)
                    {
                        var changesOfOwner = changeMap[propertyChange.Owner];
                        propertyContained = changesOfOwner.ContainsKey(propertyChange.Property.Name);
                    }
                    else
                    {
                        changeMap[propertyChange.Owner] = new Dictionary<string, ITrackedChange>();
                    }
                    if (!propertyContained)
                    {
                        changeMap[propertyChange.Owner][propertyChange.Property.Name] = _changes[i];
                    }
                    else
                    {
                        var existing = (PropertyChange)changeMap[propertyChange.Owner][propertyChange.Property.Name];
                        existing.NewValue = propertyChange.NewValue;
                        _changes.RemoveAt(i);
                        i--;
                        count--;
                    }
                    continue;
                }

                // TODO complete it and debug it
                var collectionChange = _changes[i] as ICollectionChange;
                if (collectionChange != null)
                {
                    var collection = collectionChange.Collection;
                    var collectionContained = collectionMap.ContainsKey(collection);
                    if (collectionContained)
                    {
                        var prevChange = collectionMap[collection];
                        if (prevChange.Action == collectionChange.Action)
                        {
                            switch (prevChange.Action)
                            {
                                case NotifyCollectionChangedAction.Add:
                                {
                                    if (prevChange.NewStartingIndex < 0 ||
                                        prevChange.NewStartingIndex + prevChange.NewItems.Count ==
                                        collectionChange.NewStartingIndex)
                                    {
                                        if (prevChange.NewItems.IsReadOnly)
                                        {
                                            var newItems = prevChange.NewItems.Cast<object>().ToList();
                                            newItems.AddRange(collectionChange.NewItems.Cast<object>());
                                            prevChange.NewItems = newItems;
                                        }
                                        else
                                        {
                                            foreach (var ni in collectionChange.NewItems)
                                            {
                                                prevChange.NewItems.Add(ni);
                                            }
                                        }
                                        collectionChange = null;
                                    }
                                    break;
                                }
                                case NotifyCollectionChangedAction.Remove:
                                {
                                    if (prevChange.NewStartingIndex < 0 ||
                                        collectionChange.OldStartingIndex + collectionChange.OldItems.Count
                                        == prevChange.OldStartingIndex)
                                    {
                                        prevChange.OldStartingIndex = collectionChange.OldStartingIndex;
                                        if (collectionChange.OldItems.IsReadOnly)
                                        {
                                            var oldItems = collectionChange.OldItems.Cast<object>().ToList();
                                            oldItems.AddRange(prevChange.OldItems.Cast<object>());
                                            prevChange.OldItems = oldItems;
                                        }
                                        else
                                        {
                                            foreach (var oi in prevChange.OldItems)
                                            {
                                                collectionChange.OldItems.Add(oi);
                                            }
                                            prevChange.OldItems = collectionChange.OldItems;
                                        }
                                        collectionChange = null;
                                    }
                                    break;
                                }
                                case NotifyCollectionChangedAction.Replace:
                                {
                                    if (prevChange.OldStartingIndex == collectionChange.OldStartingIndex)
                                    {
                                        if (prevChange.NewItems.Count <= collectionChange.NewItems.Count)
                                        {
                                            prevChange.NewItems = collectionChange.NewItems;
                                        }
                                        else
                                        {
                                            if (prevChange.NewItems.IsReadOnly)
                                            {
                                                var newItems = collectionChange.NewItems.Cast<object>().ToList();
                                                for (var j = collectionChange.NewItems.Count;
                                                    j < prevChange.NewItems.Count;
                                                    j++)
                                                {
                                                    newItems[j] = prevChange.NewItems[j];
                                                }
                                                prevChange.NewItems = newItems;
                                            }
                                            else
                                            {
                                                for (var j = 0; j < collectionChange.NewItems.Count; j++)
                                                {
                                                    prevChange.NewItems[j] = collectionChange.NewItems[j];
                                                }
                                            }
                                        }
                                        collectionChange = null;
                                    }
                                    break;
                                }
                            }
                            // TODO other merging conditions   
                        }
                        else
                        {
                            // TODO other merging conditions
                            if (prevChange.Action == NotifyCollectionChangedAction.Add &&
                                collectionChange.Action == NotifyCollectionChangedAction.Remove)
                            {
                                // TODO implement it
                            }
                            else if (prevChange.Action == NotifyCollectionChangedAction.Remove &&
                                     collectionChange.Action == NotifyCollectionChangedAction.Add)
                            {
                                // TODO implement it
                            }
                        }
                        if (collectionChange == null)
                        {
                            _changes.RemoveAt(i);
                            i--;
                            count--;
                        }
                    }
                    if (collectionChange != null)
                    {
                        collectionMap[collection] = collectionChange;
                    }
                }
            }
        }

        #endregion
    }
}

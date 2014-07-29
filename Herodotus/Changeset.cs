using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace Herodotus
{
    public class Changeset
    {
        #region Nested types

        private class AuxiliaryChange
        {
            #region Constructors

            protected AuxiliaryChange(object collection, Type itemType, NotifyCollectionChangedAction action)
            {
                Collection = collection;
                ItemType = itemType;
                Action = action;
            }

            #endregion

            #region Properties

            public object Collection { get; private set; }

            public Type ItemType { get; private set; }

            public NotifyCollectionChangedAction Action { get; private set; }

            #endregion
        }

        private class ListChange : AuxiliaryChange
        {
            #region Constructors

            public ListChange(object collection, Type itemType, NotifyCollectionChangedAction action)
                : base(collection, itemType, action)
            {
                IndexToObject = new Dictionary<int, object>();
                Indices = new List<int>();
            }

            #endregion

            #region Properties

            public Dictionary<int, object> IndexToObject { get; private set; }

            // TODO this may be optimized out together with IndexToObject by a balanced binary tree
            public List<int> Indices { get; private set; }

            #endregion
        }

        private class SetChange : AuxiliaryChange
        {
            #region Constructors

            public SetChange(object collection, Type itemType, NotifyCollectionChangedAction action)
                : base(collection, itemType, action)
            {
                Items = new HashSet<object>();
            }

            #endregion

            #region Properties

            public HashSet<object> Items { get; private set; }

            #endregion
        }


        #endregion

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
        public object Descriptor { get; set; }

        public ITrackingManager TrackingManager
        {
            get; set;
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
        public virtual void OnCollectionClearing<T>(ICollection<T> collection)
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
        ///  This returns from the left the first item that satisfies index + at &lt; <paramref name="indices"/>[at]
        /// </summary>
        /// <param name="indices">The list of indices</param>
        /// <param name="index">The target index</param>
        /// <returns>The position where the criterion is first satisfied</returns>
        private int ExtendedBinarySearch(IList<int> indices, int index)
        {
            var low = 0;
            var high = indices.Count;
            // find the first one that satisfies index+at < indices[at]
            for (; low < high;)
            {
                var at = (low + high) / 2;
                var v = indices[at];
                if (index + at < v)
                {
                    high = at;
                }
                else if (index + at >= v)
                {
                    low = at + 1;
                }
            }
            return high;
        }

        /// <summary>
        ///  Merges changes to the same properties on the same object
        /// </summary>
        /// <remarks>
        ///  Made virtual to allow overriding for change/data structure dependent merge implementation
        /// </remarks>
        public virtual void Merge()
        {
            var changeMap = new Dictionary<object, Dictionary<string, ITrackedChange>>();
            var addMap = new Dictionary<object, LinkedListNode<object>>();
            var removeMap =new Dictionary<object, LinkedListNode<object>>();
            var merged = new LinkedList<object>();

            var count = Changes.Count;
            for (var i = 0; i < count; i++)
            {
                // TODO change of property/collection whose owner doesn't exist either before or after in the graph should be removed!
                var change = _changes[i];

                var propertyChange = change as PropertyChange;
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
                    if (propertyContained)
                    {
                        // merge the two property changes
                        var existing = (PropertyChange)changeMap[propertyChange.Owner][propertyChange.Property.Name];
                        existing.NewValue = propertyChange.NewValue;
                    }
                    else
                    {
                        changeMap[propertyChange.Owner][propertyChange.Property.Name] = change;
                        merged.AddLast(change);
                    }
                    continue;
                }

                var collectionChange = change as ICollectionChange;
                if (collectionChange != null)
                {
                    var collection = collectionChange.Collection;
                    if (collection is IList)
                    {
                        HandleListChangesMerge(addMap, removeMap, merged, collectionChange);
                    }
                    else
                    {
                        // assume it's a set that doesn't care about the order
                        HandleSetChangesMerge(addMap, removeMap, merged, collectionChange);
                    }
                    continue;
                }

                // unrecognized/unexpected change type, which is doubted to be successfully processed/compatible with the current processing
                merged.AddLast(change);
            }

            PostProcess(merged);

            _changes.Clear();
            foreach (var change in merged)
            {
                _changes.Add((ITrackedChange)change);
            }
        }

        private void HandleListChangesMerge(Dictionary<object, LinkedListNode<object>> addMap, 
            Dictionary<object, LinkedListNode<object>> removeMap, 
            LinkedList<object> merged, ICollectionChange collectionChange)
        {
            var collection = collectionChange.Collection;
            var itemType = collectionChange.ItemType;
            LinkedListNode<object> additions;
            LinkedListNode<object> removals;
            var hasAdd = addMap.TryGetValue(collection, out additions);
            var hasRemove = removeMap.TryGetValue(collection, out removals);
            var a = hasAdd ? (ListChange)additions.Value : null;
            var r = hasRemove ? (ListChange)removals.Value : null;

            int at;
            // NOTE list allow duplicates
            switch (collectionChange.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var index = collectionChange.NewStartingIndex;
                    foreach (var newItem in collectionChange.NewItems)
                    {
                        if (r != null)
                        {
                            at = r.Indices.BinarySearch(index);
                            if (at >= 0)
                            {
                                // goes through consecutive removals starting from at for the match of newItem 
                                var start = at;
                                var found = false;
                                for (; !found && at < r.Indices.Count && r.Indices[at] == index + at - start; at++)
                                {
                                    var objAt = r.IndexToObject[r.Indices[at]];
                                    if (objAt == newItem)
                                    {
                                        found = true;
                                    }
                                }
                                if (found)
                                {
                                    // undo the removal
                                    var indexAt = r.Indices[at];
                                    r.IndexToObject.Remove(indexAt);
                                    r.Indices.RemoveAt(at);

                                    // NOTE removal of an item in the removal changeset doesn't need to have update of subsequent objects
                                    index++;
                                    continue;
                                }
                            }
                        }

                        if (a == null)
                        {
                            // insert the additions before removals
                            a = new ListChange(collectionChange.Collection, itemType, NotifyCollectionChangedAction.Add);
                            addMap[collection] = (removals != null) ? merged.AddBefore(removals, a) : merged.AddLast(a);
                        }

                        int index2;
                        if (r != null)
                        {
                            at = ExtendedBinarySearch(r.Indices, index);
                            index2 = index + at;
                        }
                        else
                        {
                            index2 = index;
                        }

                        at = a.Indices.BinarySearch(index2);
                        if (at < 0)
                        {
                            at = -at - 1;
                        }
                        // pushes the add list from the position of 'at'
                        for (var i = a.Indices.Count - 1; i >= at; i--)
                        {
                            var orig = a.Indices[i]++;
                            var obj = a.IndexToObject[orig];
                            a.IndexToObject.Remove(orig);
                            a.IndexToObject[orig + 1] = obj;
                        }

                        // inserts the item
                        a.Indices.Insert(at, index2);
                        a.IndexToObject[index2] = newItem;

                        if (r != null)
                        {
                            // pushes r
                            at = r.Indices.BinarySearch(index2);
                            if (at < 0)
                            {
                                at = -at - 1;
                            }
                            for (var i = r.Indices.Count - 1; i >= at; i--)
                            {
                                var orig = r.Indices[i]++;
                                var obj = r.IndexToObject[orig];
                                r.IndexToObject.Remove(orig);
                                r.IndexToObject[orig + 1] = obj;
                            }
                        }
                        
                        index++;
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    var index = collectionChange.OldStartingIndex;
                    foreach (var oldItem in collectionChange.OldItems)
                    {
                        if (a != null)
                        {
                            int index2;
                            if (r != null)
                            {
                                at = ExtendedBinarySearch(r.Indices, index);
                                index2 = index + at;
                            }
                            else
                            {
                                index2 = index;
                            }

                            at = a.Indices.BinarySearch(index2);
                            if (at >= 0)
                            {
                                // undo the addition
                                // oldItem has to be equal to a.IndexToObject[index]
                                // and a.ObjectToIndices[oldItem] must contain index
                                // TODO check these
                                a.IndexToObject.Remove(index2);
                                a.Indices.RemoveAt(at);

                                // update the add list
                                for (; at < a.Indices.Count; at++)
                                {
                                    var orig = a.Indices[at]--;
                                    var obj = a.IndexToObject[orig];
                                    a.IndexToObject.Remove(orig);
                                    a.IndexToObject[orig - 1] = obj;
                                }

                                // update the remove list
                                if (r != null)
                                {
                                    at = r.Indices.BinarySearch(index2);
                                    // NOTE by definition at has to be negative
                                    // TODO check and enforce this
                                    at = -at - 1;
                                    for (; at < r.Indices.Count; at++)
                                    {
                                        var orig = r.Indices[at]--;
                                        var obj = r.IndexToObject[orig];
                                        r.IndexToObject.Remove(orig);
                                        r.IndexToObject[orig - 1] = obj;
                                    }
                                }
                                index++;
                                continue;
                            }
                        }

                        if (r == null)
                        {
                            // insert the removals after the additions
                            r = new ListChange(collection, itemType, NotifyCollectionChangedAction.Remove);
                            removeMap[collection] = (additions != null) ?  merged.AddAfter(additions, r) :  merged.AddLast(r);
                        }

                        at = ExtendedBinarySearch(r.Indices, index);

                        r.Indices.Insert(at, index + at);
                        r.IndexToObject[index + at] = oldItem;

                        index++;
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                {
                    // NOTE this may be optimized but that will be a bit more complicted
                    // Splits
                    var removeChange = new CollectionChange<object>
                    {
                        OldItems = collectionChange.OldItems,
                        Action = NotifyCollectionChangedAction.Remove,
                        OldStartingIndex = collectionChange.OldStartingIndex
                    };

                    var addChange = new CollectionChange<object>
                    {
                        NewItems = collectionChange.NewItems,
                        Action = NotifyCollectionChangedAction.Add,
                        NewStartingIndex = collectionChange.NewStartingIndex
                    };

                    HandleListChangesMerge(addMap, removeMap, merged, removeChange);
                    HandleListChangesMerge(addMap, removeMap, merged, addChange);

                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }
        }

        private void HandleSetChangesMerge(Dictionary<object, LinkedListNode<object>> addMap,
            Dictionary<object, LinkedListNode<object>> removeMap,
            LinkedList<object> merged, ICollectionChange collectionChange)
        {
            var collection = collectionChange.Collection;
            var itemType = collectionChange.ItemType;
            LinkedListNode<object> additions;
            LinkedListNode<object> removals;
            var hasAdd = addMap.TryGetValue(collection, out additions);
            var hasRemove = removeMap.TryGetValue(collection, out removals);
            var a = hasAdd ? (SetChange)additions.Value : null;
            var r = hasRemove ? (SetChange)removals.Value : null;

            switch (collectionChange.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    foreach (var newItem in collectionChange.NewItems)
                    {
                        if (r != null)
                        {
                            if (r.Items.Contains(newItem))
                            {
                                r.Items.Remove(newItem);
                                continue;
                            }
                        }
                        if (a == null)
                        {
                            a = new SetChange(collectionChange.Collection, itemType, NotifyCollectionChangedAction.Add);
                            addMap[collection] = (removals != null) ? merged.AddBefore(removals, a) : merged.AddLast(a);
                        }
                        if (!a.Items.Contains(newItem))
                        {
                            a.Items.Add(newItem);
                        }
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    foreach (var oldItem in collectionChange.OldItems)
                    {
                        if (a != null)
                        {
                            if (a.Items.Contains(oldItem))
                            {
                                a.Items.Remove(oldItem);
                                continue;
                            }
                        }
                        if (r == null)
                        {
                            r = new SetChange(collectionChange.Collection, itemType, NotifyCollectionChangedAction.Remove);
                            removeMap[collection] = (additions != null)
                                ? merged.AddAfter(additions, a)
                                : merged.AddLast(r);
                        }
                        if (!r.Items.Contains(oldItem))
                        {
                            r.Items.Add(oldItem);
                        }
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                {
                    // Splits
                    var removeChange = new CollectionChange<object>
                    {
                        OldItems = collectionChange.OldItems,
                        Action = NotifyCollectionChangedAction.Remove,
                    };

                    var addChange = new CollectionChange<object>
                    {
                        NewItems = collectionChange.NewItems,
                        Action = NotifyCollectionChangedAction.Add,
                    };

                    HandleSetChangesMerge(addMap, removeMap, merged, removeChange);
                    HandleSetChangesMerge(addMap, removeMap, merged, addChange);

                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }
        }

        private void PostProcess(LinkedList<object> merged)
        {
            LinkedListNode<object> nextNode;
            for (var n = merged.First; n != null; n = nextNode)
            {
                var change = n.Value;
                if (change is PropertyChange)
                {
                    // leave it
                    nextNode = n.Next;
                    continue;
                }

                var listChange = change as ListChange;
                if (listChange != null)
                {
                    nextNode = n.Next;
                    merged.Remove(n);
                    // currently we don't merge split of single replacement

                    if (listChange.Action == NotifyCollectionChangedAction.Add)
                    {
                        for (var i = 0; i < listChange.Indices.Count; )
                        {
                            var start = listChange.Indices[i];
                            var end = start + 1;
                            var items = new List<object> { listChange.IndexToObject[start] };
                            for (i++; i < listChange.Indices.Count && listChange.Indices[i] == end; i++, end++)
                            {
                                items.Add(listChange.IndexToObject[end]);
                            }
                            var changeToAdd = (ICollectionChange)InstantiateCollectionChange(listChange.ItemType);
                            changeToAdd.Collection = listChange.Collection;
                            changeToAdd.Action = listChange.Action;
                            changeToAdd.NewStartingIndex = start;
                            changeToAdd.NewItems = items;
                            if (nextNode != null)
                            {
                                merged.AddBefore(nextNode, changeToAdd);
                            }
                            else
                            {
                                merged.AddLast(changeToAdd);
                            }
                        }
                    }
                    else // remove
                    {
                        for (var i = listChange.Indices.Count - 1; i >= 0;)
                        {
                            var start = listChange.Indices[i];
                            var end = start - 1;
                            var items = new List<object> { listChange.IndexToObject[start] };
                            for (i--; i >= 0 && listChange.Indices[i] == end; i--, end--)
                            {
                                items.Insert(0, listChange.IndexToObject[end]); //TODO optimize this
                            }
                            var changeToAdd = (ICollectionChange)InstantiateCollectionChange(listChange.ItemType);
                            changeToAdd.Collection = listChange.Collection;
                            changeToAdd.Action = listChange.Action;
                            changeToAdd.OldStartingIndex = end+1;
                            changeToAdd.OldItems = items;
                            if (nextNode != null)
                            {
                                merged.AddBefore(nextNode, changeToAdd);
                            }
                            else
                            {
                                merged.AddLast(changeToAdd);
                            }
                        }
                    }
                    
                    continue;
                }

                var setChange = change as SetChange;
                if (setChange != null)
                {
                    // check if the next is the paired set change
                    var next = n.Next != null ? n.Next.Value as SetChange : null;
                    ICollectionChange changeToAdd;
                    if (next != null && next.Collection == setChange.Collection)
                    {
                        //combine the pair
                        changeToAdd = (ICollectionChange)InstantiateCollectionChange(setChange.ItemType);
                        changeToAdd.Collection = setChange.Collection;
                        changeToAdd.Action = NotifyCollectionChangedAction.Replace;
                        changeToAdd.NewItems = setChange.Items.ToList();
                        changeToAdd.OldItems = next.Items.ToList();
                        nextNode = n.Next.Next;
                        merged.Remove(n.Next);
                        merged.Remove(n);
                    }
                    else if (setChange.Action == NotifyCollectionChangedAction.Add)
                    {
                        changeToAdd = (ICollectionChange)InstantiateCollectionChange(setChange.ItemType);
                        changeToAdd.Collection = setChange.Collection;
                        changeToAdd.Action = NotifyCollectionChangedAction.Add;
                        changeToAdd.NewItems = setChange.Items.ToList();
                        nextNode = n.Next;
                        merged.Remove(n);
                    }
                    else if (setChange.Action == NotifyCollectionChangedAction.Remove)
                    {
                        changeToAdd = (ICollectionChange) InstantiateCollectionChange(setChange.ItemType);
                        changeToAdd.Collection = setChange.Collection;
                        changeToAdd.Action = NotifyCollectionChangedAction.Remove;
                        changeToAdd.OldItems = setChange.Items.ToList();
                        nextNode = n.Next;
                        merged.Remove(n);
                    }
                    else
                    {
                        throw new Exception("Unexpected change action");
                    }

                    if (nextNode != null)
                    {
                        merged.AddBefore(nextNode, changeToAdd);
                    }
                    else
                    {
                        merged.AddLast(changeToAdd);
                    }
                    continue;
                }

                nextNode = n.Next;// unknown type
            }
        }

        public static object InstantiateCollectionChange(Type itemType)
        {
            var genericType = typeof(CollectionChange<>).MakeGenericType(itemType);
            return Activator.CreateInstance(genericType);
        }

        #endregion
    }
}


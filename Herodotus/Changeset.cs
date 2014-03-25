using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        private readonly TrackingManager _manager;

        #endregion

        #region Constructors

        /// <summary>
        ///  Creates a changeset
        /// </summary>
        /// <param name="manager">The changeset manager that owns and manages this changeset</param>
        /// <param name="descriptor">An optional Descriptor of the changeset</param>
        public Changeset(TrackingManager manager, object descriptor)
        {
            _manager = manager;

            Descriptor = descriptor;
        }

        /// <summary>
        ///  Creates a changeset owned by default manager
        /// </summary>
        /// <param name="descriptor">An optional Descriptor of the changeset</param>
        public Changeset(object descriptor)
            : this(TrackingManager.Instance, descriptor)
        {
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

        #endregion

        #region Methods

        public void AddPropertyChange(object owner, PropertyInfo property, object oldValue, object newValue)
        {
            AddChange(new PropertyChange
            {
                Owner = owner,
                Property = property,
                NewValue = newValue,
                OldValue = oldValue
            });
        }

        public void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
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
        public void OnCollectionClearing<T>(ObservableCollection<T> collection)
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
            if (_manager.MergeOnTheGo)
            {
                Merge();
            }
        }

        /// <summary>
        ///  Undoes the entire changeset, assuming starting from where the changeset has done completely
        /// </summary>
        public void Undo()
        {
            for (var i = Changes.Count - 1; i >= 0; i--)
            {
                var change = Changes[i];
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
            var count = Changes.Count;
            for (var i = 0; i < count; i++)
            {
                var propertyChange = _changes[i] as PropertyChange;
                if (propertyChange == null) continue;
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
            }
        }

        #endregion
    }
}

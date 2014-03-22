using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;

namespace Herodotus
{
    public class ChangesetManager
    {
        #region Delegates
        
        public delegate void ChangeSetIndexChangedEvent();

        public delegate void ChangeSetRemovedRangeEvent(int index, int count);

        #endregion

        #region Fields

        private static ChangesetManager _changesetManager;
        private ObservableCollection<Changeset> _changesets;

        private int _currentChangesetIndex;
        private bool _suppressIndexChangedEvent;

        /// <summary>
        ///  A flag that indicates if a changeset is currently undergoing undo/redo
        /// </summary>
        private bool _isUndoRedoing;

        /// <summary>
        ///  A counter that indicates the current level of nested tracking
        /// </summary>
        private int _trackingDepth;

        private object _trackedObject;
        private PropertyInfo _trackedProperty;
        private object _trackedOldValue;
        private object _trackedNewValue;

        #endregion

        #region Properties

        public static ChangesetManager Instance
        {
            get { return _changesetManager ?? (_changesetManager = new ChangesetManager()); }
        }

        public ObservableCollection<Changeset> Changesets
        {
            get { return _changesets ?? (_changesets = new ObservableCollection<Changeset>()); }
        }

        public int CurrentChangeSetIndex
        {
            get { return _currentChangesetIndex; }
            set
            {
                if (_currentChangesetIndex == value) return;
                _currentChangesetIndex = value;
                OnChangeSetIndexChanged();
            }
        }

        public bool IsTrackingEnabled { get; set; }

        public bool SuspendTracking
        {
            get
            {
                return (!IsTrackingEnabled || _isUndoRedoing || _trackingDepth > 0);
            }
        }

        /// <summary>
        ///  Whether to perform merge right after a change is tracked and added to the current changeset
        /// </summary>
        public bool MergeOnTheGo
        {
            get; set;
        }

        public int NestCount
        {
            get; private set;
        }

        public Changeset CommittingChangeset
        {
            get;
            private set;
        }

        #endregion

        #region Events

        public event ChangeSetIndexChangedEvent ChangeSetIndexChanged;

        public event ChangeSetRemovedRangeEvent ChangeSetRemovedRange;

        #endregion

        #region Methods

        #region Event handlers

        public void TrackPropertyChangeBegin(object owner, string propertyName, object targetValue)
        {
            lock (this)
            {
                if (SuspendTracking || CommittingChangeset == null)
                {
                    _trackingDepth++;
                    return;
                }

                var type = owner.GetType();

                _trackedObject = owner;
                _trackedProperty = type.GetRuntimeProperty(propertyName);

                _trackedOldValue = _trackedProperty.GetValue(owner, null);
                _trackedNewValue = targetValue;

                _trackingDepth++;
            }
        }

        public void TrackPropertyChangeEnd()
        {
            lock (this)
            {
                if (_trackingDepth == 1 && CommittingChangeset != null && _trackedObject != null && _trackedProperty != null)
                {
                    CommittingChangeset.AddPropertyChange(_trackedObject, _trackedProperty, _trackedOldValue, _trackedNewValue);
                    ClearTrackedProperty();
                }

                if (_trackingDepth > 0)
                {
                    _trackingDepth--;
                }
            }
        }

        public void TrackPropertyChangeCancel()
        {
            lock (this)
            {
                if (_trackingDepth == 1)
                {
                    ClearTrackedProperty();
                }
            }
        }

        private void ClearTrackedProperty()
        {
            _trackedObject = null;
            _trackedProperty = null;
            _trackedOldValue = null;
            _trackedNewValue = null;
        }

        public void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CommittingChangeset == null) return;
            if (SuspendTracking) return;
            CommittingChangeset.OnCollectionChanged<T>(sender, e);
        }

        public void OnCollectionClearing<T>(ObservableCollection<T> collection)
        {
            if (CommittingChangeset == null) return;
            if (SuspendTracking) return;
            CommittingChangeset.OnCollectionClearing(collection);
        }

        #endregion

        public int StartChangeset(object descriptor=null)
        {
            lock (this)
            {
                if (NestCount++ > 0)
                {
                    return NestCount;
                }
                if (!IsTrackingEnabled)
                {
                    return NestCount;
                }
                CommittingChangeset = new Changeset(this, descriptor);
                return NestCount;
            }
        }

        public int Commit(bool merge=false, bool commitEmpty = false)
        {
            int nestCount;
            lock (this)
            {
                nestCount = NestCount;
                if (NestCount <= 0)
                {
                    NestCount = 0;
                    return nestCount;
                }
                if (--NestCount > 0)
                {
                    return nestCount;
                }
                if (!IsTrackingEnabled)
                {
                    return nestCount;
                }
            }

            if (!commitEmpty && CommittingChangeset.Changes.Count == 0)
            {
                CommittingChangeset = null;
                return nestCount;
            }

            var d = Changesets.Count - CurrentChangeSetIndex;
            if (d > 0)
            {
                do
                {
                    Changesets.RemoveAt(Changesets.Count - 1);
                } while (CurrentChangeSetIndex < Changesets.Count);
                OnRemoveRange(CurrentChangeSetIndex, d);
            }

            if (merge)
            {
                CommittingChangeset.Merge();
            }
            Changesets.Add(CommittingChangeset);
            CurrentChangeSetIndex = Changesets.Count;
            CommittingChangeset = null;
            return nestCount;
        }

        /// <summary>
        ///  Rolls back the current changeset
        /// </summary>
        /// <param name="merge">Merge the changeset before undoing it</param>
        public void Rollback(bool merge=false)
        {
            if (CommittingChangeset == null) return;

            if (merge)
            {
                CommittingChangeset.Merge();
            }
            CommittingChangeset.Undo();

            // rolled back changeset can't be committed again 
            CommittingChangeset = null;
        }

        /// <summary>
        ///  Cancels the commitment of the current changeset
        /// </summary>
        public void Cancel()
        {
            CommittingChangeset = null;
        }

        /// <summary>
        ///  Redoes the current changest
        /// </summary>
        /// <remarks>
        ///  This should be the only place the changeset's Redo() method is called
        /// </remarks>
        public void Redo()
        {
            if (CurrentChangeSetIndex >= Changesets.Count) return;
            _isUndoRedoing = true;
            Changesets[CurrentChangeSetIndex].Redo();
            _isUndoRedoing = false;
            CurrentChangeSetIndex++;
        }

        /// <summary>
        ///  Undoes the previous changeset
        /// </summary>
        /// <remarks>
        ///  This should be the only place the changeset's Undo() method is called
        /// </remarks>
        public void Undo()
        {
            if (CurrentChangeSetIndex == 0) return;
            _isUndoRedoing = true;
            Changesets[CurrentChangeSetIndex - 1].Undo();
            _isUndoRedoing = false;
            CurrentChangeSetIndex--;
        }

        /// <summary>
        ///  removes changeset to specified index exclusive
        /// </summary>
        /// <param name="index"></param>
        public void RemoveTo(int index)
        {
            _suppressIndexChangedEvent = true;
            while (CurrentChangeSetIndex < index)
            {
                Redo();
            }
            for (var i = 0; i < index; i++)
            {
                Changesets.RemoveAt(0);
            }
            CurrentChangeSetIndex -= index;
            _suppressIndexChangedEvent = false;

            OnRemoveRange(0, index);
            // TODO always treated as changed even if the value remains the same?
            OnChangeSetIndexChanged();
        }

        /// <summary>
        ///  removes changeset from specified index inclusive
        /// </summary>
        /// <param name="index"></param>
        public void RemoveFrom(int index)
        {
            _suppressIndexChangedEvent = true;
            var count = Changesets.Count - index + 1;
            var origIndex = CurrentChangeSetIndex;
            while (CurrentChangeSetIndex > index)
            {
                Undo();
            }
            while (Changesets.Count > index)
            {
                Changesets.RemoveAt(Changesets.Count - 1);
            }
            _suppressIndexChangedEvent = false;

            OnRemoveRange(index, count);
            if (origIndex != CurrentChangeSetIndex)
            {
                OnChangeSetIndexChanged();
            }
        }

        /// <summary>
        ///  removes all changesets tracked without changing the current state of the model
        /// </summary>
        public void RemoveAll()
        {
            Changesets.Clear();
            // not to fire the event
            _currentChangesetIndex = 0;
        }

        private void OnRemoveRange(int index, int count)
        {
            if (ChangeSetRemovedRange != null)
            {
                ChangeSetRemovedRange(index, count);
            }
        }

        private void OnChangeSetIndexChanged()
        {
            if (_suppressIndexChangedEvent) return;
            if (ChangeSetIndexChanged != null)
            {
                ChangeSetIndexChanged();
            }
        }

        #endregion
    }
}

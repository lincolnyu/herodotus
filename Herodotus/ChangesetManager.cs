using System.Collections.ObjectModel;
using System.Collections.Specialized;

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
        private Changeset _committingChangeset;

        private int _currentChangesetIndex;
        private bool _suppressIndexChangedEvent;

        /// <summary>
        ///  A flag that indicates if a changeset is currently undergoing undo/redo
        /// </summary>
        private bool _isUndoRedoing;

        /// <summary>
        ///  A flag that indicates if a changeset is being currently recording a property chnage
        /// </summary>
        private bool _isTracking;

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
                return (_isUndoRedoing || _isTracking);
            }
        }

        /// <summary>
        ///  Whether to perform merge right after a change is tracked and added to the current changeset
        /// </summary>
        public bool MergeOnTheGo
        {
            get; set;
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
            if (_committingChangeset == null) return;
            if (SuspendTracking) return;
            _committingChangeset.TrackPropertyChangeBegin(owner, propertyName, targetValue);
            _isTracking = true;
        }

        public void TrackPropertyChangeEnd()
        {
            if (_committingChangeset == null) return;
            _isTracking = false;
            _committingChangeset.TrackPropertyChangeEnd();
        }

        public void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_committingChangeset == null) return;
            if (SuspendTracking) return;
            _committingChangeset.OnCollectionChanged<T>(sender, e);
        }

        public void OnCollectionClearing<T>(ObservableCollection<T> collection)
        {
            if (_committingChangeset == null) return;
            if (SuspendTracking) return;
            _committingChangeset.OnCollectionClearing(collection);
        }

        #endregion

        public Changeset StartChangeset(object descriptor=null)
        {
            lock (this)
            {
                if (!IsTrackingEnabled) return null;
                if (_committingChangeset != null) return null;   // can't track multple changesets simultaneously 
                _committingChangeset = new Changeset(this, descriptor);
                return _committingChangeset;
            }
        }

        public void Commit(bool merge=false, bool dontCommitEmpty = false)
        {
            if (_committingChangeset == null) return;
            if (dontCommitEmpty && _committingChangeset.Changes.Count == 0)
            {
                _committingChangeset = null;
                return;
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
                _committingChangeset.Merge();
            }
            Changesets.Add(_committingChangeset);
            CurrentChangeSetIndex = Changesets.Count;
            _committingChangeset = null;
        }

        /// <summary>
        ///  Rolls back the current changeset
        /// </summary>
        /// <param name="dispose">whether to dispose the changeset afterwards meaning no more recommit attempt</param>
        public void Rollback(bool dispose = true)
        {
            if (_committingChangeset == null) return;
            _committingChangeset.Undo();

            if (!dispose) return;
            // rolled back changeset can't be committed again 
            _committingChangeset.Dispose();
            _committingChangeset = null;
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

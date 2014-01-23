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
            _committingChangeset.TrackPropertyChangeBegin(owner, propertyName, targetValue);
        }

        public void TrackPropertyChangeEnd()
        {
            if (_committingChangeset == null) return;
            _committingChangeset.TrackPropertyChangeEnd();
        }

        public void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_committingChangeset == null) return;
            _committingChangeset.OnCollectionChanged<T>(sender, e);
        }

        public void OnCollectionClearing<T>(ObservableCollection<T> collection)
        {
            if (_committingChangeset == null) return;
            _committingChangeset.OnCollectionClearing(collection);
        }

        #endregion

        public Changeset StartChangeset(string description, bool autoMerge=false)
        {
            if (!IsTrackingEnabled) return null;
            _committingChangeset = new Changeset(description);
            return _committingChangeset;
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

        public void Redo()
        {
            if (CurrentChangeSetIndex >= Changesets.Count) return;
            Changesets[CurrentChangeSetIndex].Redo();
            CurrentChangeSetIndex++;
        }

        public void Undo()
        {
            if (CurrentChangeSetIndex == 0) return;
            Changesets[CurrentChangeSetIndex - 1].Undo();
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

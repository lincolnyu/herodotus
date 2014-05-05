using System.Collections.ObjectModel;

namespace Herodotus
{
    public class LinearChangesetManager : TrackingManager, ILinearChangesetManager
    {
        #region Delegates

        public delegate void ChangeSetRemovedRangeEvent(int index, int count);

        #endregion

        #region Fields

        private ObservableCollection<Changeset> _changesets;

        private int _currentChangesetIndex;

        private bool _suppressIndexChangedEvent;

        #endregion

        #region Properties

        public ObservableCollection<Changeset> Changesets
        {
            get { return _changesets ?? (_changesets = new ObservableCollection<Changeset>()); }
        }

        public int CurrentChangesetIndex
        {
            get { return _currentChangesetIndex; }
            set
            {
                if (_currentChangesetIndex == value) return;
                _currentChangesetIndex = value;
                OnChangeSetIndexChanged();
            }
        }

        #endregion

        #region Events

        #region ILinearChangeset manager members

        public event ChangesetIndexChangedEvent ChangesetIndexChanged;

        public event ChangeSetRemovedRangeEvent ChangeSetRemovedRange;

        #endregion

        #endregion

        #region Methods

        protected override void OnCommit()
        {
            var d = Changesets.Count - CurrentChangesetIndex;
            if (d > 0)
            {
                do
                {
                    Changesets.RemoveAt(Changesets.Count - 1);
                } while (CurrentChangesetIndex < Changesets.Count);
                OnRemoveRange(CurrentChangesetIndex, d);
            }

            Changesets.Add(CommittingChangeset);
            CurrentChangesetIndex = Changesets.Count;
        }

        public void Reinitialize()
        {
            Changesets.Clear();
            _currentChangesetIndex = 0;
            _suppressIndexChangedEvent = false;
        }

        public bool CanRedo()
        {
            return (CurrentChangesetIndex < Changesets.Count);
        }

        public bool CanUndo()
        {
            return (CurrentChangesetIndex > 0);
        }

        /// <summary>
        ///  Redoes the current changest
        /// </summary>
        /// <remarks>
        ///  This should be the only place the changeset's Redo() method is called
        /// </remarks>
        public void Redo()
        {
            IsUndoRedoing = true;
            Changesets[CurrentChangesetIndex].Redo();
            IsUndoRedoing = false;
            CurrentChangesetIndex++;
        }

        /// <summary>
        ///  Undoes the previous changeset
        /// </summary>
        /// <remarks>
        ///  This should be the only place the changeset's Undo() method is called
        /// </remarks>
        public void Undo()
        {
            IsUndoRedoing = true;
            Changesets[CurrentChangesetIndex - 1].Undo();
            IsUndoRedoing = false;
            CurrentChangesetIndex--;
        }

        /// <summary>
        ///  removes changeset to specified index exclusive
        /// </summary>
        /// <param name="index"></param>
        public void RemoveTo(int index)
        {
            _suppressIndexChangedEvent = true;
            while (CurrentChangesetIndex < index)
            {
                Redo();
            }
            for (var i = 0; i < index; i++)
            {
                Changesets.RemoveAt(0);
            }
            CurrentChangesetIndex -= index;
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
            var origIndex = CurrentChangesetIndex;
            while (CurrentChangesetIndex > index)
            {
                Undo();
            }
            while (Changesets.Count > index)
            {
                Changesets.RemoveAt(Changesets.Count - 1);
            }
            _suppressIndexChangedEvent = false;

            OnRemoveRange(index, count);
            if (origIndex != CurrentChangesetIndex)
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
            if (ChangesetIndexChanged != null)
            {
                ChangesetIndexChanged();
            }
        }

        #endregion
    }
}

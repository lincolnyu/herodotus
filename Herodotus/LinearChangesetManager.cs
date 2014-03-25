using System.Collections.ObjectModel;

namespace Herodotus
{
    public class LinearChangesetManager : TrackingManager, ILinearChangesetManager
    {
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

        #endregion

        #region Events

        #region ILinearChangeset manager members

        public event ChangeSetIndexChangedEvent ChangeSetIndexChanged;

        public event ChangeSetRemovedRangeEvent ChangeSetRemovedRange;

        #endregion

        #endregion

        #region Methods

        protected override void OnCommit()
        {
            var d = Changesets.Count - CurrentChangeSetIndex;
            if (d > 0)
            {
                do
                {
                    Changesets.RemoveAt(Changesets.Count - 1);
                } while (CurrentChangeSetIndex < Changesets.Count);
                OnRemoveRange(CurrentChangeSetIndex, d);
            }

            Changesets.Add(CommittingChangeset);
            CurrentChangeSetIndex = Changesets.Count;
        }

        /// <summary>
        ///  Redoes the current changest
        /// </summary>
        /// <remarks>
        ///  This should be the only place the changeset's Redo() method is called
        /// </remarks>
        public virtual void Redo()
        {
            if (CurrentChangeSetIndex >= Changesets.Count) return;
            IsUndoRedoing = true;
            Changesets[CurrentChangeSetIndex].Redo();
            IsUndoRedoing = false;
            CurrentChangeSetIndex++;
        }

        /// <summary>
        ///  Undoes the previous changeset
        /// </summary>
        /// <remarks>
        ///  This should be the only place the changeset's Undo() method is called
        /// </remarks>
        public virtual void Undo()
        {
            if (CurrentChangeSetIndex == 0) return;
            IsUndoRedoing = true;
            Changesets[CurrentChangeSetIndex - 1].Undo();
            IsUndoRedoing = false;
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

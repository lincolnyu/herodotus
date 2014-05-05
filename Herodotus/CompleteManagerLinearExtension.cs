using System.Collections.ObjectModel;

namespace Herodotus
{
    public class CompleteManagerLinearExtension : CompleteManager, ILinearChangesetManager
    {
        #region Fields

        private int _currentChangesetIndex;

        #endregion

        #region Constructors

        public CompleteManagerLinearExtension()
        {
            Changesets = new ObservableCollection<Changeset>();
        }

        #endregion

        #region Properties

        public int CurrentChangesetIndex
        {
            get { return _currentChangesetIndex; }
            set
            {
                if (_currentChangesetIndex != value)
                {
                    _currentChangesetIndex = value;
                    OnChangesetIndexChanged();
                }
            }
        }

        public ObservableCollection<Changeset> Changesets { get; private set; }

        #endregion

        #region 

        public event ChangesetIndexChangedEvent ChangesetIndexChanged;

        #endregion

        #region Methods

        public override void Reinitialize()
        {
            base.Reinitialize();
            CurrentChangesetIndex = 0;
            Changesets.Clear();
        }

        public override void Undo()
        {
            base.Undo();
            CurrentChangesetIndex--;
        }

        public override void Redo(int branchIndex)
        {
            base.Redo(branchIndex);
            CurrentChangesetIndex++;
        }

        public override void RedoVirtual(int branchIndex)
        {
            base.RedoVirtual(branchIndex);
            CurrentChangesetIndex++;
        }

        public override void UndoVirtual()
        {
            base.UndoVirtual();
            CurrentChangesetIndex--;
        }

        protected override void OnCommit()
        {
            base.OnCommit();

            var d = Changesets.Count - CurrentChangesetIndex;
            if (d > 0)
            {
                do
                {
                    Changesets.RemoveAt(Changesets.Count - 1);
                } while (CurrentChangesetIndex < Changesets.Count);
            }
            Changesets.Add(CommittingChangeset);
            CurrentChangesetIndex = Changesets.Count;
        }

        private void OnChangesetIndexChanged()
        {
            if (ChangesetIndexChanged != null)
            {
                ChangesetIndexChanged();
            }
        }

        #endregion
    }
}

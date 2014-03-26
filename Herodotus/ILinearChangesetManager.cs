using System.Collections.ObjectModel;

namespace Herodotus
{
    public delegate void ChangesetIndexChangedEvent();

    public interface ILinearChangesetManager : IChangesetManager
    {
        #region Properties

        int CurrentChangesetIndex { get; }

        ObservableCollection<Changeset> Changesets { get; }

        #endregion

        #region Events

        event ChangesetIndexChangedEvent ChangesetIndexChanged;

        #endregion
    }
}

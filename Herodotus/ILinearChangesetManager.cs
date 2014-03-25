namespace Herodotus
{
    public delegate void ChangeSetIndexChangedEvent();

    public delegate void ChangeSetRemovedRangeEvent(int index, int count);

    public interface ILinearChangesetManager : IChangesetManager
    {
        #region Properties

        int CurrentChangeSetIndex { get; }

        #endregion

        #region Events

        event ChangeSetIndexChangedEvent ChangeSetIndexChanged;

        event ChangeSetRemovedRangeEvent ChangeSetRemovedRange;

        #endregion
    }
}

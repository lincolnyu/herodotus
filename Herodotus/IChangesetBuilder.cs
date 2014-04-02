namespace Herodotus
{
    public interface IChangesetBuilder
    {
        #region Methods

        Changeset BuildChangeset(TrackingManager trackingManager, object descriptor);

        #endregion
    }
}

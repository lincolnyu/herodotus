namespace Herodotus
{
    public interface IChangesetBuilder
    {
        #region Methods

        Changeset BuildChangeset(ITrackingManager trackingManager, object descriptor);

        #endregion
    }
}

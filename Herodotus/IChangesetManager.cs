namespace Herodotus
{
    public interface IChangesetManager
    {
        #region Methods

        void Redo();

        void Undo();

        #endregion
    }
}

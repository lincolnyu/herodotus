namespace Herodotus
{
    /// <summary>
    ///  Provides basic undo/redo functionalities
    /// </summary>
    public interface IChangesetManager
    {
        #region Methods

        /// <summary>
        ///  If there's succeeding change that can be redone
        /// </summary>
        /// <returns>True if it can be redone</returns>
        bool CanRedo();

        /// <summary>
        ///  If there's preceding change that can be undone
        /// </summary>
        /// <returns>True if it can be undone</returns>
        bool CanUndo();

        /// <summary>
        ///  Redoes the change after
        /// </summary>
        /// <remarks>
        ///  NOTE it's the caller's responsibility to query CanRedo() to
        ///  make sure Redo() can be called
        /// </remarks>
        void Redo();

        /// <summary>
        ///  Undoes the change before
        /// </summary>
        /// <remarks>
        ///  NOTE it's the caller's responsibility to query CanUndo() to
        ///  make sure Undo() can be called
        /// </remarks>
        void Undo();

        #endregion
    }
}

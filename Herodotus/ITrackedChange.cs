namespace Herodotus
{
    /// <summary>
    ///  An interface that declares undo and redo capability
    /// </summary>
    public interface ITrackedChange
    {
        #region Methods

        /// <summary>
        ///  Redoes the property change presumably from where the property hasn't changed
        /// </summary>
        void Redo();
        
        /// <summary>
        ///  Undoes the property change presumably from where the property has changed
        /// </summary>
        void Undo();

        #endregion
    }
}

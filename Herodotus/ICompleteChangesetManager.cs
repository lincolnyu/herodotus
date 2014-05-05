namespace Herodotus
{
    /// <summary>
    ///  A manager that's able to track all the changes including user's undo/redo requests in a tree structure
    /// </summary>
    public interface ICompleteChangesetManager : IChangesetManager
    {
        #region Properties

        /// <summary>
        ///  The current state node
        /// </summary>
        StateNode CurrentStateNode { get; }

        #endregion

        #region Methods

        /// <summary>
        ///  Redoes a specified branch
        /// </summary>
        /// <param name="branchIndex">The branch to redo</param>
        void Redo(int branchIndex);

        /// <summary>
        ///  Makes a virtual undo (doesn't make changes but change the state/changeset pointer)
        /// </summary>
        void UndoVirtual();

        /// <summary>
        ///  Makes a virtual redo (doesn't make changes but change the state/changeset pointer)
        /// </summary>
        /// <param name="branchIndex">The branch to redo</param>
        void RedoVirtual(int branchIndex);

        /// <summary>
        ///  Clear old branches of the current node and branches of all its ancestors
        /// </summary>
        void GoLinear();

        /// <summary>
        ///  Removes all nodes before the specified node which is then made root
        /// </summary>
        void MakeRoot(StateNode node);

        /// <summary>
        ///  Moves to the specified node on the tree with undos and/or redos
        /// </summary>
        /// <param name="target"></param>
        void MoveTo(StateNode target);

        #endregion
    }
}

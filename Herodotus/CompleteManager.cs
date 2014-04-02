namespace Herodotus
{
    /// <summary>
    ///  A manager that can potentially keeps all the changes user has made
    /// </summary>
    public class CompleteManager : TrackingManager, ICompleteChangesetManager
    {
        #region Constructors

        /// <summary>
        ///  Instantiates and initialises a CompleteManager object
        /// </summary>
        public CompleteManager()
        {
            RootNode = new StateNode();
            CurrentStateNode = RootNode;
        }

        #endregion

        #region Properties

        /// <summary>
        ///  The current state node
        /// </summary>
        public StateNode CurrentStateNode
        {
            get; private set;
        }

        /// <summary>
        ///  The root node that represents the inital state
        /// </summary>
        public StateNode RootNode
        {
            get; private set;
        }

        #endregion

        #region Methods

        #region ICompleteChangesetManager members

        #region IChangesetManager members

        public bool CanRedo()
        {
            return (CurrentStateNode.Branches.Count > 0);
        }

        public bool CanUndo()
        {
            return (CurrentStateNode.Parent.Target != null);
        }

        /// <summary>
        ///  Redoes the change set to the most recent child
        /// </summary>
        public void Redo()
        {
            Redo(CurrentStateNode.Branches.Count - 1);
        }

        /// <summary>
        ///  Undoes the changeset since parent
        /// </summary>
        public void Undo()
        {
            CurrentStateNode.Parent.Changeset.Undo();
            CurrentStateNode = CurrentStateNode.Parent.Target;
        }

        #endregion

        /// <summary>
        ///  Redoes a specified branch
        /// </summary>
        /// <param name="branchIndex">The branch to redo</param>
        public void Redo(int branchIndex)
        {
            var branch = CurrentStateNode.Branches[branchIndex];
            branch.Changeset.Redo();
            CurrentStateNode = branch.Target;
        }

        /// <summary>
        ///  Makes a virtual undo (doesn't make changes but change the state/changeset pointer)
        /// </summary>
        public void UndoVirtual()
        {
            CurrentStateNode = CurrentStateNode.Parent.Target;
        }

        /// <summary>
        ///  Makes a virtual redo (doesn't make changes but change the state/changeset pointer)
        /// </summary>
        /// <param name="branchIndex">The branch to redo</param>
        public void RedoVirtual(int branchIndex)
        {
            var branch = CurrentStateNode.Branches[branchIndex];
            CurrentStateNode = branch.Target;
        }

        #endregion

        #region TrackingManager members

        protected override void OnCommit()
        {
            var newNode = new StateNode
            {
                Parent = new StateNode.Link
                {
                    Changeset = CommittingChangeset,
                    Target = CurrentStateNode
                }
            };
            var branch = new StateNode.Link
            {
                Changeset = CommittingChangeset,
                Target = newNode
            };
            if (CurrentStateNode != null)
            {
                CurrentStateNode.Branches.Add(branch);
            }
            CurrentStateNode = newNode;
        }

        #endregion

        /// <summary>
        ///  Clear all branches of the current node
        /// </summary>
        public void ClearBranches()
        {
            ClearFromNode(CurrentStateNode);
        }

        /// <summary>
        ///  Clear all branches of the specified node excepts for the branch that leads the specified child
        /// </summary>
        /// <param name="node">The node whose branches are to be removed</param>
        /// <param name="child">The child to which the branch to spare</param>
        protected void ClearBranchesBut(StateNode node, StateNode child)
        {
            var changeset = child.Parent.Changeset;
            node.Branches.Clear();
            node.Branches.Add(new StateNode.Link
            {
                Changeset = changeset,
                Target = child
            });
        }

        /// <summary>
        ///  Clear branches of the current and branches of all its ancestors
        /// </summary>
        public void ClearAllBranches()
        {
            CurrentStateNode.Branches.Clear();
            var child = CurrentStateNode;
            for (var p = CurrentStateNode.Parent.Target; p != null; p = p.Parent.Target)
            {
                ClearBranchesBut(p, child);
                child = p;
            }
        }

        /// <summary>
        ///  Removes all nodes before the specified node
        /// </summary>
        public void ClearToNode(StateNode node)
        {
            node.Parent = new StateNode.Link();
            RootNode = node;
        }

        /// <summary>
        ///  Removes everything down the specified node
        /// </summary>
        /// <param name="node"></param>
        public void ClearFromNode(StateNode node)
        {
            node.Branches.Clear();
        }

        #endregion
    }
}

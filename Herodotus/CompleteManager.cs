namespace Herodotus
{
    /// <summary>
    ///  A manager that can potentially keeps all the changes user has made
    /// </summary>
    public class CompleteManager : TrackingManager, IChangesetManager
    {
        #region Properties

        /// <summary>
        ///  The current state node
        /// </summary>
        public StateNode CurrentStateNode
        {
            get; private set;
        }

        #endregion

        #region Methods

        #region IChangesetManager members

        public bool CanRedo()
        {
            return (CurrentStateNode.Branches.Count > 0);
        }

        public bool CanUndo()
        {
            return (CurrentStateNode.Parent != null);
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
            if (CanUndo())
            {
                CurrentStateNode.ChangeFromParent.Undo();
                CurrentStateNode = CurrentStateNode.Parent;
            }
        }

        #endregion

        #region TrackingManager members

        protected override void OnCommit()
        {
            var newNode = new StateNode
            {
                Parent = CurrentStateNode,
                ChangeFromParent = CommittingChangeset
            };
            var branch = new StateNode.Branch
            {
                Change = CommittingChangeset,
                Target = newNode
            };
            if (CurrentStateNode != null)
            {
                CurrentStateNode.Branches.Add(branch);
            }
            CurrentStateNode = newNode;
        }

        #endregion

        public void Redo(int branchIndex)
        {
            if (CanRedo())
            {
                var branch = CurrentStateNode.Branches[branchIndex];
                branch.Change.Redo();
                CurrentStateNode = branch.Target;
            }
        }

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
            var changeset = child.ChangeFromParent;
            node.Branches.Clear();
            node.Branches.Add(new StateNode.Branch
            {
                Change = changeset,
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
            for (var p = CurrentStateNode.Parent; p != null; p = p.Parent)
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
            node.Parent = null;
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

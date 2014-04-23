using System.Collections.Generic;

namespace Herodotus
{
    /// <summary>
    ///  A manager that can potentially keeps all the changes user has made
    /// </summary>
    public class CompleteManager : TrackingManager, ICompleteChangesetManager
    {
        #region Properties

        /// <summary>
        ///  The current state node
        /// </summary>
        public StateNode CurrentStateNode
        {
            get; set;
        }

        /// <summary>
        ///  The root node that represents the inital state
        /// </summary>
        public StateNode RootNode
        {
            get; set;
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
        public virtual void Redo()
        {
            Redo(CurrentStateNode.Branches.Count - 1);
        }

        /// <summary>
        ///  Undoes the changeset since parent
        /// </summary>
        public virtual void Undo()
        {
            CurrentStateNode.Parent.Changeset.Undo();
            CurrentStateNode = CurrentStateNode.Parent.Target;
        }

        #endregion

        public virtual void Reinitialize()
        {
            RootNode = BuildStateNode();
            CurrentStateNode = RootNode;
        }

        /// <summary>
        ///  Redoes a specified branch
        /// </summary>
        /// <param name="branchIndex">The branch to redo</param>
        public virtual void Redo(int branchIndex)
        {
            var branch = CurrentStateNode.Branches[branchIndex];
            branch.Changeset.Redo();
            CurrentStateNode = branch.Target;
        }

        /// <summary>
        ///  Makes a virtual undo (doesn't make changes but change the state/changeset pointer)
        /// </summary>
        public virtual void UndoVirtual()
        {
            CurrentStateNode = CurrentStateNode.Parent.Target;
        }

        /// <summary>
        ///  Makes a virtual redo (doesn't make changes but change the state/changeset pointer)
        /// </summary>
        /// <param name="branchIndex">The branch to redo</param>
        public virtual void RedoVirtual(int branchIndex)
        {
            var branch = CurrentStateNode.Branches[branchIndex];
            CurrentStateNode = branch.Target;
        }

        /// <summary>
        ///  Clear old branches of the current node and branches of all its ancestors
        /// </summary>
        public void GoLinear()
        {
            if (CurrentStateNode.Branches.Count > 1)
            {
                // removes all branches but the recently committed one
                CurrentStateNode.Branches.RemoveRange(0, CurrentStateNode.Branches.Count - 1);
            }
            var child = CurrentStateNode;
            for (var p = CurrentStateNode.Parent.Target; p != null; p = p.Parent.Target)
            {
                p.ClearBranchesBut(child);
                child = p;
            }
        }

        /// <summary>
        ///  Removes all nodes before the specified node which is then made root
        /// </summary>
        public void MakeRoot(StateNode node)
        {
            node.Parent = default(StateNode.Link);
            RootNode = node;
        }

        /// <summary>
        ///  Moves to the specified node on the tree with undos and/or redos
        /// </summary>
        /// <param name="target"></param>
        public void MoveTo(StateNode target)
        {
            if (CurrentStateNode == target)
            {
                return;
            }
            var pathToAncestor = GetPathTargetToCommonAncestor(CurrentStateNode, target);
            var ancestor = pathToAncestor[pathToAncestor.Count - 1];
            while (CurrentStateNode != ancestor)
            {
                Undo();
            }
            var nodeIndex = pathToAncestor.Count - 2;
            while (CurrentStateNode != target)
            {
                var next = pathToAncestor[nodeIndex];

                for (var branchIndex = 0; branchIndex < CurrentStateNode.Branches.Count; branchIndex++)
                {
                    var branch = CurrentStateNode.Branches[branchIndex];
                    if (branch.Target == next)
                    {
                        Redo(branchIndex);
                        break;
                    }
                }
                nodeIndex--;
            }
        }

        #endregion

        #region TrackingManager members

        protected override void OnCommit()
        {
            var newNode = BuildStateNode();
            newNode.Parent = new StateNode.Link
            {
                Changeset = CommittingChangeset,
                Target = CurrentStateNode
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
        ///  Creates a new state node of the type wanted with its default settings 
        /// </summary>
        /// <returns>The bran new state node</returns>
        protected virtual StateNode BuildStateNode()
        {
            return new StateNode();
        }

        /// <summary>
        ///  Returns the path from the target (inclusive) to the common ancestor of source and target
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private List<StateNode>  GetPathTargetToCommonAncestor(StateNode source, StateNode target)
        {
            var result = new List<StateNode>();

            // Adds the nodes from target all the way to the root
            for (var p = target; p != null; p = p.Parent.Target)
            {
                result.Add(p);
            }

            var sourceToRoot = new List<StateNode>();
            for (var p = source; p != null; p = p.Parent.Target)
            {
                sourceToRoot.Add(p);
            }

            // removes the common chain except the closest common ancestor
            int i, j;
            for (i = result.Count - 1, j = sourceToRoot.Count - 1; i >= 0 && j >= 0 && result[i] == sourceToRoot[j]; i--, j--)
            {
            }

            result.RemoveRange(i+2, result.Count-i-2);

            return result;
        }

        #endregion
    }
}

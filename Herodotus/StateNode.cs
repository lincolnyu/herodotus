using System.Collections.Generic;

namespace Herodotus
{
    public class StateNode
    {
        #region Nested types

        public struct Link
        {
            public Changeset Changeset;
            public StateNode Target;
        }

        #endregion

        #region Constructors

        public StateNode()
        {
            Branches = new List<Link>();
        }

        #endregion

        #region Properties

        public Link Parent
        {
            get; set;
        }

        public List<Link> Branches
        {
            get; private set;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Clear the branches of the node and therefore everything
        ///  down the node
        /// </summary>
        public void ClearBranches()
        {
            Branches.Clear();
        }

        /// <summary>
        ///  Clear all branches except for the branch that leads to the specified child
        /// </summary>
        /// <param name="childToKeep">The child to which the branch to spare</param>
        public void ClearBranchesBut(StateNode childToKeep)
        {
            var changeset = childToKeep.Parent.Changeset;
            Branches.Clear();
            Branches.Add(new StateNode.Link
            {
                Changeset = changeset,
                Target = childToKeep
            });
        }

        #endregion
    }
}

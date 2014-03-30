using System.Collections.Generic;

namespace Herodotus
{
    public class StateNode
    {
        #region Nested types

        public struct Link
        {
            public Changeset Change;
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
    }
}

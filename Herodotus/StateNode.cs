using System.Collections.Generic;

namespace Herodotus
{
    public class StateNode
    {
        #region Nested types

        public struct Branch
        {
            public Changeset Change;
            public StateNode Target;
        }

        #endregion

        #region Constructors

        public StateNode()
        {
            Branches = new List<Branch>();
        }

        #endregion

        #region Properties

        public StateNode Parent
        {
            get; set;
        }

        public Changeset ChangeFromParent
        {
            get; set;
        }

        public List<Branch> Branches
        {
            get; private set;
        }

        #endregion
    }
}

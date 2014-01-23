using System;
using Herodotus;

namespace TestApp
{
    public class ChangesetViewModel : ViewModelBase<Changeset>
    {
        #region Properties

        public string Description
        {
            get { return Model.Description; }
        }

        public string NumberOfChanges
        {
            get
            {
                return Model.Changes.Count.ToString();
            }
        }

        public new Changeset Model
        {
            get { return ModelAs<Changeset>(); }
        }

        #endregion

        #region Constructors

        public ChangesetViewModel(Changeset changeset)
            : base(changeset)
        {
        }

        #endregion
    }
}

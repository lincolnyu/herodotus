using Windows.UI.Xaml.Media;
using Herodotus;

namespace TestApp
{
    public class ChangesetViewModel : ViewModelBase<Changeset>
    {
        #region Fields

        private Brush _itemBrush;

        #endregion

        #region Properties

        public string Description
        {
            get { return Model.Descriptor as string; }
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

        public Brush ItemBrush
        {
            get
            {
                return _itemBrush;
            }
            set
            {
                if (!Equals(_itemBrush, value))
                {
                    _itemBrush = value;
                    OnPropertyChanged("ItemBrush");
                }
            }
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

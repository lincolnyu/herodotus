using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Herodotus;

namespace TestApp.ViewModels
{
    class MainPageViewModel : ViewModelBase<object>
    {
        #region Fields

        #region Change Tracking related

        private static MainPageViewModel _instance;

        private ObservableCollection<ChangesetViewModel> _changesets;

        #endregion

        #endregion

        #region Constructors

        public MainPageViewModel()
        {
            ((LinearChangesetManager)TrackingManager.Instance).Changesets.CollectionChanged += ChangesetCollectionChanged;
            ((LinearChangesetManager)TrackingManager.Instance).ChangeSetIndexChanged += OnChangeSetIndexChanged;
            ClearChangesetViewModels();
        }

        #endregion

        #region Properties

        public static MainPageViewModel Instance
        {
            get { return _instance ?? (_instance = new MainPageViewModel()); }
        }

        public ObservableCollection<ChangesetViewModel> Changesets
        {
            get { return _changesets ?? (_changesets = new ObservableCollection<ChangesetViewModel>()); }
        }

        public int SelectedChangesetIndex
        {
            get { return ((LinearChangesetManager)TrackingManager.Instance).CurrentChangeSetIndex; }
        }

        public static bool IsTrackingEnabled
        {
            get { return (TrackingManager.Instance).IsTrackingEnabled; }
            set { (TrackingManager.Instance).IsTrackingEnabled = value; }
        }

        #endregion


        #region Methods

        public void RestoreTo(int target)
        {
            if (target >= 0)
            {
                for (; ((LinearChangesetManager)TrackingManager.Instance).CurrentChangeSetIndex < target; )
                {
                    ((LinearChangesetManager)TrackingManager.Instance).Redo();
                }
                for (; ((LinearChangesetManager)TrackingManager.Instance).CurrentChangeSetIndex > target; )
                {
                    ((LinearChangesetManager)TrackingManager.Instance).Undo();   
                }
            }
        }

        public void RemoveFrom(int index)
        {
            ((LinearChangesetManager)TrackingManager.Instance).RemoveFrom(index);
        }

        public void RemoveTo(int index)
        {
            ((LinearChangesetManager)TrackingManager.Instance).RemoveTo(index);
        }

        public void ResetChangesets()
        {
            ((LinearChangesetManager)TrackingManager.Instance).RemoveAll();
        }

        private void ClearChangesetViewModels()
        {
            Changesets.Clear();
            Changesets.Add(new ChangesetViewModel(new Changeset("Initial")));
            UpdateColors();
        }

        private void ChangesetCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        Changesets.Add(new ChangesetViewModel((Changeset)item));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var index = e.OldStartingIndex + 1;
                    for (var c = 0; c < e.OldItems.Count; c++)
                    {
                        Changesets.RemoveAt(index);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearChangesetViewModels();
                    break;
            }
        }

        private void OnChangeSetIndexChanged()
        {
            UpdateColors();
            
            OnPropertyChanged("SelectedChangesetIndex");
        }

        private void UpdateColors()
        {
            var sel = SelectedChangesetIndex;
            for (var i = 0; i < Changesets.Count; i++)
            {
                var vm = Changesets[i];
                var color = (i == sel) ? Colors.Red : Colors.Green;
                vm.ItemBrush = new SolidColorBrush(color);
            }
        }

        #endregion
    }
}

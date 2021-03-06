﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Herodotus;
using Trollveggen;

namespace TestApp.ViewModels
{
    class MainPageViewModel : ViewModelBase<object>
    {
        #region Fields

        #region Change Tracking related

        private static MainPageViewModel _instance;

        private ObservableCollection<ChangesetViewModel> _changesets;

        private static ILinearChangesetManager _changesetManager;

        #endregion

        #endregion

        #region Constructors

        public MainPageViewModel()
        {
            ChangesetManager.Changesets.CollectionChanged += ChangesetCollectionChanged;
            ChangesetManager.ChangesetIndexChanged += OnChangesetIndexChanged;
            ClearChangesetViewModels();
        }

        #endregion

        #region Properties

        private static ILinearChangesetManager ChangesetManager
        {
            get { return _changesetManager ?? (_changesetManager = Factory.Resolve<ILinearChangesetManager>()); }
        }

        private static ITrackingManager TrackingManager
        {
            get { return (ITrackingManager)ChangesetManager; }
        }

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
            get { return ChangesetManager.CurrentChangesetIndex; }
        }

        public static bool IsTrackingEnabled
        {
            get { return TrackingManager.IsTrackingEnabled; }
            set { TrackingManager.IsTrackingEnabled = value; }
        }

        public bool CanUndo
        {
            get
            {
                return ChangesetManager.CanUndo();
            }
        }

        public bool CanRedo
        {
            get
            {
                return ChangesetManager.CanRedo();
            }
        }
        

        #endregion

        #region Methods

        public void RestoreTo(int target)
        {
            if (target >= 0)
            {
                for (; ChangesetManager.CurrentChangesetIndex < target; )
                {
                    ChangesetManager.Redo();
                }
                for (; ChangesetManager.CurrentChangesetIndex > target; )
                {
                    ChangesetManager.Undo();   
                }
            }
        }

#if false
        public void RemoveFrom(int index)
        {
            ChangesetManager.RemoveFrom(index);
        }

        public void RemoveTo(int index)
        {
            ChangesetManager.RemoveTo(index);
        }

        public void ResetChangesets()
        {
            ChangesetManager.RemoveAll();
        }
#endif

        private void ClearChangesetViewModels()
        {
            Changesets.Clear();
            Changesets.Add(new ChangesetViewModel(new Changeset(TrackingManager, "Initial")));
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

        private void OnChangesetIndexChanged()
        {
            UpdateColors();
            
            OnPropertyChanged("SelectedChangesetIndex");
            OnPropertyChanged("CanUndo");
            OnPropertyChanged("CanRedo");
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

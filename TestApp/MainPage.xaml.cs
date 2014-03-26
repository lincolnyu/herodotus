using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Herodotus;
using TestApp.ViewModels;
using Trollveggen;

namespace TestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Fields

        private readonly IList<IManagedShape> _managedShapes = new ObservableCollection<IManagedShape>();

        private uint _pressId;
        private double _startX;
        private double _startY;

        ManagedLine _draggedLine;
        bool _startPoint;

        private Line _tempLine;

        #endregion

        #region Constructors

        public MainPage()
        {
            InitializeComponent();

            //Factory.Register<IChangesetManager>(new LinearChangesetManager());
            Factory.Register<IChangesetManager>(new CompleteManagerLinearExtension());

            MainCanvas.DoubleTapped += MainCanvasOnDoubleTapped;
            MainCanvas.PointerPressed += MainCanvasOnPointerPressed;
            MainCanvas.PointerMoved += MainCanvasOnPointerMoved;
            MainCanvas.PointerReleased += MainCanvasOnPointerReleased;

            BtnRedo.Click += BtnRedoOnClick;
            BtnUndo.Click += BtnUndoOnClick;

            var observableShapeCollection = (ObservableCollection<IManagedShape>)_managedShapes;
            observableShapeCollection.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems != null)
                {
                    foreach (var oldItem in args.OldItems)
                    {
                        var mshape = (IManagedShape)oldItem;
                        MainCanvas.Children.Remove(mshape.InnerShape);
                    }
                }
                if (args.NewItems != null)
                {
                    foreach (var newItem in args.NewItems)
                    {
                        var mshape = (IManagedShape)newItem;
                        MainCanvas.Children.Add(mshape.InnerShape);
                    }
                }
                TrackingManager.Instance.OnCollectionChanged<IManagedShape>(sender, args);
            };

            TrackingManager.Instance.IsTrackingEnabled = true;
            DataContext = MainPageViewModel.Instance;
        }

        #endregion

        #region Methods

        #region Event handlers

        private void MainCanvasOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(MainCanvas);
            var x1 = pos.X - 100;
            var y1 = pos.Y - 100;
            var x2 = pos.X + 100;
            var y2 = pos.Y + 100;
            var link = new ManagedLine
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2
            };
            _managedShapes.Add(link);
        }

        private void MainCanvasOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pp = e.GetCurrentPoint(MainCanvas);
            _pressId = pp.PointerId;
            var pos = pp.Position;

            if (HitTest(pos, out _draggedLine, out _startPoint))
            {
                TrackingManager.Instance.StartChangeset("Move");
            }
            else
            {
                _startX = pos.X;
                _startY = pos.Y;
            }
        }

        private void MainCanvasOnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var pp = e.GetCurrentPoint(MainCanvas);
            if (_pressId != pp.PointerId)
            {
                return;
            }
            var pos = pp.Position;
            var currX = pos.X;
            var currY = pos.Y;

            if (_draggedLine != null)
            {
                if (_startPoint)
                {
                    _draggedLine.X1 = currX;
                    _draggedLine.Y1 = currY;
                }
                else
                {
                    _draggedLine.X2 = currX;
                    _draggedLine.Y2 = currY;
                }
            }
            else
            {
                if (_tempLine != null)
                {
                    MainCanvas.Children.Remove(_tempLine);
                    _tempLine = null;
                }

                _tempLine = new Line
                {
                    X1 = _startX,
                    Y1 = _startY,
                    X2 = currX,
                    Y2 = currY,
                    Stroke = new SolidColorBrush(Colors.Purple)
                };

                MainCanvas.Children.Add(_tempLine);
            }
        }

        private void MainCanvasOnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var pp = e.GetCurrentPoint(MainCanvas);
            if (_pressId != pp.PointerId)
            {
                return;
            }

            if (_draggedLine != null)
            {
                _draggedLine = null;
                _pressId = 0;
                TrackingManager.Instance.Commit(true);
                return;
            }

            var pos = pp.Position;

            var endX = pos.X;
            var endY = pos.Y;
            
            if (_tempLine != null)
            {
                MainCanvas.Children.Remove(_tempLine);
                _tempLine = null;
            }

            const double minLineLen = 10;
            if (Distance(_startX, _startY, endX, endY) > minLineLen)
            {
                var desc = string.Format("Add Line ({0:0.00},{1:0.00})-({2:0.00},{3:0.00})", _startX, _startY, endX, endY);
                TrackingManager.Instance.StartChangeset(desc);
                var link = new ManagedLine
                {
                    X1 = _startX,
                    Y1 = _startY,
                    X2 = endX,
                    Y2 = endY
                };

                _managedShapes.Add(link);

                TrackingManager.Instance.Commit();
            }

            _pressId = 0;
        }

        private void BtnUndoOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            ((IChangesetManager)TrackingManager.Instance).Undo();
        }

        private void BtnRedoOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            ((IChangesetManager)TrackingManager.Instance).Redo();
        }

        #endregion

        private double Distance(double x1, double y1, double x2, double y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return Math.Sqrt(dx*dx + dy*dy);
        }

        private bool HitTest(Point point, out ManagedLine hitLine, out bool startPoint)
        {
			const double epsilon = 5;
			
            foreach (var shape in _managedShapes.Reverse())
            {
                var line = shape as ManagedLine;
                if (line == null) continue;
                var dist1 = Distance(line.X1, line.Y1, point.X, point.Y);
                var dist2 = Distance(line.X2, line.Y2, point.X, point.Y);
                //var lineLen = Distance(line.X1, line.Y1, line.X2, line.Y2);
				if (dist1 < dist2 && dist1 < epsilon)
				{
					hitLine = line;
					startPoint = true;
				    return true;
				}
				if (dist2 < dist1 && dist2 < epsilon)
				{
					hitLine = line;
					startPoint = false;
				    return true;
				}
            }
            hitLine = null;
            startPoint = false;
            return false;
        }

        #endregion

    }
}

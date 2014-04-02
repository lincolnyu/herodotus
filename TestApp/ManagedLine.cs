using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Herodotus;
using TestApp.ChangeTracking;

namespace TestApp
{
    class ManagedLine : IManagedShape
    {
        #region Constructors

        public ManagedLine()
        {
            InnerLine = new Line {Stroke = new SolidColorBrush(Colors.Green), StrokeThickness = 3};
        }

        #endregion

        #region Properties

        #region IManagedShape members

        public Shape InnerShape
        {
            get
            {
                return InnerLine;
            }
        }

        #endregion

        public double X1
        {
            get
            {
                return InnerLine.X1;
            }
            set
            {
                if (InnerLine.X1.Equals(value))
                {
                    return;
                }

                using (new GlobalPropertyChangeMarker(this, "X1", value))
                {
                    InnerLine.X1 = value;
                }
            }
        }

        public double Y1
        {
            get
            {
                return InnerLine.Y1;
            }
            set
            {
                using (new GlobalPropertyChangeMarker(this, "Y1", value))
                {
                    InnerLine.Y1 = value;
                }
            }
        }

        public double X2
        {
            get
            {
                return InnerLine.X2;
            }
            set
            {
                using (new GlobalPropertyChangeMarker(this, "X2", value))
                {
                    InnerLine.X2 = value;
                }
            }
        }

        public double Y2
        {
            get
            {
                return InnerLine.Y2;
            }
            set
            {
                using (new GlobalPropertyChangeMarker(this, "Y2", value))
                {
                    InnerLine.Y2 = value;
                }
            }
        }

        public Line InnerLine
        {
            get; private set;
        }

        #endregion
    }
}

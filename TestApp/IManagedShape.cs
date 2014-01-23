using Windows.UI.Xaml.Shapes;

namespace TestApp
{
    interface IManagedShape
    {
        #region Proepties

        Shape InnerShape { get; }

        #endregion
    }
}

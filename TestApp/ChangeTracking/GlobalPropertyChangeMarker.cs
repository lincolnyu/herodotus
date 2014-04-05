using Herodotus;
using Trollveggen;

namespace TestApp.ChangeTracking
{
    public class GlobalPropertyChangeMarker : PropertyChangeMarker
    {
        #region Fields

        private static ITrackingManager _trackingManager;

        #endregion

        #region Constructors

        public GlobalPropertyChangeMarker(object owner, string propertyName, object targetValue)
            : base(TrackingManager, owner, propertyName, targetValue)
        {
        }

        #endregion

        #region Properties

        public static ITrackingManager TrackingManager
        {
            get
            {
                return _trackingManager ?? (_trackingManager = Factory.Resolve<ITrackingManager>());
            }
        }

        #endregion
    }
}

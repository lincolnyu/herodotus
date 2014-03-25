using System;

namespace Herodotus
{
    public class GlobalPropertyChangeMarker : IDisposable
    {
        #region Constructors

        public GlobalPropertyChangeMarker(object owner, string propertyName, object targetValue)
        {
            TrackingManager.Instance.TrackPropertyChangeBegin(owner, propertyName, targetValue);
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            TrackingManager.Instance.TrackPropertyChangeEnd();
        }

        #endregion

        public static void Cancel()
        {
            (TrackingManager.Instance).TrackPropertyChangeCancel();
        }

        #endregion
    }
}

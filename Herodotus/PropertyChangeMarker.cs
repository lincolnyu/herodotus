using System;

namespace Herodotus
{
    public class PropertyChangeMarker : IDisposable
    {
        #region Fields

        private readonly ITrackingManager _trackingManager;

        #endregion

        #region Constructors

        public PropertyChangeMarker(ITrackingManager trackingManager, object owner, string propertyName,
            object targetValue)
        {
            _trackingManager = trackingManager;
            trackingManager.TrackPropertyChangeBegin(owner, propertyName, targetValue);
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            _trackingManager.TrackPropertyChangeEnd();
        }

        #endregion

        public void Cancel()
        {
            _trackingManager.TrackPropertyChangeCancel();
        }

        #endregion
    }
}

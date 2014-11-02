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
            if (trackingManager != null)
            {
                trackingManager.TrackPropertyChangeBegin(owner, propertyName, targetValue);
            }
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_trackingManager != null)
            {
                _trackingManager.TrackPropertyChangeEnd();
            }
        }

        #endregion

        public void Cancel()
        {
            if (_trackingManager != null)
            {
                _trackingManager.TrackPropertyChangeCancel();
            }
        }

        #endregion
    }
}

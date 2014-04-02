using System;

namespace Herodotus
{
    public class PropertyChangeMarker : IDisposable
    {
        #region Fields

        private readonly TrackingManager _changesetManager;

        #endregion

        #region Constructors

        public PropertyChangeMarker(TrackingManager changesetManager, object owner, string propertyName,
            object targetValue)
        {
            _changesetManager = changesetManager;
            changesetManager.TrackPropertyChangeBegin(owner, propertyName, targetValue);
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            _changesetManager.TrackPropertyChangeEnd();
        }

        #endregion

        public void Cancel()
        {
            _changesetManager.TrackPropertyChangeCancel();
        }

        #endregion
    }
}

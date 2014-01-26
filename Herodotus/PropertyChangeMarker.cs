using System;

namespace Herodotus
{
    internal class PropertyChangeMarker : IDisposable
    {
        #region Fields

        private readonly ChangesetManager _changesetManager;

        #endregion

        #region Constructors

        public PropertyChangeMarker(ChangesetManager changesetManager, object owner, string propertyName,
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

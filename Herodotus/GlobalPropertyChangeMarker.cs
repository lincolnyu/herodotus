using System;

namespace Herodotus
{
    public class GlobalPropertyChangeMarker : IDisposable
    {
        #region Constructors

        public GlobalPropertyChangeMarker(object owner, string propertyName, object targetValue)
        {
            ChangesetManager.Instance.TrackPropertyChangeBegin(owner, propertyName, targetValue);
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            ChangesetManager.Instance.TrackPropertyChangeEnd();
        }

        #endregion

        #endregion
    }
}

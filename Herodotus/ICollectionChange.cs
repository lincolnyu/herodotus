using System.Collections;
using System.Collections.Specialized;

namespace Herodotus
{
    public interface ICollectionChange
    {
        #region Properties

        object Collection { get; }

        NotifyCollectionChangedAction Action { get; }

        IList OldItems { get; set; }

        IList NewItems { get; set; }

        int NewStartingIndex { get; }

        int OldStartingIndex { get; set; }

        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Specialized;

namespace Herodotus
{
    public interface ICollectionChange
    {
        #region Properties

        object Collection { get; set; }

        NotifyCollectionChangedAction Action { get; set; }

        IList OldItems { get; set; }

        IList NewItems { get; set; }

        int NewStartingIndex { get; set; }

        int OldStartingIndex { get; set; }

        Type ItemType { get; }

        #endregion
    }
}

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace Herodotus
{
    public abstract class TrackingManager : ITrackingManager
    {
        #region Fields

        /// <summary>
        ///  A flag that indicates if a changeset is currently undergoing undo/redo
        /// </summary>
        protected bool IsUndoRedoing;

        /// <summary>
        ///  A counter that indicates the current level of nested tracking
        /// </summary>
        private int _trackingDepth;

        private object _trackedObject;
        private PropertyInfo _trackedProperty;
        private object _trackedOldValue;
        private object _trackedNewValue;

        #endregion

        #region Properties

        #region ITrackingManager members

        public bool IsTrackingEnabled { get; set; }

        public bool IsTrackingSuspended
        {
            get
            {
                return (!IsTrackingEnabled || IsUndoRedoing || _trackingDepth > 0);
            }
        }

        public Changeset CommittingChangeset
        {
            get;
            protected set;
        }

        public int NestCount
        {
            get;
            private set;
        }

        /// <summary>
        ///  Whether to perform merge right after a change is tracked and added to the current changeset
        /// </summary>
        public bool MergeOnTheGo
        {
            get;
            set;
        }

        #endregion

        #endregion

        #region Methods

        #region ITrackingManager

        #region Tracking handlers

        public void TrackPropertyChangeBegin(object owner, string propertyName, object targetValue)
        {
            lock (this)
            {
                if (IsTrackingSuspended || CommittingChangeset == null)
                {
                    _trackingDepth++;
                    return;
                }

                var type = owner.GetType();

                _trackedObject = owner;
                _trackedProperty = type.GetRuntimeProperty(propertyName);

                _trackedOldValue = _trackedProperty.GetValue(owner, null);
                _trackedNewValue = targetValue;

                _trackingDepth++;
            }
        }

        public void TrackPropertyChangeEnd()
        {
            lock (this)
            {
                if (_trackingDepth == 1 && CommittingChangeset != null && _trackedObject != null)
                {
                    CommittingChangeset.AddPropertyChange(_trackedObject, _trackedProperty, _trackedOldValue, _trackedNewValue);
                    ClearTrackedProperty();
                }

                if (_trackingDepth > 0)
                {
                    _trackingDepth--;
                }
            }
        }

        public void TrackPropertyChangeCancel()
        {
            lock (this)
            {
                if (_trackingDepth == 1)
                {
                    ClearTrackedProperty();
                }
            }
        }

        public void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CommittingChangeset == null) return;
            if (IsTrackingSuspended) return;
            CommittingChangeset.OnCollectionChanged<T>(sender, e);
        }

        public void OnCollectionClearing<T>(ICollection<T> collection)
        {
            if (CommittingChangeset == null) return;
            if (IsTrackingSuspended) return;
            CommittingChangeset.OnCollectionClearing(collection);
        }

        #endregion

        #region Changeset operations

        public int StartChangeset(object descriptor = null, IChangesetBuilder changesetBuilder = null)
        {
            lock (this)
            {
                if (NestCount++ > 0)
                {
                    return NestCount;
                }
                if (!IsTrackingEnabled)
                {
                    return NestCount;
                }
                CommittingChangeset = changesetBuilder!= null? changesetBuilder.BuildChangeset(this, descriptor) : 
                    new Changeset(this, descriptor);
                return NestCount;
            }
        }

        public int Commit(bool merge = false, bool commitEmpty = false)
        {
            int nestCount;
            lock (this)
            {
                nestCount = NestCount;
                if (NestCount <= 0)
                {
                    NestCount = 0;
                    return nestCount;
                }
                if (--NestCount > 0)
                {
                    return nestCount;
                }
                if (!IsTrackingEnabled)
                {
                    return nestCount;
                }
            }

            if (!commitEmpty && CommittingChangeset.Changes.Count == 0)
            {
                CommittingChangeset = null;
                return nestCount;
            }

            if (merge)
            {
                CommittingChangeset.Merge();
            }

            OnCommit();

            CommittingChangeset = null;
            return nestCount;
        }

        /// <summary>
        ///  Rolls back the current changeset
        /// </summary>
        /// <param name="merge">Merge the changeset before undoing it</param>
        public void Rollback(bool merge = false)
        {
            if (CommittingChangeset == null) return;

            if (merge)
            {
                CommittingChangeset.Merge();
            }
            CommittingChangeset.Undo();

            // rolled back changeset can't be committed again 
            CommittingChangeset = null;
        }

        /// <summary>
        ///  Cancels the commitment of the current changeset
        /// </summary>
        public void Cancel()
        {
            CommittingChangeset = null;
        }

        #endregion

        #endregion

        protected abstract void OnCommit();

        private void ClearTrackedProperty()
        {
            _trackedObject = null;
        }

        #endregion
    }
}

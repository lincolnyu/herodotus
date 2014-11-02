using System.Collections.Generic;
using System.Collections.Specialized;

namespace Herodotus
{
    public interface ITrackingManager
    {
        #region Properties

        /// <summary>
        ///  For user to turn on or off tracking
        /// </summary>
        bool IsTrackingEnabled { get; set; }

        /// <summary>
        ///  Whether the tracking is currently suspended (NOTE the existence of 
        ///  any factor that prevents tracking such as IsTrackingEnabled being false
        ///  makes this property true)
        /// </summary>
        bool IsTrackingSuspended { get; }

        /// <summary>
        ///  The changeset that has started and yet to commit
        /// </summary>
        Changeset CommittingChangeset { get; }

        /// <summary>
        ///  Current level of nested StartChangeset() call
        /// </summary>
        int NestCount { get; }

        /// <summary>
        ///  Whether to perform merge right after a change is tracked and added to the current changeset
        /// </summary>
        bool MergeOnTheGo { get; set; }

        #endregion

        #region Methods

        #region Tracking handlers

        /// <summary>
        ///  Initiates a property change
        /// </summary>
        /// <param name="owner">The owner of the property</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="targetValue">The value the property is changing to</param>
        void TrackPropertyChangeBegin(object owner, string propertyName, object targetValue);

        /// <summary>
        ///  Finalises a property change
        /// </summary>
        void TrackPropertyChangeEnd();

        /// <summary>
        ///  Cancels a property change tracking (so no changes will be recorded upon the call to TrackPropertyChangeEnd())
        /// </summary>
        /// <remarks>
        ///  NOTE It doesn't undo the actual change made to the real property value
        /// </remarks>
        void TrackPropertyChangeCancel();

        /// <summary>
        ///  Handles general changes to a collection
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection</typeparam>
        /// <param name="sender">The sender of the change MUST BE the collection itself</param>
        /// <param name="e">The change event argument</param>
        void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e);
        
        /// <summary>
        ///  Handles a collection reset/clearing event
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection</typeparam>
        /// <param name="collection">The collection whose changes are to be handled</param>
        void OnCollectionClearing<T>(ICollection<T> collection);

        #endregion

        #region Changeset operations

        /// <summary>
        ///  Starts a new changeset to track change
        /// </summary>
        /// <param name="descriptor">The descriptor object for this changeset</param>
        /// <param name="changesetBuilder">The object that builds a new changeset object if any</param>
        /// <returns>The level of nested call() to this method, for instancre 1 for the first call to this</returns>
        int StartChangeset(object descriptor = null, IChangesetBuilder changesetBuilder = null);

        /// <summary>
        ///  Commits the current changeset
        /// </summary>
        /// <param name="merge">If merging redudant changesets</param>
        /// <param name="commitEmpty">If committing even if it contains no changesets</param>
        /// <returns>The level of nested StartChangset() call before commitment</returns>
        int Commit(bool merge = false, bool commitEmpty = false);

        /// <summary>
        ///  Rolls back the current changest
        /// </summary>
        /// <param name="merge">True if merging the changeset before undoing the changes so far</param>
        void Rollback(bool merge = false);

        /// <summary>
        ///  Cancels the commitment of the current changeset
        /// </summary>
        void Cancel();

        #endregion

        #endregion
    }
}

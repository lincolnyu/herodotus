using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace TestApp
{
    public abstract class ViewModelBase<TModel> : INotifyPropertyChanged, IDisposable where TModel : class
    {
        #region Properties

        /// <summary>
        ///  The data model this instance of view-model represents
        /// </summary>
        public TModel Model { get; protected set; }

        /// <summary>
        /// Returns the user-friendly name of this object.
        /// Child classes can set this property to a new value,
        /// or override it to determine the value on-demand.
        /// </summary>
        public virtual string DisplayName { get; set; }

        #endregion

        #region Events

        #region Implementation of INotifyPropertyChanged

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates the base class
        /// </summary>
        // ReSharper disable EmptyConstructor
        protected ViewModelBase()
        // ReSharper restore EmptyConstructor
        {
        }

        /// <summary>
        ///  Instantiates the base class with given model
        /// </summary>
        /// <param name="model">The model the instance of the class represents</param>
        protected ViewModelBase(TModel model)
        {
            Model = model;
        }

        #endregion

        #region Methods

        #region Implementation of IDispoable

        /// <summary>
        ///  Invoked when this object is being removed from the application
        ///  and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            OnDispose();
        }

        #endregion

        /// <summary>
        ///  Converts the contained model to specified type
        /// </summary>
        /// <typeparam name="T">The specified type to convert to</typeparam>
        /// <returns>The result of the conversion</returns>
        public T ModelAs<T>() where T : class
        {
            return Model as T;
        }

        /// <summary>
        ///  Child classes can override this method to perform 
        ///  clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>
        ///  Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);

            var handler = PropertyChanged;
            if (handler == null) return;
            var e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }


        #endregion

        #region Debugging Aides

        /// <summary>
        ///  Warns the developer if this object does not have
        ///  a public property with the specified name. This 
        ///  method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            var t = GetType();
            
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (t.GetRuntimeProperty(propertyName) != null) return;

            var msg = "Invalid property name: " + propertyName;

            if (ThrowOnInvalidPropertyName)
            {
                throw new Exception(msg);
            }

            Debug.Assert(false);
        }

        /// <summary>
        ///  Returns whether an exception is thrown, or if a Debug.Fail() is used
        ///  when an invalid property name is passed to the VerifyPropertyName method.
        ///  The default value is false, but subclasses used by unit tests might 
        ///  override this property's getter to return true.
        /// </summary>
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        #endregion // Debugging Aides

    }
}

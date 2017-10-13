using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Loki.SignalServer.Interfaces.Utility;

namespace Loki.SignalServer.Common.Utility
{
    public class ParameterList : IParameterList
    {
        #region Readonly Variables

        /// <summary>
        /// The backing store
        /// </summary>
        private readonly ConcurrentDictionary<string,dynamic> _backingStore = new ConcurrentDictionary<string, dynamic>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="dynamic"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="dynamic"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public dynamic this[string index]
        {
            get => _backingStore.ContainsKey(index) ? _backingStore[index] : null;
            set
            {
                int i = 0;

                while (i < 3) { 
                    _backingStore.TryAdd(index, value);
                    ++i;
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterList"/> class.
        /// </summary>
        public ParameterList()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterList"/> class.
        /// </summary>
        /// <param name="list">The list.</param>
        public ParameterList(IDictionary<string, dynamic> list)
        {
            foreach (var item in list)
                this[item.Key] = item.Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterList"/> class.
        /// </summary>
        /// <param name="existingList">The existing list.</param>
        public ParameterList(ParameterList existingList)
        {
            foreach (var item in existingList)
                this[item.Key] = item.Value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(string key, dynamic value)
        {
            this[key] = value;
        }

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="list">The list.</param>
        public void AddRange(IParameterList list)
        {
            foreach (var item in list)
                this[item.Key] = item.Value;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, dynamic>> GetEnumerator()
        {
            foreach (var item in _backingStore)
                yield return item;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
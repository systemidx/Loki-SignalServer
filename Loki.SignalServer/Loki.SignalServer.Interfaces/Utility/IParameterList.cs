using System.Collections.Generic;

namespace Loki.SignalServer.Interfaces.Utility
{
    public interface IParameterList : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Gets or sets the <see cref="dynamic"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="dynamic"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        dynamic this[string index] { get; set; }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void Add(string key, dynamic value);

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="list">The list.</param>
        void AddRange(IParameterList list);
    }
}
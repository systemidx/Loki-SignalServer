using Microsoft.WindowsAzure.Storage.Table;

namespace Loki.SignalServer.Interfaces.Tables
{
    public interface ITableHandler
    {
        /// <summary>
        /// Retrieves the specified partition key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns></returns>
        T Retrieve<T>(string partitionKey, string rowKey) where T : class, ITableEntity;

        /// <summary>
        /// Inserts the or replace.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">The entity.</param>
        void InsertOrReplace<T>(T entity) where T: class, ITableEntity;
    }
}
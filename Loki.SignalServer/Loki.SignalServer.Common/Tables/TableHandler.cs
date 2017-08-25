using System;
using Loki.SignalServer.Interfaces.Tables;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Loki.SignalServer.Common.Tables
{
    public class TableHandler : ITableHandler
    {
        private readonly CloudStorageAccount _account;
        private readonly CloudTableClient _client;
        private readonly CloudTable _table;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableHandler"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <exception cref="ArgumentNullException">connectionString</exception>
        public TableHandler(string connectionString, string tableName)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _account = CloudStorageAccount.Parse(connectionString);
            _client = _account.CreateCloudTableClient();

            _table = _client.GetTableReference(tableName);
            //_table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the specified partition key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns></returns>
        public T Retrieve<T>(string partitionKey, string rowKey) where T: class, ITableEntity
        {
            TableOperation operation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult result = _table.ExecuteAsync(operation).GetAwaiter().GetResult();

            return result.Result as T;
        }

        /// <summary>
        /// Inserts the or replace.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">The entity.</param>
        public void InsertOrReplace<T>(T entity) where T: class, ITableEntity
        {
            TableOperation.InsertOrReplace(entity);
        }
    }
}

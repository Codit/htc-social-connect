using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CommunicationApi.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using TableStorage.Abstractions.TableEntityConverters;

namespace CommunicationApi.Services.Tablestorage
{
    public abstract class TableTransmitter<T> : AzureStorageProvider where T : new()
    {
        private readonly IDictionary<string, CloudTable> _loadedTables = new Dictionary<string, CloudTable>();

        protected string TableName { get; set; }

        protected TableTransmitter(string tableName, ISecretProvider secretProvider, ILogger logger)
            : base(secretProvider, logger)
        {
            TableName = tableName;
        }

        protected async Task<CloudTable> GetTable()
        {
            if (!_loadedTables.ContainsKey(TableName))
            {
                try
                {
                    var storageAccount = await GetStorageAccount();
                    var table = storageAccount.CreateCloudTableClient().GetTableReference(TableName);
                    await table.CreateIfNotExistsAsync();

                    _loadedTables.Upsert(TableName, table);
                }
                catch (Exception exception)
                {
                    // ignored
                    Logger.LogCritical(exception, "Unable to upsert in table {TableName}", TableName);
                }
            }

            return _loadedTables[TableName];
        }

        protected async Task<T> GetItem(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                throw new KeyNotFoundException($"Trying to get entity with empty rowkey from table {TableName}");
            }

            //Table  
            var table = await GetTable();

            //Operation  
            var operation = TableOperation.Retrieve(partitionKey, rowKey);

            //Execute  
            var result = await table.ExecuteAsync(operation);
            if (result.Result != null)
            {
                var resultEntity = result.Result as DynamicTableEntity;
                return resultEntity.FromTableEntity<T>();
            }

            return default;
        }

        public async Task<List<T>> GetItems(string partitionKey)
        {
            //Table  
            var table = await GetTable();

            var query =
                new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                    partitionKey));
            var entities = new List<T>();


            var continuationToken = new TableContinuationToken();
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                entities.AddRange(segment.Select(ParseTableEntity));
                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            return entities;
        }

        protected virtual T ParseTableEntity(DynamicTableEntity dte)
        {
            return dte.FromTableEntity<T>();
        }

        protected async Task Insert(T entity, string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                throw new KeyNotFoundException(
                    $"Trying to insert entity with empty rowkey in table {TableName}:\r\n{PrintEntity(entity)}");
            }

            //Table  
            var table = await GetTable();

            var dte = entity.ToTableEntity(partitionKey, rowKey);
            var tableEntity = entity as DynamicTableEntity ?? entity.ToTableEntity(partitionKey, rowKey);

            //Operation  
            var operation = TableOperation.Insert(tableEntity);

            //Execute  
            await table.ExecuteAsync(operation);
        }

        protected async Task InsertBatch(IEnumerable<DynamicTableEntity> entities)
        {
            int batchSize = 99;
            //Table  
            var table = await GetTable();

            int remainder, batches = Math.DivRem(entities.Count(), batchSize, out remainder);
            for (int iteration = 0; iteration <= batches; iteration++)
            {
                var batchOperation = new TableBatchOperation();
                foreach (var dynamicTableEntity in entities.Skip(iteration * batchSize).Take(batchSize))
                {
                    batchOperation.InsertOrReplace(dynamicTableEntity);
                }

                if (batchOperation.Count > 0)
                {
                    Console.WriteLine($"Inserting batch {iteration} to table");
                    await table.ExecuteBatchAsync(batchOperation);
                }
            }
        }

        protected async Task Upsert(T entity, string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                throw new KeyNotFoundException(
                    $"Trying to update entity with empty rowkey in table {TableName}:\r\n{PrintEntity(entity)}");
            }

            //Table  
            var table = await GetTable();

            var tableEntity = entity as DynamicTableEntity ?? entity.ToTableEntity(partitionKey, rowKey);
            //Operation  
            var operation = TableOperation.InsertOrReplace(tableEntity);

            //Execute  
            await table.ExecuteAsync(operation);
        }

        protected async Task Delete(string partitionKey)
        {
            int batchSize = 100;
            //Table  
            var table = await GetTable();
            var query =
                new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,                    partitionKey))
                    .Select(new string[] { "PartitionKey", "RowKey" });

            TableContinuationToken continuationToken = null;

            do
            {
                var tableQueryResult = table.ExecuteQuerySegmentedAsync(query, continuationToken);

                continuationToken = tableQueryResult.Result.ContinuationToken;

                List<List<DynamicTableEntity>> batch = tableQueryResult.Result.Select((x, index) => new { Index = index, Value = x })
                    .Where(x => x.Value != null)
                    .GroupBy(x => x.Index / batchSize)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (List<DynamicTableEntity> rows in batch)
                {
                    TableBatchOperation tableBatchOperation = new TableBatchOperation();
                    rows.ForEach(x => tableBatchOperation.Add(TableOperation.Delete(x)));

                    await table.ExecuteBatchAsync(tableBatchOperation);
                }
            }
            while (continuationToken != null);
        }

        public string PrintEntity(T entity)
        {
            var output = new StringBuilder();
            foreach (var prop in entity.GetType().GetProperties())
            {
                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                output.AppendLine($"- {prop.Name}:{prop.GetValue(entity, null)}");
            }

            return output.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunicationApi.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using TableStorage.Abstractions.TableEntityConverters;

namespace CommunicationApi.Services.Tablestorage
{
    public static class DictionaryExtensions
    {
        public static void Upsert<K, V>(this IDictionary<K, V> dictionary, K key, V value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
    }
    
    public abstract class TableTransmitter<T> where T : new()
    {
        private StorageSettings _settings;
        private CloudStorageAccount _storageAccount;
        private IDictionary<string, CloudTable> _loadedTables = new Dictionary<string, CloudTable>();
        protected string TableName { get; set; }

        protected TableTransmitter(StorageSettings settings, string tableName)
        {
            _settings = settings;
            TableName = tableName;
        }

        private CloudStorageAccount Account
        {
            get
            {
                if (_storageAccount == null)
                {
                    _storageAccount = new CloudStorageAccount(
                        new StorageCredentials(_settings.AccountName, _settings.AccountKey), true);
                }

                return _storageAccount;
            }
        }

        protected async Task<CloudTable> GetTable()
        {
            if (!_loadedTables.ContainsKey(TableName))
            {
                var table = Account.CreateCloudTableClient().GetTableReference(TableName);
                await table.CreateIfNotExistsAsync();
                try
                {
                    _loadedTables.Upsert(TableName, table);
                }
                catch (Exception)
                {
                    // ignored
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

            return default(T);
        }


        public async Task<List<T>> GetItems(string partitionKey)
        {
            //Table  
            var table = await GetTable();

            var query = new TableQuery();
            var entities = new List<T> { };


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
# TableService

This `TableService` class is an implementation of `ITableService` that interacts with **Azure Table Storage** using the `Azure.Data.Tables` SDK. It provides methods for common table storage operations, such as **retrieving, inserting, updating, deleting, and querying entities**.

## **Key Features & Methods**

1. **Dependency Injection**

   - The class takes a `TableServiceClient` instance as a dependency via its constructor.
   - This client is used to create `TableClient` instances for specific tables.

2. **CRUD Operations**

   - **`UpsertEntity<T>`**
     - Inserts or updates an entity in a specified table.
   - **`DeleteEntity`**
     - Deletes an entity by **PartitionKey** and **RowKey**.

3. **Retrieving Entities**

   - **`GetEntity<T>(tableName, partitionKey, rowKey)`**
     - Fetches a single entity based on both `PartitionKey` and `RowKey`.
   - **`GetEntity<T>(tableName, partitionKey)`**
     - Fetches a single entity using only `PartitionKey`.
   - **`GetFirstEntity<T>(tableName)`**
     - Retrieves the first entity from the table.
   - **`GetEntities<T>(tableName, query)`**
     - Retrieves multiple entities matching a query.
   - **`GetPaginatedEntities<T>(tableName, query, maxPerPage, continuationToken)`**
     - Fetches paginated results, returning a tuple containing:
     - `values` → The retrieved entities.
     - `continuationToken` → Token for fetching the next page.

## Class content

```c#
using Azure.Data.Tables;

namespace myproject.project_1.data.Services.TableService;

public class TableService : ITableService
{
    private readonly TableServiceClient _tableServiceClient;
    public TableService(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

    public async Task DeleteEntity(string partitionKey, string rowKey, string tableName)
    {
        TableClient entTable = _tableServiceClient.GetTableClient(tableName);
        await entTable.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task<T?> GetFirstEntity<T>(string tableName) where T : class, ITableEntity, new()
    {
        TableClient entTable = _tableServiceClient.GetTableClient(tableName);
        return await entTable.QueryAsync<T>(maxPerPage: 1).FirstOrDefaultAsync();
    }

    public async Task<T?> GetEntity<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
    {
        TableClient entTable = _tableServiceClient.GetTableClient(tableName);
        return await entTable.QueryAsync<T>(maxPerPage: 1, filter: e => e.RowKey == rowKey && e.PartitionKey == partitionKey).FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<T>? values, string continurationToken)> GetPaginatedEntities<T>
        (string tableName, string query, int maxPerPage, string? continuationToken) where T : class, ITableEntity, new()
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        int pageCount = (maxPerPage == 0) ? 10 : maxPerPage;
        var queryResultsFilter = tableClient.QueryAsync<T>(
            filter: query
            );

        await foreach (var page in queryResultsFilter.AsPages(continuationToken, maxPerPage))
        {
            return (page.Values, page.ContinuationToken ?? string.Empty);
        }

        return (null, string.Empty);
    }

    public async Task UpsertEntity<T>(T record, string tableName) where T : ITableEntity
    {
        TableClient jobTable = _tableServiceClient.GetTableClient(tableName);
        await jobTable.UpsertEntityAsync<T>(record);
    }

    public async Task<T?> GetEntity<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
    {
        TableClient entTable = _tableServiceClient.GetTableClient(tableName);
        return await entTable.QueryAsync<T>(maxPerPage: 1, filter: e => e.PartitionKey == partitionKey).FirstOrDefaultAsync();
    }

    public async Task<List<T>?> GetEntities<T>(string tableName, string query) where T : class, ITableEntity, new()
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        var queryResultsFilter = tableClient.QueryAsync<T>(
            filter: query
            );

        return await queryResultsFilter.ToListAsync();
    }
}
```

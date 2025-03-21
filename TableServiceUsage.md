# Sample usage of TableService class

```csharp
public BrandFeatureConfigurationRepository(
    ITableService tableService,
    IMapper mapper,
    ILogger<BrandFeatureConfigurationRepository> logger)
{
    _tableService = tableService;
    _mapper = mapper;
    _logger = logger;
}

    public async Task<List<BrandAccessControl>> GetAllRecords()
    {
        var entities = await _tableService.GetEntities<BrandAccessControlEntity>(_tableName, "");
        return _mapper.Map<List<BrandAccessControl>>(entities);
    }

    var query = $"RowKey eq '{brandName}'";
    var existingRecords = await _tableService.GetEntities<BrandAccessControlEntity>(_tableName, query);

    await _tableService.UpsertEntity(brandAccessControlEntity, _tableName);


    public async Task<(List<StoreActivationState> states, string continuationToken)> FilterAndGetPaginatedRecords
        (PaginatedActivatedStoresQueriesDto queryDto)
    {
        string query = string.Empty;

        if (queryDto.StoreNumber is not null)
        {
            if (!string.IsNullOrEmpty(query)) query += " and ";
            query += $"{nameof(StoreActivationStateEntity.StoreNumber)} eq '{queryDto.StoreNumber}'";
        }

        if (queryDto.StoreName is not null)
        {
            if (!string.IsNullOrEmpty(query)) query += " and ";
            query += $"{nameof(StoreActivationStateEntity.StoreName)} eq '{queryDto.StoreName.Replace("'", "''")}'";
        }

        if (queryDto.StoreBrand is not null)
        {
            if (!string.IsNullOrEmpty(query)) query += " and ";
            query += $"{nameof(StoreActivationStateEntity.StoreBrand)}  eq '{queryDto.StoreBrand}'";
        }

        if (queryDto.StoreStateCode is not null)
        {
            if (!string.IsNullOrEmpty(query)) query += " and ";
            query += $"{nameof(StoreActivationStateEntity.StoreStateCode)} eq '{queryDto.StoreStateCode}'";
        }

        var (values, newContinuationToken) = await _tableService
                .GetPaginatedEntities<StoreActivationStateEntity>(_tableName, query, queryDto.PageSize, queryDto.ContinuationToken);
        var storeStates = _mapper.Map<List<StoreActivationState>>(values);
        return (storeStates, newContinuationToken);
    }
```

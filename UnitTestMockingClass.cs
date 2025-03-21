using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Moq;
...
using Azure;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
...
  
namespace myProject.project_1.tests;
public static class FeatureActivationServiceProvider
{
    public static IServiceProvider GetNewServiceProvider
        (List<ITableEntity> existingTableEntities,
        List<ProjectFacility>? mockProjectFacilities = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAutoMapper(new Type[]
        {
            typeof(FeatureActivationModelsMappingProfile),
            typeof(FeatureActivationMappingProfile)
        });

        services.RegisterFeatureActivationMockTableServiceClient(existingTableEntities);
        services.AddFeatureActivationImplementations();
        services.AddFeatureActivationServices();

        if (mockProjectFacilities != null)
        {
            services.AddScoped(provider => GetMockMyProjectproject_1ActivationClient(mockProjectFacilities));
        }

        services.AddScoped<ITableService, TableService>();

        return services.BuildServiceProvider();
    }

    public static IServiceCollection RegisterFeatureActivationMockTableServiceClient
        (this IServiceCollection services, List<ITableEntity> existingTableEntities)
    {
        services.AddSingleton(provider =>
        {
            var mockTableServiceClient = new Mock<TableServiceClient>();

            var validStores = new List<ValidStoreEntity>();
            var brandLevelActivationRequests = new List<BrandLevelActivationRequestEntity>();
            var brandAccessControls = new List<BrandAccessControlEntity>();
            var contractTypeLevelActivationRequests = new List<ContractTypeLevelActivationRequestEntity>();
            var contractTypeAccessControls = new List<ContractTypeAccessControlEntity>();
            var storeActivationStates = new List<StoreActivationStateEntity>();
            var storeActivationRequests = new List<StoreActivationRequestEntity>();
            var storeAccessControls = new List<StoreAccessControlEntity>();
            var featureActivationRequests = new List<FeatureActivationRequestEntity>();

            foreach (var entity in existingTableEntities)
            {
                switch (entity)
                {
                    case ValidStoreEntity:
                        validStores.Add((ValidStoreEntity)entity);
                        break;
                    case BrandLevelActivationRequestEntity:
                        brandLevelActivationRequests.Add((BrandLevelActivationRequestEntity)entity);
                        break;
                    case BrandAccessControlEntity:
                        brandAccessControls.Add((BrandAccessControlEntity)entity);
                        break;
                    case ContractTypeLevelActivationRequestEntity:
                        contractTypeLevelActivationRequests.Add((ContractTypeLevelActivationRequestEntity)entity);
                        break;
                    case ContractTypeAccessControlEntity:
                        contractTypeAccessControls.Add((ContractTypeAccessControlEntity)entity);
                        break;
                    case StoreActivationStateEntity:
                        storeActivationStates.Add((StoreActivationStateEntity)entity);
                        break;
                    case StoreActivationRequestEntity:
                        storeActivationRequests.Add((StoreActivationRequestEntity)entity);
                        break;
                    case StoreAccessControlEntity:
                        storeAccessControls.Add((StoreAccessControlEntity)entity);
                        break;
                    case FeatureActivationRequestEntity:
                        featureActivationRequests.Add((FeatureActivationRequestEntity)entity);
                        break;
                    default:
                        break;
                }
            }

            // ValidStores table client setup
            RegisterTableClientForEntity(validStores,
                StoreActivationConstants.TableStorage.ValidStores,
                mockTableServiceClient);

            // BrandLevelActivationRequests Table client setup
            RegisterTableClientForEntity(brandLevelActivationRequests,
                StoreActivationConstants.TableStorage.BrandLevelActivationRequests,
                mockTableServiceClient);

            // BrandAccessControl table client setup
            RegisterTableClientForEntity(brandAccessControls, 
                Constants.Storage.Tables.AccessControls.Brand,
                mockTableServiceClient);

            // ContractTypeLevelActivationRequests table client setup
            RegisterTableClientForEntity(contractTypeLevelActivationRequests,
                StoreActivationConstants.TableStorage.ContractTypeLevelActivationRequests, mockTableServiceClient);

            // ContractTypeAccessControl table client setup
            RegisterTableClientForEntity(contractTypeAccessControls,
                Constants.Storage.Tables.AccessControls.ContractType,
                mockTableServiceClient);

            // StoreActivationState table client setup
            RegisterTableClientForEntity(storeActivationStates,
                StoreActivationConstants.TableStorage.StoreActivationState, 
                mockTableServiceClient);

            //StoreActivationRequests
            RegisterTableClientForEntity(
                storeActivationRequests,
                StoreActivationConstants.TableStorage.StoreActivationRequests,
                mockTableServiceClient);

            // StoreAccessControl table client setup
            RegisterTableClientForEntity(storeAccessControls,
                Constants.Storage.Tables.AccessControls.Store,
                mockTableServiceClient);

            //FeatureActivationRequests table client setup
            RegisterTableClientForEntity(featureActivationRequests,
                StoreActivationConstants.TableStorage.FeatureActivationRequests,
                mockTableServiceClient);

            return mockTableServiceClient.Object;
        });

        return services;
    }

    // TODO: Extract this into a separate class to enable reuse across multiple unit tests
    private static void RegisterTableClientForEntity<tableEntity>(
        List<tableEntity> inMemoryTable, string tableName, Mock<TableServiceClient>? mockTableServiceClient) where tableEntity : class, ITableEntity, new ()
    {
        var mockTableClient = new Mock<TableClient>();
        mockTableClient.Setup(m => m.Name).Returns(tableName);

        mockTableClient
            .Setup(m => m.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), default, default))
            .Callback((string partitionKey, string rowKey, ETag _, CancellationToken _) =>
            {
                inMemoryTable = inMemoryTable.Where(x => !(x.PartitionKey == partitionKey && x.RowKey == rowKey)).ToList();
            });

        mockTableClient
            .Setup(m => m.QueryAsync<tableEntity>(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns((string filter, int? maxPerPage, IEnumerable<string> select, CancellationToken _) =>
            {
                // Since Table Storage queries use OData filter syntax
                // This mock method uses the System.Linq.Dynamic.Core package
                // to interpret the Azure Table Storage query string into LINQ expressions
                // the '.Where' method is coming from that package

                // Convert OData-style query syntax to Dynamic LINQ syntax
                string linqFilter = filter
                .Replace(" eq ", " == ")
                .Replace(" ne ", "!=")
                .Replace(" gt ", ">")
                .Replace(" ge ", ">=")
                .Replace(" lt ", "<")
                .Replace(" le ", "<=")
                .Replace("'", "\"")
                .Replace("guid", string.Empty);

                var filteredData = inMemoryTable.AsQueryable();

                if (!string.IsNullOrEmpty(filter))
                {
                    filteredData = filteredData.Where(linqFilter);
                }
                return new FakeAsyncPageable<tableEntity>(filteredData);
            });

        mockTableClient
            .Setup(m => m.QueryAsync(
                It.IsAny<Expression<Func<tableEntity, bool>>>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()
            ))
            .Callback((Expression<Func<tableEntity, bool>> filter, int? maxPerPage, IEnumerable<string> select, CancellationToken _) =>
            {
                Console.WriteLine($"Mock QueryAsync invoked with filter: {filter}");
            })
            .Returns((Expression<Func<tableEntity, bool>> filter, int? maxPerPage, IEnumerable<string> select, CancellationToken _) =>
            {
                var filteredData = inMemoryTable.AsQueryable().Where(filter).ToList();
                return new FakeAsyncPageable<tableEntity>(filteredData);
            });

        mockTableClient
            .Setup(m => m.UpsertEntityAsync(It.IsAny<tableEntity>(), TableUpdateMode.Merge, default))
            .Callback((tableEntity entity, TableUpdateMode mode, CancellationToken _) =>
            {
                inMemoryTable = inMemoryTable.Where(x => !(x.PartitionKey == entity.PartitionKey && x.RowKey == entity.RowKey)).ToList();
                inMemoryTable.Add(entity);
            });

        mockTableClient
            .Setup(m => m.AddEntityAsync(It.IsAny<tableEntity>(), default))
            .Callback((tableEntity entity, CancellationToken _) =>
            {
                inMemoryTable.Add(entity);
            })
            .Returns(Task.FromResult(new Mock<Response>().Object));

        mockTableServiceClient
            .Setup(m => m.GetTableClient(tableName))
            .Returns(mockTableClient.Object);
    }

    private static IMyProjectproject_1ActivationClient GetMockMyProjectproject_1ActivationClient(List<ProjectFacility> mockProjectFacilities)
    {
        var mock = new Mock<IMyProjectproject_1ActivationClient>();

        mock.Setup(m => m.GetStoreSecurityGroup(It.IsAny<string>()))
            .Returns<string>(securityGroupName => Task.FromResult(new MicrosoftSecurityGroup
            {
                ObjectId = "somerandomstring",
                DisplayName = securityGroupName,
                HasAtLeastOneMember = true,
            }));

        mock.Setup(m => m.GetAllProjectFacilities()).Returns(Task.FromResult(mockProjectFacilities));
        mock.Setup(m => m.GetProjectFacilities(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((storeNumber, stateCode) =>
                Task.FromResult(mockProjectFacilities.Where(x => x.FacilityNumber == storeNumber && x.State == stateCode).ToList()));

        mock.Setup(m => m.UpsertTMSSPilotStore(It.IsAny<TMSSPilotStore>()))
            .Returns<TMSSPilotStore>((record) => Task.FromResult(record));

        mock.Setup(m => m.AssignSecurityGroupToSharePointGroup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(true));

        mock.Setup(m => m.UnAssignSecurityGroupFromSharePointGroup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(true));

        return mock.Object;
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Cosmos.Database.Setup.Abstract;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Random;

namespace Soenneker.Cosmos.Database.Setup;

///<inheritdoc cref="ICosmosDatabaseSetupUtil"/>
public class CosmosDatabaseSetupUtil : ICosmosDatabaseSetupUtil
{
    private readonly ILogger<CosmosDatabaseSetupUtil> _logger;
    private readonly IConfiguration _config;
    private readonly ICosmosClientUtil _clientUtil;

    public CosmosDatabaseSetupUtil(IConfiguration config, ILogger<CosmosDatabaseSetupUtil> logger, ICosmosClientUtil clientUtil)
    {
        _config = config;
        _logger = logger;
        _clientUtil = clientUtil;
    }

    public async ValueTask<Microsoft.Azure.Cosmos.Database> EnsureDatabase()
    {
        var databaseName = _config.GetValueStrict<string>("Azure:Cosmos:DatabaseName");

        Microsoft.Azure.Cosmos.Database database = await EnsureDatabase(databaseName).NoSync();

        return database;
    }

    public async ValueTask<Microsoft.Azure.Cosmos.Database> EnsureDatabase(string name)
    {
        _logger.LogDebug("Ensuring Cosmos database {database} exists ... if not, creating", name);

        DatabaseResponse? databaseResponse = null;

        CosmosClient client = await _clientUtil.GetClient().NoSync();

        try
        {
            AsyncRetryPolicy? retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // exponential back-off: 2, 4, 8 etc, with jitter
                                                      + TimeSpan.FromMilliseconds(RandomUtil.Next(0, 1000)),
                    async (exception, timespan, retryCount) =>
                    {
                        _logger.LogError(exception, "*** CosmosSetupUtil *** Failed to EnsureDatabase, trying again in {delay}s ... count: {retryCount}", timespan.Seconds, retryCount);
                        await ValueTask.CompletedTask;
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                databaseResponse = await client.CreateDatabaseIfNotExistsAsync(name, GetDatabaseThroughput()).NoSync();
                _logger.LogDebug("Ensured database {database}", name);
            });
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "*** CosmosSetupUtil *** Stopped retrying database creation: {database}, aborting!", name);
            throw;
        }

        Microsoft.Azure.Cosmos.Database database = databaseResponse!.Database;

        if (database == null)
            throw new Exception($"Failed to create Cosmos database {name} diagnostics: {databaseResponse.Diagnostics}");

        await SetDatabaseThroughput(database);

        return databaseResponse.Database;
    }

    private async ValueTask SetDatabaseThroughput(Microsoft.Azure.Cosmos.Database database)
    {
        _logger.LogInformation("Setting database throughput...");
        await database.ReplaceThroughputAsync(GetDatabaseThroughput()).NoSync();

        _logger.LogDebug("Finished setting database throughput");
    }

    private ThroughputProperties GetDatabaseThroughput()
    {
        var throughput = _config.GetValueStrict<int>("Azure:Cosmos:DatabaseThroughput");
        
        var properties = ThroughputProperties.CreateAutoscaleThroughput(throughput);

        _logger.LogDebug("Retrieved the Cosmos DB AutoScale throughput of {throughput} RU", throughput);

        return properties;
    }
}
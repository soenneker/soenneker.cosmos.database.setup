using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Cosmos.Database.Setup.Abstract;
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
        var databaseName = _config.GetValue<string>("Azure:Cosmos:DatabaseName");

        if (databaseName == null)
            throw new Exception("Azure:Cosmos:DatabaseName is required");

        Microsoft.Azure.Cosmos.Database database = await EnsureDatabase(databaseName);

        return database;
    }

    public async ValueTask<Microsoft.Azure.Cosmos.Database> EnsureDatabase(string name)
    {
        _logger.LogDebug("Ensuring Cosmos database {database} exists ... if not, creating", name);

        DatabaseResponse? databaseResponse = null;

        CosmosClient client = await _clientUtil.GetClient();

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
                databaseResponse = await client.CreateDatabaseIfNotExistsAsync(name, GetDatabaseThroughput());
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
        await database.ReplaceThroughputAsync(GetDatabaseThroughput());

        _logger.LogDebug("Finished setting database throughput");
    }

    private ThroughputProperties GetDatabaseThroughput()
    {
        // TODO: Look at throughput and do further testing with AzOps

        var environment = _config.GetValue<string>("Environment:Name");

        if (environment == null)
            throw new Exception("Environment:Name is required");

        var throughput = _config.GetValue<int?>("Azure:Cosmos:DatabaseThroughput");

        if (throughput == null)
            throw new Exception("Azure:Cosmos:DatabaseThroughput is required");

        var properties = ThroughputProperties.CreateAutoscaleThroughput(throughput.Value);

        _logger.LogDebug("Retrieved the Cosmos DB AutoScale throughput of {throughput} RU", throughput);

        return properties;
    }
}
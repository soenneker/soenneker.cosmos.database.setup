using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Cosmos.Database.Setup.Abstract;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.String;
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

    public async ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(CancellationToken cancellationToken = default)
    {
        var databaseName = _config.GetValueStrict<string>("Azure:Cosmos:DatabaseName");

        return await Ensure(databaseName, cancellationToken).NoSync();
    }

    public async ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Ensuring Cosmos database ({name}) exists ... if not, creating", name);

        DatabaseResponse? databaseResponse = null;

        CosmosClient client = await _clientUtil.Get(cancellationToken).NoSync();

        try
        {
            AsyncRetryPolicy? retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // exponential back-off: 2, 4, 8 etc, with jitter
                                                      + TimeSpan.FromMilliseconds(RandomUtil.Next(0, 1000)),
                    async (exception, timespan, retryCount) =>
                    {
                        _logger.LogError(exception, "*** CosmosDatabaseSetupUtil *** Failed to ensure database ({name}), trying again in {delay}s ... count: {retryCount}", name, timespan.Seconds,
                            retryCount);
                        await ValueTask.CompletedTask;
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                databaseResponse = await client.CreateDatabaseIfNotExistsAsync(name, GetDatabaseThroughput(), cancellationToken: cancellationToken).NoSync();
                _logger.LogDebug("Ensured Cosmos database ({name})", name);
            }).NoSync();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "*** CosmosDatabaseSetupUtil *** Stopped retrying database creation: {database}, aborting!", name);
            throw;
        }

        Microsoft.Azure.Cosmos.Database database = databaseResponse!.Database;

        if (database == null)
            throw new Exception($"Failed to create Cosmos database ({name}) diagnostics: {databaseResponse.Diagnostics}");

        await SetDatabaseThroughput(database).NoSync();

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
        var throughputType = _config.GetValueStrict<string>("Azure:Cosmos:DatabaseThroughputType");

        ThroughputProperties properties;

        if (throughputType.EqualsIgnoreCase("autoscale"))
            properties = ThroughputProperties.CreateAutoscaleThroughput(throughput);
        else
            properties = ThroughputProperties.CreateManualThroughput(throughput);

        _logger.LogDebug("Retrieved the Cosmos DB AutoScale throughput ({throughput} RU) with type ({throughputType})", throughput, throughputType);

        return properties;
    }
}
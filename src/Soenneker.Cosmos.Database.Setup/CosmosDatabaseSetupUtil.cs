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

/// <inheritdoc cref="ICosmosDatabaseSetupUtil"/>
public sealed class CosmosDatabaseSetupUtil : ICosmosDatabaseSetupUtil
{
    private readonly ILogger<CosmosDatabaseSetupUtil> _logger;
    private readonly IConfiguration _config;
    private readonly ICosmosClientUtil _clientUtil;
    private readonly AsyncRetryPolicy _retryPolicy;

    public CosmosDatabaseSetupUtil(IConfiguration config, ILogger<CosmosDatabaseSetupUtil> logger, ICosmosClientUtil clientUtil)
    {
        _config = config;
        _logger = logger;
        _clientUtil = clientUtil;

        _retryPolicy = Policy.Handle<Exception>(static ex => ex is not OperationCanceledException)
                             .WaitAndRetryAsync(5,
                                 static retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                     + TimeSpan.FromMilliseconds(RandomUtil.Next(0, 1000)),
                                 (exception, timespan, retryCount, context) =>
                                 {
                                     _logger.LogError(exception,
                                         "*** CosmosDatabaseSetupUtil *** Failed to ensure database ({databaseName}), trying again in {delay}s ... count: {retryCount}",
                                         context["databaseName"], timespan.TotalSeconds, retryCount);
                                     return Task.CompletedTask;
                                 });
    }

    public ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(CancellationToken cancellationToken = default)
    {
        var databaseName = _config.GetValueStrict<string>("Azure:Cosmos:DatabaseName");
        var endpoint = _config.GetValueStrict<string>("Azure:Cosmos:Endpoint");
        var accountKey = _config.GetValueStrict<string>("Azure:Cosmos:AccountKey");

        return Ensure(endpoint, accountKey, databaseName, cancellationToken);
    }

    public async ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(string endpoint, string accountKey, string databaseName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Ensuring Cosmos database ({databaseName}) exists ... if not, creating", databaseName);

        DatabaseResponse? databaseResponse = null;

        CosmosClient client = await _clientUtil.Get(endpoint, accountKey, cancellationToken).NoSync();
        ThroughputProperties databaseThroughput = GetDatabaseThroughput();

        try
        {
            var context = new Context {["databaseName"] = databaseName};
            await _retryPolicy.ExecuteAsync(async _ =>
                             {
                                 databaseResponse = await client
                                                          .CreateDatabaseIfNotExistsAsync(databaseName, databaseThroughput,
                                                              cancellationToken: CancellationToken.None)
                                                          .NoSync();
                                 _logger.LogDebug("Ensured Cosmos database ({databaseName})", databaseName);
                             }, context)
                             .NoSync();

            Microsoft.Azure.Cosmos.Database database = databaseResponse!.Database;

            if (database == null)
                throw new Exception($"Failed to create Cosmos database ({databaseName}) diagnostics: {databaseResponse.Diagnostics}");

            await SetDatabaseThroughput(database, databaseThroughput, CancellationToken.None).NoSync();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "*** CosmosDatabaseSetupUtil *** Stopped retrying database creation: {database}, aborting!", databaseName);
            throw;
        }

        return databaseResponse.Database;
    }

    private async ValueTask SetDatabaseThroughput(Microsoft.Azure.Cosmos.Database database, ThroughputProperties databaseThroughput,
        CancellationToken cancellationToken)
    {
        var replaceDatabaseThroughput = _config.GetValue<bool>("Azure:Cosmos:ReplaceDatabaseThroughput");

        if (replaceDatabaseThroughput)
        {
            _logger.LogInformation("Setting database throughput...");

            await database.ReplaceThroughputAsync(databaseThroughput, cancellationToken: cancellationToken).NoSync();

            _logger.LogDebug("Finished setting database throughput");
        }
    }

    private ThroughputProperties GetDatabaseThroughput()
    {
        var throughput = _config.GetValueStrict<int>("Azure:Cosmos:DatabaseThroughput");
        var throughputType = _config.GetValueStrict<string>("Azure:Cosmos:DatabaseThroughputType");

        ThroughputProperties properties = throughputType.EqualsIgnoreCase("autoscale")
            ? ThroughputProperties.CreateAutoscaleThroughput(throughput)
            : ThroughputProperties.CreateManualThroughput(throughput);

        _logger.LogDebug("Using Cosmos DB throughput ({throughput} RU - {throughputType})...", throughput, throughputType);
        return properties;
    }

}

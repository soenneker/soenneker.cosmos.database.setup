using System;
using System.Net;
using System.Net.Http;
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

        _retryPolicy = Policy.Handle<CosmosException>(static exception =>
                                 exception.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
                                     HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable || (int)exception.StatusCode == 449)
                             .Or<HttpRequestException>()
                             .Or<TimeoutException>()
                             .WaitAndRetryAsync(5,
                                 static retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                     + TimeSpan.FromMilliseconds(RandomUtil.Next(0, 1000)),
                                 (exception, timespan, retryCount, context) =>
                                 {
                                     _logger.LogWarning(exception,
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

        var context = new Context {["databaseName"] = databaseName};
        await _retryPolicy.ExecuteAsync(async (_, token) =>
                         {
                             databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName, databaseThroughput, cancellationToken: token)
                                                            .NoSync();
                             _logger.LogDebug("Ensured Cosmos database ({databaseName})", databaseName);
                         }, context, cancellationToken)
                         .NoSync();

        DatabaseResponse ensuredResponse = databaseResponse ??
                                            throw new InvalidOperationException($"Cosmos did not return a response while ensuring database '{databaseName}'.");
        Microsoft.Azure.Cosmos.Database database = ensuredResponse.Database;

        await SetDatabaseThroughput(database, databaseThroughput, cancellationToken).NoSync();

        return database;
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

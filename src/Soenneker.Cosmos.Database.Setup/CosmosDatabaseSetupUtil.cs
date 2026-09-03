using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Kevlar;
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
    private readonly Shield _retryShield;

    public CosmosDatabaseSetupUtil(IConfiguration config, ILogger<CosmosDatabaseSetupUtil> logger, ICosmosClientUtil clientUtil)
    {
        _config = config;
        _logger = logger;
        _clientUtil = clientUtil;

        _retryShield = Shield.When<CosmosException>(static exception =>
                                  exception.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
                                      HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable || (int) exception.StatusCode == 449)
                              .Or<HttpRequestException>()
                              .Or<TimeoutException>()
                              .Retry(options =>
                              {
                                  options.MaxRetries = 5;
                                  options.Backoff = Backoff.Custom(static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
                                      + TimeSpan.FromMilliseconds(RandomUtil.Next(0, 1000)));
                                  options.OnRetry = retry =>
                                  {
                                      _logger.LogWarning(retry.Exception,
                                          "*** CosmosDatabaseSetupUtil *** Failed to ensure database ({databaseName}), trying again in {delay}s ... count: {retryCount}",
                                          retry.Context.Properties.GetOrDefault(KevlarKeys.OperationKey, string.Empty), retry.Delay.TotalSeconds,
                                          retry.AttemptNumber + 1);
                                      return default;
                                  };
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

        await _retryShield.ExecuteWithContextAsync(databaseName,
                         static (name, properties) => properties.Set(KevlarKeys.OperationKey, name),
                         async (_, context) =>
                         {
                             databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName, databaseThroughput,
                                                         cancellationToken: context.CancellationToken)
                                                            .NoSync();
                             _logger.LogDebug("Ensured Cosmos database ({databaseName})", databaseName);
                         }, cancellationToken)
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

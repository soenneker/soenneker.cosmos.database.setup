using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Cosmos.Database.Setup.Abstract;

/// <summary>
/// Creates an Azure Cosmos DB database when it does not already exist and optionally replaces its throughput.
/// </summary>
public interface ICosmosDatabaseSetupUtil
{
    /// <summary>
    /// Ensures the database configured under <c>Azure:Cosmos</c> exists.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested microsoft.Azure.Cosmos.Database.</returns>
    ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the specified database exists.
    /// </summary>
    /// <param name="endpoint">Service endpoint to call.</param>
    /// <param name="accountKey">Account key used for authentication.</param>
    /// <param name="databaseName">Name of the target database.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested microsoft.Azure.Cosmos.Database.</returns>
    ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(string endpoint, string accountKey, string databaseName, CancellationToken cancellationToken = default);
}

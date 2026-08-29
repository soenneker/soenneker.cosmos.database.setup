using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Cosmos.Database.Setup.Abstract;

/// <summary>
/// A utility library for Azure Cosmos database setup operations
/// Singleton IoC
/// </summary>
public interface ICosmosDatabaseSetupUtil
{
    /// <summary>
    /// Ensure the database is created
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested microsoft.Azure.Cosmos.Database.</returns>
    ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure the database is created
    /// </summary>
    /// <param name="endpoint">Service endpoint to call.</param>
    /// <param name="accountKey">Account key used for authentication.</param>
    /// <param name="databaseName">Name of the target database.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested microsoft.Azure.Cosmos.Database.</returns>
    ValueTask<Microsoft.Azure.Cosmos.Database> Ensure(string endpoint, string accountKey, string databaseName, CancellationToken cancellationToken = default);
}

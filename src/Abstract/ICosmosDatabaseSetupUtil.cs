using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Soenneker.Cosmos.Database.Setup.Abstract;

/// <summary>
/// A utility library for Azure Cosmos database setup operations
/// Singleton
/// </summary>
public interface ICosmosDatabaseSetupUtil
{
    /// <summary>
    /// Ensure the database is created
    /// </summary>
    ValueTask EnsureDatabase(CosmosClient client);
}
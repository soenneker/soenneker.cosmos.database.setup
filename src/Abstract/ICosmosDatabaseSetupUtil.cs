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
    ValueTask<Microsoft.Azure.Cosmos.Database> EnsureDatabase();

    ValueTask<Microsoft.Azure.Cosmos.Database> EnsureDatabase(string name);
}
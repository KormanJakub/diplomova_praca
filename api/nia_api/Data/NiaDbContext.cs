using Microsoft.Extensions.Options;
using MongoDB.Driver;
using nia_api.Models;

namespace nia_api.Data
{
    public class NiaDbContext
    {
        private readonly IMongoDatabase _database;

        public NiaDbContext(IOptions<NiaDbSettings> configuration)
        {
            var settings = configuration.Value;
            if (string.IsNullOrEmpty(settings.ConnectionString) || string.IsNullOrEmpty(settings.DatabaseName))
            {
                throw new ArgumentNullException(nameof(settings), "ConnectionString or DatabaseName cannot be null or empty.");
            }

            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}

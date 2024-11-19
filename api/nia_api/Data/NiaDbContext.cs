using Microsoft.Extensions.Options;
using MongoDB.Driver;
using nia_api.Models;
using Tag = nia_api.Models.Tag;

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
        public IMongoCollection<GuestUser> GuestUsers => _database.GetCollection<GuestUser>("GuestUsers");
        public IMongoCollection<Design> Designs => _database.GetCollection<Design>("Designs");
        public IMongoCollection<Tag> Tags => _database.GetCollection<Tag>("Tags");
        public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
        public IMongoCollection<PairedDesign> PairedDesigns => _database.GetCollection<PairedDesign>("PairedDesigns");

        public IMongoCollection<Customization> Customizations =>
            _database.GetCollection<Customization>("Customizations");
        
        public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");
    }
}

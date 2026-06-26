using Microsoft.Extensions.Configuration;
using HireIQ.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace HireIQ.Infrastructure.Persistence
{
    public class MongoDbService
    {
        private readonly IMongoCollection<BsonDocument> _resumeCollection;

        public MongoDbService(IConfiguration config)
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            var connectionString = config["MongoDbSettings:ConnectionString"];

            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // ✅ SSL fix
            settings.SslSettings = new SslSettings
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
            settings.ConnectTimeout = TimeSpan.FromSeconds(15);

            var client = new MongoClient(settings);
            var database = client.GetDatabase(config["MongoDbSettings:DatabaseName"]);
            _resumeCollection = database.GetCollection<BsonDocument>(
                config["MongoDbSettings:CollectionName"]);
        }

        public async Task<BsonDocument> GetResumeByUserIdAsync(Guid userId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("userId", userId);
            return await _resumeCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task SaveResumeAsync(BsonDocument resumeData)
        {
            var userId = resumeData["userId"].AsGuid;
            var filter = Builders<BsonDocument>.Filter.Eq("userId", userId);
            await _resumeCollection.ReplaceOneAsync(
                filter, resumeData, new ReplaceOptions { IsUpsert = true });
        }
    }
}

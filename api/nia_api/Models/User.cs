using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Services;

namespace nia_api.Models
{
    public class User
    {
        [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
        [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        [BsonElement("email"), BsonRepresentation(BsonType.String)]
        public string? Email { get; set; }
        [BsonElement("email_confirmed"), BsonRepresentation(BsonType.Boolean)]
        public bool IsEmailConfirmed { get; set; }
        [BsonElement("password"), BsonRepresentation(BsonType.String)]
        public string? Password { get; set; }
        [BsonElement("first_name"), BsonRepresentation(BsonType.String)]
        public string? FirstName { get; set; }
        [BsonElement("last_name"), BsonRepresentation(BsonType.String)]
        public string? LastName { get; set; }
        [BsonElement("country"), BsonRepresentation(BsonType.String)]
        public string? Country { get; set; }
        [BsonElement("phone_number"), BsonRepresentation(BsonType.String)]
        public string? PhoneNumber { get; set; }
        [BsonElement("address"), BsonRepresentation(BsonType.String)]
        public string? Address { get; set; }
        [BsonElement("zip"), BsonRepresentation(BsonType.String)]
        public string? Zip {  get; set; }
        [BsonElement("admin"), BsonRepresentation(BsonType.Boolean)]
        public bool IsAdmin {  get; set; }
        [BsonElement("verification_code"), BsonRepresentation(BsonType.Int32)]
        public int VerificationCode {  get; set; }
        [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
        public DateTime? CreatedAt { get; set; }
        [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
        public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
    }
}

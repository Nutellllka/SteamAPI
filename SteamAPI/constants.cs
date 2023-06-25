using MongoDB.Bson;
using MongoDB.Driver;

public class constants
{
    public static string botId = "6248442755:AAFkaGWSj4Bo7BfxM4RrSpcDPW8criSBRRI";
    public static string host = "steamapi20230625182605.azurewebsites.net";
    public static MongoClient mongoClient;
    public static IMongoDatabase database;
    public static IMongoCollection<BsonDocument> collection;
    //public static string apikey = "645ba868c66aa4fc529b4df70645d0b7";
}
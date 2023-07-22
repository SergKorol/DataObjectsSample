// See https://aka.ms/new-console-template for more information

using System.IO.Compression;
using System.Text;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace DataObjectsSample;

internal static class Program
{
    private static void Main()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string filePath = Path.Combine(currentDirectory, "generated.json");
        
        string json = File.ReadAllText(filePath);
        string bson = GetBson(json);
        string mongo = BsonArray(json);
        string yaml = ToYaml(json);
        
        byte[] jsonCompressed = CompressStringData(json);
        byte[] bsonCompressed = CompressStringData(bson);
        byte[] mongoCompressed = CompressStringData(mongo);
        byte[] yamlCompressed = CompressStringData(yaml);
        
        Console.WriteLine($"Original json size: {json.Length} Bytes");
        Console.WriteLine($"Original bson size: {bson.Length} Bytes");
        Console.WriteLine($"Original mongo size: {mongo.Length} Bytes");
        Console.WriteLine($"Original yaml size: {yaml.Length} Bytes");
        
        Console.WriteLine($"Compressed json size: {jsonCompressed.Length} Bytes");
        Console.WriteLine($"Compressed bson size: {bsonCompressed.Length} Bytes");
        Console.WriteLine($"Compressed mongo size: {mongoCompressed.Length} Bytes");
        Console.WriteLine($"Compressed yaml size: {yamlCompressed.Length} Bytes");
    }
    
    private static string GetBson(string json)
    {
        var jsonObj = JsonConvert.DeserializeObject(json);
        var ms = new MemoryStream();
        using (var writer = new BsonWriter(ms))
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(writer, jsonObj);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
    
    private static string BsonArray(string json)
    {
        using var jsonReader = new MongoDB.Bson.IO.JsonReader(json);
        var serializer = new MongoDB.Bson.Serialization.Serializers.BsonArraySerializer();
        var bsonArray = serializer.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));
        return bsonArray.ToString();
    }
    
    private static string ToYaml(string json)
    {
        var document = ConvertJTokenToObject(JsonConvert.DeserializeObject<JToken>(json)!);
        var serializer = new YamlDotNet.Serialization.Serializer();

        using var writer = new StringWriter();
        serializer.Serialize(writer, document);
        var yaml = writer.ToString();
        return yaml;

        static object? ConvertJTokenToObject(JToken token)
        {
            return token switch
            {
                JValue value => value.Value,
                JArray => token.AsEnumerable().Select(ConvertJTokenToObject).ToList(),
                JObject => token.AsEnumerable()
                    .Cast<JProperty>()
                    .ToDictionary(x => x.Name, x => ConvertJTokenToObject(x.Value)),
                _ => throw new InvalidOperationException("Unexpected token: " + token)
            };
        }
    }
    
    private static byte[] CompressStringData(string jsonData)
    {
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);

        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
        {
            gzipStream.Write(byteArray, 0, byteArray.Length);
        }
        return memoryStream.ToArray();
    }
}
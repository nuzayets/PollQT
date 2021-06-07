using System.Text.Json;

namespace PollQT
{
    public abstract class JsonSerializable<T>
    {
        private static readonly JsonSerializerOptions defaultJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static T FromJson(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(json, options);

        public static T FromJson(string json) => FromJson(json, defaultJsonSerializerOptions);

        public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, typeof(T), options);

        public string ToJson() => ToJson(defaultJsonSerializerOptions);

        public override int GetHashCode() => ToJson().GetHashCode();

        public override bool Equals(object? obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            var t = (JsonSerializable<T>)obj;
            return t.ToJson().Equals(ToJson());
        }
    }
}

using System.Text.Json;

namespace PollQT
{
    /// <summary>
    /// Provides automatic serialization and janky but functional equality comparison for types
    /// mostly consisting of properties that can easily be converted to and from JSON.
    /// </summary>
    /// <typeparam name="T">A type back parameter to the implementing class,
    /// e.g. <example>class SomeObject : JsonSerializable&lt;SomeObject&gt;</example>
    /// </typeparam>
    public abstract class JsonSerializable<T>
    {
        private static readonly JsonSerializerOptions defaultJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Deserializes into a new object or throws
        /// </summary>
        /// <param name="json">the JSON</param>
        /// <param name="options">the options</param>
        /// <returns></returns>
        public static T FromJson(string json, JsonSerializerOptions options) =>
            JsonSerializer.Deserialize<T>(json, options) ?? throw new JsonException($"Error parsing {json}");

        /// <summary>
        /// Deserializes into a new object or throws. Uses default web options - case insensitive and camel-case.
        /// </summary>
        /// <param name="json">the JSON</param>
        /// <returns></returns>
        public static T FromJson(string json) => FromJson(json, defaultJsonSerializerOptions);

        /// <summary>
        ///  Serializes to JSON. 
        /// </summary>
        /// <param name="options">the options</param>
        /// <returns></returns>
        public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, typeof(T), options);

        /// <summary>
        ///  Serializes to JSON. Uses default web options - case insensitive and camel-case.
        /// </summary>
        /// <returns></returns>
        public string ToJson() => ToJson(defaultJsonSerializerOptions);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Just hashes the JSON. Don't use for performance critical stuff.</returns>
        public override int GetHashCode() => ToJson().GetHashCode();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Serializes both sides and tests string equality! Lots of assumptions being made!</returns>
        public override bool Equals(object? obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            var t = (JsonSerializable<T>)obj;
            return t.ToJson().Equals(ToJson());
        }
    }
}

using System.Text;
using System.Text.Json;
namespace PollQT.Questrade.Responses
{
    /// <summary>
    /// OAuth Token
    /// </summary>
    public class Token : JsonSerializable<Token>
    {
        /// <summary>
        /// OAuth Access Token
        /// </summary>
        public string? AccessToken { get; set; }
        /// <summary>
        /// OAuth Refresh Token
        /// </summary>
        public string? RefreshToken { get; set; }
        /// <summary>
        /// The API server the token-granter pointed us towards
        /// </summary>
        public string? ApiServer { get; set; }
        private static readonly JsonSerializerOptions jsonSerializerOptions =
            new() { PropertyNamingPolicy = new SnakeCaseNamingPolicy() };
        /// <summary>
        /// Serialize to JSON but use non default web options - snake_case property names.
        /// </summary>
        /// <returns>JSON token with snake_case props</returns>
        public new string ToJson() => ToJson(jsonSerializerOptions);
        /// <summary>
        /// Deserializes from JSON given snake_case props
        /// </summary>
        /// <param name="json">the JSON</param>
        /// <returns>new Token</returns>
        public static new Token FromJson(string json) => Token.FromJson(json, jsonSerializerOptions);
    }
    internal class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) {
            var result = new StringBuilder();
            for (var i = 0; i < name.Length; i++) {
                var c = name[i];
                if (i == 0) {
                    result.Append(char.ToLower(c));
                } else {
                    if (char.IsUpper(c)) {
                        result.Append('_');
                        result.Append(char.ToLower(c));
                    } else {
                        result.Append(c);
                    }
                }
            }
            return result.ToString();
        }
    }
}
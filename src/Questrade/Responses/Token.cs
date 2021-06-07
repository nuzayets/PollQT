using System.Text;
using System.Text.Json;

namespace PollQT.Questrade.Responses
{
    public class Token : JsonSerializable<Token>
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ApiServer { get; set; }

        private static readonly JsonSerializerOptions jsonSerializerOptions =
            new JsonSerializerOptions { PropertyNamingPolicy = new SnakeCaseNamingPolicy() };

        public new string ToJson() => ToJson(jsonSerializerOptions);

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
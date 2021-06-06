using System;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PollQT.Questrade.Responses
{
    public class Token : JsonSerializable<Token>
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ApiServer { get; set; }

        private static readonly JsonSerializerOptions jsonSerializerOptions = 
            new JsonSerializerOptions { PropertyNamingPolicy = new SnakeCaseNamingPolicy() };

        public new string ToJson() => this.ToJson(jsonSerializerOptions);
        public new static Token FromJson(string json) => Token.FromJson(json, jsonSerializerOptions);

    }

    class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var result = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i == 0)
                {
                    result.Append(char.ToLower(c));
                }
                else
                {
                    if (char.IsUpper(c))
                    {
                        result.Append('_');
                        result.Append(char.ToLower(c));
                    }
                    else
                    {
                        result.Append(c);
                    }
                }
            }
            return result.ToString();
        }
    }
}
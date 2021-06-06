using Xunit;
using System;
using PollQT.Questrade.Responses;
using System.Text.RegularExpressions;

namespace PollQT.Questrade
{
    public class TokenTest
    {
        [Theory]
        [FileData("inputs/example-token.json")]
        public void DeserializesExample(string json)
        {
            var t = Token.FromJson(json);
            Assert.NotNull(t.AccessToken);
            Assert.NotNull(t.ApiServer);
            Assert.NotNull(t.RefreshToken);
        }

        [Theory]
        [FileData("inputs/bootstrap-token.json")]
        public void DeserializesBootstrap(string json)
        {
            var t = Token.FromJson(json);
            Assert.Null(t.AccessToken);
            Assert.Null(t.ApiServer);
            Assert.NotNull(t.RefreshToken);
        }

        [Theory]
        [FileData("inputs/example-token.json")]
        public void SerializeRoundTrip(string json)
        {
            var t = Token.FromJson(json);
            var s = t.ToJson();
            var t2 = Token.FromJson(s);
            Assert.Equal(t,t2);
        }

        [Theory]
        [FileData("inputs/example-token.json")]
        public void SerializesNonEmpty(string json)
        {
            var t = Token.FromJson(json);
            var s = Regex.Replace(t.ToJson(), @"\s+", "");
            Assert.NotEqual("{}", s);
        }
    }
}
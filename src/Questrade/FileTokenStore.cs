using PollQT.Questrade.Responses;
using Serilog;
using System.IO;

namespace PollQT.Questrade
{
    internal class FileTokenStore : ITokenStore
    {
        private readonly ILogger log;
        private readonly string filePath;

        public FileTokenStore(ILogger log, string filePath)
        {
            this.log = log;
            this.filePath = filePath;
        }

        public Token GetToken()
        {
            try
            {
                log.Information("Reading token: {filePath}", filePath);
                return Token.FromJson(File.ReadAllText(filePath));
            }
            catch (FileNotFoundException e)
            {
                log.Error("Could not find token: {filePath}", filePath);
                throw new FileNotFoundException("Cannot find token file.", e);
            }
        }

        public void WriteToken(Token t)
        {
            log.Information("Writing token: {filePath}", filePath);
            log.Debug("{@token}", t);
            File.WriteAllText(filePath, t.ToJson());
        }

        public override string ToString() => $"{GetType()}: {filePath}";
    }
}
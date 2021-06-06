using PollQT.Questrade.Responses;
using Serilog;
using System;
using System.IO;

namespace PollQT.Questrade
{
    class FileTokenStore : ITokenStore
    {
        private readonly ILogger log;
        private readonly string filePath;

        public FileTokenStore(ILogger log, string filePath) {
            this.log = log;
            this.filePath = filePath;
        }

        public Token GetToken()
        {
            try {
                log.Information("Reading token: {filePath}", filePath);
                return Token.FromJson(File.ReadAllText(this.filePath));
            } catch (FileNotFoundException e)
            {
                log.Error("Could not find token: {filePath}", filePath);
                throw new FileNotFoundException("Cannot find token file.", e);
            }
        }

        public void WriteToken(Token t)
        {
            log.Information("Writing token: {filePath}", filePath);
            log.Debug("{@token}", t);
            File.WriteAllText(this.filePath, t.ToJson());
        }

        public override string ToString()
        {
            return $"{this.GetType()}: {filePath}";
        }
    }
}
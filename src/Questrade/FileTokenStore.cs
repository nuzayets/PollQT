using System.IO;
using PollQT.Questrade.Responses;
using Serilog;

namespace PollQT.Questrade
{
    internal class FileTokenStore : ITokenStore
    {
        private readonly ILogger log;
        private readonly string filePath;

        public FileTokenStore(Context context) {
            log = context.Logger.ForContext<FileTokenStore>();
            filePath = Path.Combine(context.WorkDir, "token.json");
        }

        public Token GetToken() {
            try {
                log.Information("Reading token: {filePath}", filePath);
                return Token.FromJson(File.ReadAllText(filePath));
            } catch (FileNotFoundException e) {
                log.Fatal("Could not find token: {filePath}", filePath);
                throw new FileNotFoundException("Cannot find token file.", e);
            }
        }

        public void WriteToken(Token t) {
            log.Information("Writing token: {filePath}", filePath);
            log.Debug("{@token}", t);
            File.WriteAllText(filePath, t.ToJson());
        }

        public override string ToString() => $"{GetType()}: {filePath}";
    }
}
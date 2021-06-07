using PollQT.Questrade.Responses;

namespace PollQT.Questrade
{
    internal interface ITokenStore
    {
        public abstract Token GetToken();
        public abstract void WriteToken(Token t);
    }
}
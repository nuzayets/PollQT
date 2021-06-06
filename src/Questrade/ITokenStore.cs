using PollQT.Questrade.Responses;
using System;

namespace PollQT.Questrade
{
    interface ITokenStore {
        abstract public Token GetToken();
        abstract public void WriteToken(Token t);
    }
}
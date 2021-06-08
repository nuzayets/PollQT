using System.Collections.Generic;
using PollQT.DataTypes;
namespace PollQT.Questrade.Responses
{
    internal class AccountsResponse : JsonSerializable<AccountsResponse>
    {
        public List<Account> Accounts { get; set; } = new List<Account>();
        public int? UserID { get; set; }
    }
}

using PollQT.DataTypes;
using System.Collections.Generic;

namespace PollQT.Questrade.Responses
{
    public class AccountsResponse : JsonSerializable<AccountsResponse>
    {
        public List<Account> Accounts { get; set; } = new List<Account>();
        public int? UserID { get; set; }
    }
}

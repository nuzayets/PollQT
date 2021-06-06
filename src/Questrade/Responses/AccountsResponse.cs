using System;
using System.Collections.Generic;
using System.Text;

namespace PollQT.Questrade.Responses
{
    public class Account
    {
        public string Type { get; set; } = "";
        public string Number { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsPrimary { get; set; } = default;
        public bool IsBilling { get; set; } = default;
        public string ClientAccountType { get; set; } = "";
    }

    public class AccountsResponse : JsonSerializable<AccountsResponse>
    {
        public List<Account> Accounts { get; set; } = new List<Account>();
        public int? UserID { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace PollQT.DataTypes
{
    public class PollResult : JsonSerializable<PollResult>
    {
        public DateTime Timestamp { get; }
        public Account Account { get; }
        public AccountBalance Balance { get; }
        public List<AccountPosition> Positions { get; }


        public PollResult(Account account, AccountBalance balance, List<AccountPosition> positions) {
            Timestamp = DateTime.UtcNow;
            Account = account;
            Balance = balance;
            Positions = positions;
        }
    }
}

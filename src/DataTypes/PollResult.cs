using System;
using System.Collections.Generic;

namespace PollQT.DataTypes
{
    internal class PollResult : JsonSerializable<PollResult>
    {
        public DateTimeOffset Timestamp { get; }
        public Account Account { get; }
        public AccountBalance Balance { get; }
        public List<AccountPosition> Positions { get; }


        public PollResult(Account account, AccountBalance balance, List<AccountPosition> positions) {
            Timestamp = DateTimeOffset.UtcNow;
            Account = account;
            Balance = balance;
            Positions = positions;
        }
    }
}

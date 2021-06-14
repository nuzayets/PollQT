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
        public List<AccountActivity> Activities { get; }
        public PollResult(DateTimeOffset timestamp, Account account, AccountBalance balance, List<AccountPosition> positions, List<AccountActivity> activities) {
            Timestamp = timestamp;
            Account = account;
            Balance = balance;
            Positions = positions;
            Activities = activities;
        }
    }
}

using PollQT.DataTypes;
using System.Collections.Generic;

namespace PollQT.Questrade.Responses
{
    public class AccountPositionsResponse : JsonSerializable<AccountPositionsResponse>
    {
        public List<AccountPosition> Positions { get; set; } = new List<AccountPosition>();
    }
}

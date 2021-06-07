﻿using System.Collections.Generic;
using PollQT.DataTypes;

namespace PollQT.Questrade.Responses
{
    public class AccountPositionsResponse : JsonSerializable<AccountPositionsResponse>
    {
        public List<AccountPosition> Positions { get; set; } = new List<AccountPosition>();
    }
}

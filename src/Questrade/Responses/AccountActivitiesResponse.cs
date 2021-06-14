using System.Collections.Generic;
using PollQT.DataTypes;
namespace PollQT.Questrade.Responses
{
    internal class AccountActivitiesResponse : JsonSerializable<AccountActivitiesResponse>
    {
        public List<AccountActivity> Activities { get; set; } = new List<AccountActivity>();
    }
}

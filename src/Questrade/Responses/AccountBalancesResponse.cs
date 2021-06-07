using PollQT.DataTypes;
using System.Collections.Generic;

namespace PollQT.Questrade.Responses
{
    public class AccountBalancesResponse : JsonSerializable<AccountBalancesResponse>
    {
        public List<AccountBalance> PerCurrencyBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> CombinedBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> SodPerCurrencyBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> SodCombinedBalances { get; set; } = new List<AccountBalance>();

    }
}

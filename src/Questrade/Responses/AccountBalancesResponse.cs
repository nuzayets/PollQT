using System.Collections.Generic;
using PollQT.DataTypes;
namespace PollQT.Questrade.Responses
{
    internal class AccountBalancesResponse : JsonSerializable<AccountBalancesResponse>
    {
        public List<AccountBalance> PerCurrencyBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> CombinedBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> SodPerCurrencyBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> SodCombinedBalances { get; set; } = new List<AccountBalance>();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PollQT.Questrade.Responses
{
    public class AccountBalance
    {
        public string Currency { get; set; } = "";
        public double Cash { get; set; } = default;
        public double MarketValue { get; set; } = default;
        public double TotalEquity { get; set; } = default;
        public double BuyingPower { get; set; } = default;
        public double MaintenanceExcess { get; set; } = default;
        public bool IsRealTime { get; set; } = default;


    }

    public class AccountBalancesResponse : JsonSerializable<AccountBalancesResponse>
    {
        public List<AccountBalance> PerCurrencyBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> CombinedBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> SodPerCurrencyBalances { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> SodCombinedBalances { get; set; } = new List<AccountBalance>();

    }
}

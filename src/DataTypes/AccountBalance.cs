namespace PollQT.DataTypes
{


    internal class AccountBalance : JsonSerializable<AccountBalance>
    {


        public string Currency { get; set; } = "";


        public double Cash { get; set; } = default;


        public double MarketValue { get; set; } = default;


        public double TotalEquity { get; set; } = default;


        public double BuyingPower { get; set; } = default;


        public double MaintenanceExcess { get; set; } = default;


        public bool IsRealTime { get; set; } = default;
    }
}

namespace PollQT.DataTypes
{
    public class AccountPosition : JsonSerializable<AccountPosition>
    {
        public string Symbol { get; set; } = "";
        public int SymbolId { get; set; } = default;
        public double OpenQuantity { get; set; } = default;
        public double ClosedQuantity { get; set; } = default;
        public double CurrentMarketValue { get; set; } = default;
        public double CurrentPrice { get; set; } = default;
        public double AverageEntryPrice { get; set; } = default;
        public double ClosedPnL { get; set; } = default;
        public double OpenPnL { get; set; } = default;
        public double TotalCost { get; set; } = default;
        public bool IsRealTime { get; set; } = default;
        public bool IsUnderReorg { get; set; } = default;
    }
}

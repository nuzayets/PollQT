using System;

namespace PollQT.DataTypes
{
    internal class AccountActivity : JsonSerializable<AccountActivity>
    {
        public string Type { get; set; } = "";
        public DateTimeOffset TradeDate { get; set; } = default;
        public DateTimeOffset TransactionDate { get; set; } = default;
        public DateTimeOffset SettlementDate { get; set; } = default;
        public string Action { get; set; } = "";
        public string Symbol { get; set; } = "";
        public uint SymbolId { get; set; } = default;
        public string Description { get; set; } = "";
        public double Quantity { get; set; } = default;
        public double Price { get; set; } = default;
        public double GrossAmount { get; set; } = default;
        public double Commission { get; set; } = default;
        public double NetAmount { get; set; } = default;
    }
}

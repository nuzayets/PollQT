namespace PollQT.DataTypes
{


    internal class Account : JsonSerializable<Account>
    {


        public string Type { get; set; } = "";


        public string Number { get; set; } = "";


        public string Status { get; set; } = "";


        public bool IsPrimary { get; set; } = default;


        public bool IsBilling { get; set; } = default;


        public string ClientAccountType { get; set; } = "";
    }
}

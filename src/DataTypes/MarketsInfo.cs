using System;

namespace PollQT.DataTypes
{
    internal class MarketsInfo : JsonSerializable<MarketsInfo>
    {
        public Market[] Markets { get; set; } = Array.Empty<Market>();
    }

    internal class Market : JsonSerializable<Market>
    {
        public string Name { get; set; } = "";
        public DateTimeOffset StartTime { get; set; } = default;
        public DateTimeOffset EndTime { get; set; } = default;
    }
}

namespace EfxSimulator.Api.Models
{
    public sealed class FxPrice
    {
        public required string Pair { get; init; }

        public decimal Bid { get; init; }

        public decimal Ask { get; init; }

        public decimal Mid => (Bid + Ask) / 2;

        public decimal Spread => Ask - Bid;

        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }
}
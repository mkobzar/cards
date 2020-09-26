using System.Collections.Generic;

namespace Cards
{
    public class CardBook
    {
        public List<CardGroups> ColorGroups { get; set; }
        public Dictionary<int, string> InsertBlocks { get; set; }
        public Kolor[][][] BackgroundColors { get; set; }
        public Dictionary<ulong, int> LevelCounters { get; set; }
        public List<Card4s> Cards { get; set; }
        public Dictionary<string, int> DistinctColorGroupssAndCounters { get; set; }
    }
}

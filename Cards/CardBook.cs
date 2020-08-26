using System.Collections.Generic;

namespace Cards
{
    public class CardBook
    {
        public List<string> CoverBlocks = new List<string>();
        public List<List<string>> BackgroundColors = new List<List<string>>();
        public Dictionary<ulong, int> LevelCounters { get; set; }
        public List<Card4s> Cards { get; set; }
        public Dictionary<string, int> DistinctCardsAndCounters { get; set; }
    }
}

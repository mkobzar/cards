using System.Collections.Generic;

namespace Cards
{
    public class CardStat
    {
        public int WindowID { get; set; }
        public int InsertBlockID { get; set; }
        public int InsertBlockPosition { get; set; }
        public string InsertBlockHoles { get; set; }
        public string ColorDotsOfThisWindow { get; set; }
        public List<string> Colors { get; set; }
    }
}

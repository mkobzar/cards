using System.Collections.Generic;

namespace Cards
{
    public class CardStat
    {
        public int WindowID { get; set; }
        public int InsertBlockID { get; set; }
        public int InsertBlockPosition { get; set; }
        public string InsertBlockPattern { get; set; }
        public string OpenedColors { get; set; }
        public List<string> Colors { get; set; }
    }
}

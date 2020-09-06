using System;
using System.Collections.Generic;
using System.Linq;


namespace Cards
{
    public class Card4s
    {
        public int ID { get; set; }
        public ulong Level { get; set;  }
        public int Variation { get; set; }
        public string CardColors { get; set; }
        public List<CardStat> Windows { get; set; }
        public void CaclulateLevel()
        {
            if (string.IsNullOrEmpty(CardColors)) return;
            var colors = CardColors.Split(',');
            var colorsGrouped = colors.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            var colorCount = colors.Distinct().Count();
            double level = 1;  
            foreach (var g in colorsGrouped)
            {
                level = level * Math.Pow(colorCount, g.Value);
            }
            Level = (ulong)(level * colors.Count());
        }
    }

   

    public class CardGroups
    {
        public int GroupID { get; set; }
        public ulong Level { get; set; }
        public string FindColors { get; set; }
        public List<Card4> GroupsOfPossibleVariationsForThisColorList { get; set; }
    }


    public class Card4
    {
        public int ID { get; set; }
        public int Variation { get; set; }
        public List<CardStat> Windows { get; set; }
    }
}

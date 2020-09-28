using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace Cards
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Kolor
    {
        Blue,
        Green,
        Red,
        White,
        Yellow,
        None
    }

    public class CardBook
    {
        public List<CardGroups> ColorGroups { get; set; }
        public Dictionary<int, string> InsertBlocks { get; set; }
        public Kolor[][][] BackgroundColors { get; set; }
        public Dictionary<ulong, int> LevelCounters { get; set; }
        public List<Card4s> Cards { get; set; }
        public Dictionary<string, int> DistinctColorGroupssAndCounters { get; set; }
    }
    public class Card4s
    {
        public int ID { get; set; }
        public ulong Level { get; set; }
        public int Variation { get; set; }
        public string ColorCode { get; set; }
        public List<CardStat> Windows { get; set; }
        public int GroupID { get; set; }
        public void CaclulateLevel()
        {
            if (string.IsNullOrEmpty(ColorCode)) return;
            var colors = ColorCode.Split(',');
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
        public string ColorCode { get; set; }
        public List<Card4> VariationsOfSameColors { get; set; }
    }

    public class CardDescription
    {
        public string ColorList { get; set; }
        public int Id { get; set; }
        public int Variations { get; set; }
        public ulong Level { get; set; }
    }

    public class Card4
    {
        public int ID { get; set; }
        public int Variation { get; set; }
        public List<CardStat> Windows { get; set; }
    }

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

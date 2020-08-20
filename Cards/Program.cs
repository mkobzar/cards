using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Cards
{
    class Program
    {
        static void Main(string[] args)
        {
            GetCards();
        }


        private static void GetCards()
        {
            var cardBook = new CardBook();
            var backgroundColors = GetBackgroundColors();
            
            foreach (var bcg in backgroundColors)
            {
                var bl = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    var cStr = "";
                    for (var j = 0; j < 3; j++)
                    {
                        var bc = bcg[i,j];
                        cStr = cStr == "" ? bc.ToString() : cStr + "," + bc.ToString();
                    }
                    bl.Add(cStr);
                }
                cardBook.BackgroundColors.Add(bl);
            }
            cardBook.CoverBlocks = BoolsToStrList(CoverBlocks); 
            var cardList = GetDistinctHoles(CoverBlocks);  
            var cardOrders = ArrangeCards(); 
            var list3CardStat = new List<List<List<CardStat>>>();
            foreach (var cardOrder in cardOrders)  
            {
                var list2CardStat = new List<List<CardStat>>();
                for (var i = 0; i < 4; i++)
                {
                    var cardindex = cardOrder[i];
                    var b = backgroundColors[i];
                    var h = cardList[cardindex];
                    var list1CardStat = DistinctColorOfOneArea(i, cardindex, b, h);
                    list2CardStat.Add(list1CardStat);
                }
                list3CardStat.Add(list2CardStat);
            }
            cardBook.Cards = CardParser(list3CardStat);
            cardBook.Cards = cardBook.Cards.OrderBy(x => x.CardColors).ToList();
            cardBook.DistinctCards = cardBook.Cards.GroupBy(x => x.CardColors).OrderByDescending(x => x.Count()).ToDictionary(x => x.Key, x => x.Count());
            var cardBookJson = JsonConvert.SerializeObject(cardBook);
        }
 
        private static List<Card4s> CardParser(List<List<List<CardStat>>> cccc)
        {
            var cardStatList = new List<Card4s>();
            var id = 1;
            foreach (var ccc in cccc)
            {
                var A = ccc[0];
                var B = ccc[1];
                var C = ccc[2];
                var D = ccc[3];
                foreach (var a in A)
                {
                    foreach (var b in B)
                    {
                        foreach (var c in C)
                        {
                            foreach (var d in D)
                            {
                                var y = new List<string>();
                                y.AddRange(a.Colors);
                                y.AddRange(b.Colors);
                                y.AddRange(c.Colors);
                                y.AddRange(d.Colors);
                                y.Sort();
                                var x = string.Join(",", y);
                                var cardStat = new Card4s
                                {
                                    ID = id++,
                                    CardColors = x,
                                    Cards = new List<CardStat> { a, b, c, d }
                                };
                                cardStatList.Add(cardStat);
                                
                            }
                        }
                    }
                }
            }
            return cardStatList;
        }
 
 
      
        /// <summary>
        /// get index on the window for each of 4 
        /// </summary>
        /// <returns></returns>
        private static List<int[]> ArrangeCards()
        {
            var arrays = new List<string>();
            var arraysInt = new List<int[]>();
            for (var a = 0; a < 4; a++)
            {
                for (var b = 0; b < 4; b++)
                {
                    if (a == b) continue;
                    for (var c = 0; c < 4; c++)
                    {
                        if (a == c || b == c) continue;
                        for (var d = 0; d < 4; d++)
                        {
                            if (a == d || b == d || c == d) continue;
                            var s = $"{a}{b}{c}{d}";
                            if (!arrays.Contains(s))
                            {
                                arraysInt.Add(new[] { a, b, c, d });
                                arrays.Add(s);
                            }
                        }
                    }
                }
            }
            return arraysInt;
        }

        /// <summary>
        /// get static background colors 
        /// </summary>
        /// <returns></returns>
        private static List<Color[,]> GetBackgroundColors()
        {
            var list = new List<Color[,]>();
            list.Add(new Color[,] { { Color.Yellow, Color.None, Color.Blue }, { Color.Red, Color.Green, Color.Red }, { Color.White, Color.Blue, Color.Yellow } });
            list.Add(new Color[,] { { Color.Green, Color.None, Color.Blue }, { Color.None, Color.Red, Color.Yellow }, { Color.Blue, Color.White, Color.Green } });
            list.Add(new Color[,] { { Color.White, Color.Yellow, Color.Red }, { Color.Green, Color.None, Color.Green }, { Color.Blue, Color.None, Color.White } });
            list.Add(new Color[,] { { Color.None, Color.None, Color.None }, { Color.None, Color.White, Color.Blue }, { Color.Green, Color.Yellow, Color.Red } });
            return list;
        }

        private static bool[,,] CoverBlocks = new bool[,,] {
            { { false, true, true }, { false, false, false }, { true, false, false } },
            { { false, true, false }, { false, false, false }, { false, true, false } },
            { { false, false, false }, { false, true, false }, { false, true, false } },
            { { true, false, false }, { false, false, false }, { false, true, false } }};

        private static List<string> BoolsToStrList(bool[,,] holes)
        {
            var retStrList = new List<string>();

            for (var t = 0; t < 4; t++)
            {
                var hl = new List<char>();
                for (var p = 0; p < 3; p++)
                {
                    for (var r = 0; r < 3; r++)
                    {
                        var b = holes[t, p, r];
                        if (b)
                            hl.Add('O');
                        else
                            hl.Add('C');
                    }
                    if (p < 2)
                        hl.Add(' ');
                }

                retStrList.Add(string.Join("", hl));
            }
            return retStrList;
        }

        private static List<CardStat> DistinctColorOfOneArea(int windowIndex, int cardIndex, Color[,] paintedColors, List<bool[,]> holesList)
        {
            var cardStatList = new List<CardStat>();
            //var allLists = new Dictionary<string, List<Color>>();
            var listStr = new List<string>();
            for (int i = 0; i < holesList.Count; i++)
            {
                var holes = holesList[i];
              
                var hl = new List<char>();
                for (var p = 0; p < 3; p++)
                {
                    for (var r = 0; r < 3; r++)
                    {
                        var b = holes[p, r];
                        if (b)
                            hl.Add('O');
                        else
                            hl.Add('C');
                    }
                    if (p < 2)
                        hl.Add(' ');
                }

                var holesStr = string.Join("", hl);
                var colors = new List<Color>(); // TODO DELETE
                var colorList = new List<string>();
                for (var y = 0; y < 3; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        if (holes[y, x] && paintedColors[y, x] != Color.None)
                        {
                            colors.Add(paintedColors[y, x]);
                            colorList.Add(paintedColors[y, x].ToString());
                        }
                    }
                }
                colorList.Sort();
                colors.Sort();
                var str = string.Join(",", colorList);
                if (!listStr.Contains(str))
                {
                    listStr.Add(str);
                    var cardStat = new CardStat
                    {
                        Window = windowIndex + 1,
                        Card = cardIndex + 1,
                        CardHoles = holesStr,
                        CardPosition = i + 1,
                        Colors = colorList,
                    };
                    cardStatList.Add(cardStat);
                }
            }
            return cardStatList;
        }

        /// <summary>
        /// get list of lists of all possible holes for all 4 windows
        /// </summary>
        /// <returns></returns>
        private static List<List<bool[,]>> GetDistinctHoles(bool[,,] blocks)
        {
            var holes = new List<List<bool[,]>>();
            for (var i = 0; i < 4; i++)
            {
                var v = GenerateHoles(new bool[3, 3] { { blocks[i, 0, 0], blocks[i, 0, 1], blocks[i, 0, 2] }, { blocks[i, 1, 0], blocks[i, 1, 1], blocks[i, 1, 2] }, { blocks[i, 2, 0], blocks[i, 2, 1], blocks[i, 2, 2] } });
                holes.Add(v);
            }

            return holes;
        }

        /// <summary>
        /// get list of all possible holes of given window
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        private static List<bool[,]> GenerateHoles(bool[,] window)
        {
            var bools = new bool[] { true, false };
            var distinctHoles = new List<bool[,]> { window };
            foreach (var b in bools)
            {
                for (var i = 0; i <= 3; i++)
                {
                    AddDistinctHoles(distinctHoles, HolesRotate(window, i, b));
                }
            }
            return distinctHoles;
        }

        /// <summary>
        /// Add Distinct Holes to the holesList
        /// </summary>
        /// <param name="holesList"></param>
        /// <param name="newHoles"></param>
        private static void AddDistinctHoles(List<bool[,]> holesList, bool[,] newHoles)
        {
            foreach (var h in holesList)
            {
                var same = HolesAreSame(h, newHoles);
                if (same)
                {
                    return;
                }
            }
            holesList.Add(newHoles);
        }

        private static bool[,] CopyHoles(bool[,] holes, bool flip)
        {
            var newHoles = new bool[3, 3];
            for (var r = 0; r < 3; r++)
            {
                for (var c = 0; c < 3; c++)
                {
                    if (flip)
                        newHoles[r, 2 - c] = holes[r, c];
                    else
                        newHoles[r, c] = holes[r, c];
                }
            }
            return newHoles;
        }

        private static bool HolesAreSame(bool[,] holes1, bool[,] holes2)
        {
            for (var r = 0; r < 3; r++) // r = row
            {
                for (var c = 0; c < 3; c++) // c = column
                {
                    if (holes1[r, c] != holes2[r, c])
                        return false;
                }
            }
            return true;
        }

        private static bool[,] HolesRotate(bool[,] holes, int rotateCount, bool flip)
        {
            var rotetedHoles = new bool[3, 3];
            var copiedHoles = CopyHoles(holes, flip);
            for (var i = 0; i <= rotateCount; i++)
            {
                for (var y = 0; y < 3; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        rotetedHoles[y, x] = copiedHoles[x, 2 - y];
                    }
                }
                copiedHoles = CopyHoles(rotetedHoles, false);
            }
            return rotetedHoles;
        }
    }

    public class CardBook
    {
        public List<string> CoverBlocks = new List<string>();
        public List<List<string>> BackgroundColors = new List<List<string>>();
        public List<Card4s> Cards { get; set; }
        public Dictionary<string, int> DistinctCards { get; set; }
    }
    public class Card4s
    {
        public int ID { get; set; }
        public string CardColors { get; set; }
        public List<CardStat> Cards { get; set; }
    }

    public class CardStat
    {
        public int Window { get; set; }
        public int Card { get; set; }
        public int CardPosition { get; set; }
        public string CardHoles { get; set; }
        public List<string> Colors  { get; set; }
    }

    public enum Color
    {
        Blue,
        Green,
        Red,
        White,
        Yellow,
        None
    }
}

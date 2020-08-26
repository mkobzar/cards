using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Cards
{
    public class Cards
    {
        private string WindowsLeafsFileName = "WindowsLeaf.json";
        private string BackgroundColorsFileName = "BackgroundColors.json";
        private bool[,,] WindowsLeafs;
        private List<Color[,]> BackgroundColors;
        private JsonSerializerSettings DefaultJsonSerializerSettingz { get; set; }

        public Cards(string[] args)
        {
            if (args != null && args.Length == 2)
            {
                if (args.Any(x => x.ToLower().Contains("window")) && args.Any(x => x.ToLower().Contains("color")))
                {
                    WindowsLeafsFileName = args.Where(x => x.ToLower().Contains("window")).FirstOrDefault();
                    BackgroundColorsFileName = args.Where(x => x.ToLower().Contains("color")).FirstOrDefault();
                }
            }
        }


        private bool ReadInputSettings()
        {
            try
            {
                if (!File.Exists(WindowsLeafsFileName))
                {
                    Console.WriteLine($"{WindowsLeafsFileName} is not exist. Program is aborted");
                    return false;
                }
                if (!File.Exists(BackgroundColorsFileName))
                {
                    Console.WriteLine($"{BackgroundColorsFileName} is not exist. Program is aborted");
                    return false;
                }

                var streamReader = new StreamReader(WindowsLeafsFileName);
                var fileContent = streamReader.ReadToEnd();
                WindowsLeafs = JsonConvert.DeserializeObject<bool[,,]>(fileContent);
                streamReader.Close();

                streamReader = new StreamReader(BackgroundColorsFileName);
                fileContent = streamReader.ReadToEnd();
                BackgroundColors = JsonConvert.DeserializeObject<List<Color[,]>>(fileContent);
                streamReader.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}\n{e.StackTrace}");
            }
            Console.WriteLine("failed complete ReadInputSettings(). Program is aborted");
            return false;
        }

        public void Run()
        {
            if (!ReadInputSettings()) return;

            var cardBook = new CardBook();
            foreach (var bcg in BackgroundColors)
            {
                var bl = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    var cStr = "";
                    for (var j = 0; j < 3; j++)
                    {
                        var bc = bcg[i, j];
                        cStr = cStr == "" ? bc.ToString() : cStr + "," + bc.ToString();
                    }
                    bl.Add(cStr);
                }
                cardBook.BackgroundColors.Add(bl);
            }
            cardBook.CoverBlocks = BoolsToStrList(WindowsLeafs);
            var cardList = GetDistinctHoles(WindowsLeafs);
            var cardOrders = ArrangeCards();
            var list3CardStat = new List<List<List<CardStat>>>();
            foreach (var cardOrder in cardOrders)
            {
                var list2CardStat = new List<List<CardStat>>();
                for (var i = 0; i < 4; i++)
                {
                    var cardindex = cardOrder[i];
                    var b = BackgroundColors[i];
                    var h = cardList[cardindex];
                    var list1CardStat = DistinctColorOfOneArea(i, cardindex, b, h);
                    list2CardStat.Add(list1CardStat);
                }
                list3CardStat.Add(list2CardStat);
            }
            cardBook.Cards = CardParser(list3CardStat);
            cardBook.Cards = cardBook.Cards.OrderBy(x => x.Level).ToList();
            ulong previousLevel = 1;
            ulong smartLevel = 0;
            foreach (var card in cardBook.Cards)
            {
                var thisLevel = card.Level;
                if (card.Level > previousLevel)
                    card.Level = ++smartLevel;
                else
                    card.Level = smartLevel;
                previousLevel = thisLevel;
            }
            cardBook.LevelCounters = cardBook.Cards.GroupBy(x => x.Level).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Count());
            cardBook.DistinctCards = cardBook.Cards.GroupBy(x => x.CardColors).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Count());
            var cardBookJson = JsonConvert.SerializeObject(cardBook, JsonSerializerSettingsIgnoingNulls);
            Console.WriteLine($"found {cardBook.DistinctCards.Count} Distinct Cards\nTotal Cards count: {cardBook.Cards.Count}");
            var fileOutput = $"CardsReport_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.json";
            var sw = new StreamWriter(fileOutput);
            sw.Write(cardBookJson);
            sw.Close();
            Console.WriteLine($"Cards report written to {fileOutput}");
        }

        private List<Card4s> CardParser(List<List<List<CardStat>>> cccc)
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
                                cardStat.CaclulateLevel();
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
        private List<int[]> ArrangeCards()
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

        private List<string> BoolsToStrList(bool[,,] holes)
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
                            hl.Add('X');
                    }
                    if (p < 2)
                        hl.Add(' ');
                }

                retStrList.Add(string.Join("", hl));
            }
            return retStrList;
        }

        private List<CardStat> DistinctColorOfOneArea(int windowIndex, int cardIndex, Color[,] paintedColors, List<bool[,]> holesList)
        {
            var cardStatList = new List<CardStat>();
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
                            hl.Add('X');
                    }
                    if (p < 2)
                        hl.Add(' ');
                }

                var holesStr = string.Join("", hl);
                //var colors = new List<Color>(); // TODO DELETE
                var colorList = new List<string>();
                for (var y = 0; y < 3; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        if (holes[y, x] && paintedColors[y, x] != Color.None)
                        {
                            //colors.Add(paintedColors[y, x]);
                            colorList.Add(paintedColors[y, x].ToString());
                        }
                    }
                }
                colorList.Sort();
                //colors.Sort();
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
        private List<List<bool[,]>> GetDistinctHoles(bool[,,] blocks)
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
        private List<bool[,]> GenerateHoles(bool[,] window)
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
        private void AddDistinctHoles(List<bool[,]> holesList, bool[,] newHoles)
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

        private bool[,] CopyHoles(bool[,] holes, bool flip)
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

        private bool HolesAreSame(bool[,] holes1, bool[,] holes2)
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

        private bool[,] HolesRotate(bool[,] holes, int rotateCount, bool flip)
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

        private JsonSerializerSettings JsonSerializerSettingsIgnoingNulls
        {
            get
            {
                DefaultJsonSerializerSettingz = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                };
                DefaultJsonSerializerSettingz.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                return DefaultJsonSerializerSettingz;
            }
        }

    }
}

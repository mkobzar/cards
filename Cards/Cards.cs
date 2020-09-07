using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cards
{
    public class Cards
    {
        private readonly string _windowsLeafsFileName = "WindowsLeaf.json";
        private readonly string _backgroundColorsFileName = "BackgroundColors.json";
        private bool[,,] _windowsLeafs;
        private List<Color[,]> _backgroundColors;
        private JsonSerializerSettings DefaultJsonSerializerSettings { get; set; }
        private CardBook CardBook { get; set; }

        // ReSharper disable once CollectionNeverQueried.Local
        private List<Card4s> Card4S { get; set; }

        /// <summary>
        /// Cards initialization
        /// </summary>
        /// <param name="args"></param>
        public Cards(IReadOnlyCollection<string> args)
        {
            if (args == null || args.Count != 2) return;
            if (!args.Any(x => x.ToLower().Contains("window")) || !args.Any(x => x.ToLower().Contains("color"))) return;
            _windowsLeafsFileName = args.FirstOrDefault(x => x.ToLower().Contains("window"));
            _backgroundColorsFileName = args.FirstOrDefault(x => x.ToLower().Contains("color"));
        }

        /// <summary>
        /// Cards execution
        /// </summary>
        public void Run()
        {
            if (!ReadInputSettings()) return;
            CardBook = new CardBook
            {
                BackgroundColors = new List<List<string>>()
            };
            foreach (var bcg in _backgroundColors)
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

                CardBook.BackgroundColors.Add(bl);
            }

            CardBook.InsertBlocks = BoolsToStrList(_windowsLeafs);
            var cardList = GetDistinctHoles(_windowsLeafs);
            var cardOrders = ArrangeCards();
            var list3CardStat = new List<List<List<CardStat>>>();
            foreach (var cardOrder in cardOrders)
            {
                var list2CardStat = new List<List<CardStat>>();
                for (var i = 0; i < 4; i++)
                {
                    var cardIndex = cardOrder[i];
                    var b = _backgroundColors[i];
                    var h = cardList[cardIndex];
                    var list1CardStat = DistinctColorOfOneArea(i, cardIndex, b, h);
                    list2CardStat.Add(list1CardStat);
                }

                list3CardStat.Add(list2CardStat);
            }

            CardBook.Cards = CardParser(list3CardStat);
            CardBookGroupAndOrder();
            CardBookPrint();
            //var c = Card4S.FirstOrDefault(x => x.ID == 3);
            //var cs = CardBook.ColorGroups.SelectMany(x => x.VariationsOfSameColors.Where(y => y.ID == 12)).FirstOrDefault();
        }

        private void CardBookPrint()
        {
            var cardBookJson = JsonConvert.SerializeObject(CardBook, JsonSerializerSettingsIgnoringNulls);
            var fileOutput = $"CardsReport_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.json";
            var sw = new StreamWriter(fileOutput);
            sw.Write(cardBookJson);
            sw.Close();
            Console.WriteLine($"Cards report written to {fileOutput}");
        }

        private static List<CardGroups> CardsToGroups(Dictionary<string, List<Card4s>> gg)
        {
            var cardGroups = new List<CardGroups>();
            var groupId = 1;
            var id = 1;
            foreach (var g in gg)
            {
                var variation = 1;
                var cg = new CardGroups
                {
                    ColorCode = g.Key,
                    // ReSharper disable once PossibleNullReferenceException
                    Level = g.Value.FirstOrDefault().Level
                };
                var card4List = g.Value.Select(c => new Card4
                {
                    Windows = c.Windows,
                    Variation = variation++
                }).ToList();
                cg.VariationsOfSameColors = card4List;
                cardGroups.Add(cg);
            }

            cardGroups = cardGroups.OrderBy(x => x.Level).ToList();
            cardGroups.ForEach(x =>
            {
                x.GroupID = groupId++;
                x.VariationsOfSameColors.ForEach(y => y.ID = id++);
            });
            return cardGroups;
        }

        private void CardBookGroupAndOrder()
        {
            CardBook.Cards = CardBook.Cards.OrderBy(x => x.Level).ToList();
            ulong previousLevel = 1;
            var previousVariation = 0;
            var previousColor = "";
            ulong smartLevel = 0;
            foreach (var card in CardBook.Cards)
            {
                var thisLevel = card.Level;
                if (!string.Equals(card.ColorCode, previousColor))
                {
                    previousVariation = 1;
                    previousColor = card.ColorCode;
                }
                else
                {
                    previousVariation++;
                }

                card.Variation = previousVariation;
                if (card.Level > previousLevel)
                {
                    // id = 1;
                    card.Level = ++smartLevel;
                }
                else
                {
                    card.Level = smartLevel;
                }

                // card.ID = $"{card.ID}:{id++}:{card.Variation}";
                previousLevel = thisLevel;
            }

            CardBook.LevelCounters = CardBook.Cards.GroupBy(x => x.Level).OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Count());
            CardBook.DistinctColorGroupssAndCounters = CardBook.Cards.GroupBy(x => x.ColorCode).OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Count());
            CardBook.Cards = CardBook.Cards.OrderBy(x => x.Level).ToList();
            CardBook.ColorGroups = new List<CardGroups>();
            CardBook.Cards = CardBook.Cards.OrderBy(x => x.ColorCode).ToList();
            var gg = CardBook.Cards.GroupBy(x => x.ColorCode).ToDictionary(x => x.Key, x => x.ToList());
            CardBook.ColorGroups = CardsToGroups(gg);
            Card4S = new List<Card4s>();
            foreach (var cg in CardBook.ColorGroups)
            {
                foreach (var c in cg.VariationsOfSameColors)
                {
                    Card4S.Add(new Card4s
                    {
                        ID = c.ID,
                        ColorCode = cg.ColorCode,
                        GroupID = cg.GroupID,
                        Level = cg.Level,
                        Variation = c.Variation,
                        Windows = c.Windows
                    });
                }
            }

            CardBook.Cards = null;
        }

        /// <summary>
        /// read input settings from json files
        /// </summary>
        /// <returns></returns>
        private bool ReadInputSettings()
        {
            try
            {
                if (!File.Exists(_windowsLeafsFileName))
                {
                    Console.WriteLine($"{_windowsLeafsFileName} is not exist. Program is aborted");
                    return false;
                }

                if (!File.Exists(_backgroundColorsFileName))
                {
                    Console.WriteLine($"{_backgroundColorsFileName} is not exist. Program is aborted");
                    return false;
                }

                var streamReader = new StreamReader(_windowsLeafsFileName);
                var fileContent = streamReader.ReadToEnd();
                _windowsLeafs = JsonConvert.DeserializeObject<bool[,,]>(fileContent);
                streamReader.Close();

                streamReader = new StreamReader(_backgroundColorsFileName);
                fileContent = streamReader.ReadToEnd();
                _backgroundColors = JsonConvert.DeserializeObject<List<Color[,]>>(fileContent);
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

        /// <summary>
        /// converts true and false to O and X chars
        /// </summary>
        /// <param name="holes"></param>
        /// <returns></returns>
        private static Dictionary<int, string> BoolsToStrList(bool[,,] holes)
        {
            var retStrList = new Dictionary<int, string>();

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

                retStrList.Add(t + 1, string.Join("", hl));
            }

            return retStrList;
        }


        /// <summary>
        /// parse 
        /// </summary>
        /// <param name="cccc"></param>
        /// <returns></returns>
        private static List<Card4s> CardParser(IEnumerable<List<List<CardStat>>> cccc)
        {
            var id = 1;
            var cardStatList = new List<Card4s>();
            foreach (var ccc in cccc)
            {
                // ReSharper disable InconsistentNaming
                var A = ccc[0];
                var B = ccc[1];
                var C = ccc[2];
                var D = ccc[3];
                // ReSharper restore InconsistentNaming
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
                                    ColorCode = x,
                                    Windows = new List<CardStat> {a, b, c, d},
                                    ID = id++
                                };
                                cardStat.CaclulateLevel();
                                cardStatList.Add(cardStat);
                            }
                        }
                    }
                }

                foreach (var c in ccc.SelectMany(cc => cc))
                {
                    c.OpenedColors = string.Join(", ", c.Colors);
                    c.Colors = null;
                }
            }

            return cardStatList;
        }


        /// <summary>
        /// get index on the window for each of 4 
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<int[]> ArrangeCards()
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
                            if (arrays.Contains(s)) continue;
                            arraysInt.Add(new[] {a, b, c, d});
                            arrays.Add(s);
                        }
                    }
                }
            }

            return arraysInt;
        }


        private static List<CardStat> DistinctColorOfOneArea(int windowIndex, int cardIndex, Color[,] paintedColors,
            IReadOnlyList<bool[,]> holesList)
        {
            var cardStatList = new List<CardStat>();
            var listStr = new List<string>();
            for (var i = 0; i < holesList.Count; i++)
            {
                var holes = holesList[i];

                var hl = new List<char>();
                for (var p = 0; p < 3; p++)
                {
                    for (var r = 0; r < 3; r++)
                    {
                        var b = holes[p, r];
                        hl.Add(b ? 'O' : 'X');
                    }

                    if (p < 2)
                        hl.Add(' ');
                }

                var holesStr = string.Join("", hl);
                var colorList = new List<string>();
                for (var y = 0; y < 3; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        if (holes[y, x] && paintedColors[y, x] != Color.None)
                        {
                            colorList.Add(paintedColors[y, x].ToString());
                        }
                    }
                }

                colorList.Sort();
                var str = string.Join(",", colorList);
                if (listStr.Contains(str)) continue;
                listStr.Add(str);
                var cardStat = new CardStat
                {
                    WindowID = windowIndex + 1,
                    InsertBlockID = cardIndex + 1,
                    InsertBlockPattern = holesStr,
                    InsertBlockPosition = i + 1,
                    Colors = colorList,
                };
                cardStatList.Add(cardStat);
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
                var v = GenerateHoles(new[,]
                {
                    {blocks[i, 0, 0], blocks[i, 0, 1], blocks[i, 0, 2]},
                    {blocks[i, 1, 0], blocks[i, 1, 1], blocks[i, 1, 2]},
                    {blocks[i, 2, 0], blocks[i, 2, 1], blocks[i, 2, 2]}
                });
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
            var bools = new[] {true, false};
            var distinctHoles = new List<bool[,]> {window};
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
        private void AddDistinctHoles(ICollection<bool[,]> holesList, bool[,] newHoles)
        {
            if (holesList.Select(h => HolesAreSame(h, newHoles)).Any(same => same))
            {
                return;
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

        private JsonSerializerSettings JsonSerializerSettingsIgnoringNulls
        {
            get
            {
                DefaultJsonSerializerSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                };
                DefaultJsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                return DefaultJsonSerializerSettings;
            }
        }
    }
}

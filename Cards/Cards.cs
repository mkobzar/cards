using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Cards
{
    public class Cards
    {
        private readonly string _windowsLeafsFileName = "WindowsLeaf.json";
        private readonly string _backgroundColorsFileName = "BackgroundColors.json";
        private bool[][][] _windowsLeafs;
        private Kolor[][][] _backgroundColors;
        private JsonSerializerSettings DefaultJsonSerializerSettings { get; set; }
        private CardBook CardBook { get; set; }
        private Dictionary<int, bool[,]> Locations = new Dictionary<int, bool[,]>();
        private Dictionary<int, int> ReverseImageIndex = new Dictionary<int, int>();
        Font MyFont = new Font("Microsoft Sans Serif", 11);
        //Font MyFont = new Font("Microsoft Sans Serif", 24);
        private int Dia = 36;
        private int Width = 256;
        private int Height = 157;
        Rectangle[] Rectangles = new Rectangle[20];
        // ReSharper disable once CollectionNeverQueried.Local
        private List<Card4s> Card4S { get; set; }

        private string CardToStr(Card4 cv, int i)
        {
            return $"{cv.Windows[i].InsertBlockID}:{cv.Windows[i].InsertBlockPattern}={cv.Windows[i].OpenedColors}";
        }

        private void Tocsv()
        {
            var header = "ID\tGroupID\tLevel\tVariation\tColorList\tBlock1\tBlock2\tBlock3\tBlock4";
            var sw = new StreamWriter("Cards.csv");
            sw.WriteLine(header);
            foreach (var cg in CardBook.ColorGroups)
            {
                var variation = 1;
                foreach (var cv in cg.VariationsOfSameColors)
                {
                    sw.WriteLine($"{cv.ID}\t{cg.GroupID}\t{cg.Level}\t{variation++}\t{cg.ColorCode}\t{CardToStr(cv, 0)}\t{CardToStr(cv, 1)}\t{CardToStr(cv, 2)}\t{CardToStr(cv, 3)}");
                };
            }
        }

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
                BackgroundColors = _backgroundColors
            };

            CardBook.InsertBlocks = BoolsToStrList(_windowsLeafs);
            var cardList = GetDistinctHoles(_windowsLeafs);
            var cardOrders = ArrangeCards();
            var list3CardStat = new List<List<List<CardStat>>>();
            foreach (var cardOrder in cardOrders)
            {
                var list2CardStat = new List<List<CardStat>>();
                for (var i = 0; i < cardOrder.Length; i++)
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


        private void PopLocations()
        {
            var j = 0;
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 4; c++)
                {
                    ReverseImageIndex.Add(j, 3 - c + r * 4);
                    Rectangles[j++] = new Rectangle(c * Width, r * Height, Width, Height);
                }
            }
            Locations.Add(3, new bool[,] { { false, false, true }, { false, true, false }, { true, false, false } });
            Locations.Add(4, new bool[,] { { true, false, true }, { false, false, false }, { true, false, true } });
            Locations.Add(5, new bool[,] { { true, false, true }, { false, true, false }, { true, false, true } });
            Locations.Add(6, new bool[,] { { true, false, true }, { true, false, true }, { true, false, true } });
            Locations.Add(7, new bool[,] { { true, true, true }, { false, true, false }, { true, true, true } });
            Locations.Add(8, new bool[,] { { true, true, true }, { true, false, true }, { true, true, true } });
            Locations.Add(9, new bool[,] { { true, true, true }, { true, true, true }, { true, true, true } });
        }

        Bitmap GetBitmap(string colorList, string text)
        { 
            var cc = colorList.Split(',').Select(x => Color.FromName(x)).ToList();
            var bmp = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            var c = 0;
            double multiplierX = 2;
            double multiplierY = 1.2;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    if (Locations[cc.Count][i, j])
                    {
                        var offsetX = (int)((i + 1) * Dia * multiplierX - Dia);
                        var offsetY = (int)((j + 1) * Dia * multiplierY - Dia);
                        var p = new Pen(Color.Black);
                        var sb = new SolidBrush(cc[c++]);
                        g.FillEllipse(sb, offsetX, offsetY, Dia, Dia);
                        g.DrawEllipse(p, offsetX, offsetY, Dia, Dia);
                    }
                }
            }
            var brr = new SolidBrush(Color.Black);
            g.DrawString(text, MyFont, brr, 25, 130);
            // g.DrawString(text, MyFont, brr, 67, 27);
            return bmp;
        }

        Bitmap GetBitmap(CardDescription cardDescription)
        {
            var text = $"ID:{cardDescription.Id}, Level:{cardDescription.Level}, Variations:{cardDescription.Variations}";
            return GetBitmap(cardDescription.ColorList, text);
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

        private void Print48()
        {
            var kolors48 = new List<string>()
            {
                "Yellow,Yellow,Yellow,Yellow,Yellow",//1
                "Green,Green,Green,Green,Green",
                "Blue,Blue,Blue,Blue",
                "Red,Red,Red,Red,White,White,White",
                "Green,Green,Green,Green,Green,Green,Blue",
                "Blue,Blue,Blue,White,White,White",
                "Blue,Blue,Blue,White,White,White,White",
                "Blue,Blue,Blue,Red",
                "Blue,Blue,Blue,Blue,Yellow,Yellow,Yellow",
                "Green,Green,White,White,White",//10
                "Green,Green,Blue",
                "Green,Green,Green,Blue,Blue,Blue,Blue",
                "Green,Green,Green,Yellow",
                "Blue,Blue,Yellow,Yellow",
                "Yellow,Yellow,White,White,White,White,White",
                "White,Red,Red,Green",
                "Blue,Blue,Blue,White,White,Yellow,Yellow,Green,Green",
                "Green,Yellow,White",
                "Yellow,Yellow,Red,Red,Red,Red,Red",
                "Green,Red,Blue",//20
                "Yellow,Yellow,Yellow,Red,Red,White,Blue,Blue,Blue",
                "White,Red,Yellow",
                "Green,Red,Blue,Blue,Blue,Blue",
                "White,White,Yellow,Green",
                "Green,Green,Green,Green,Yellow,Blue",
                "Blue,Blue,Blue,Red,White,Blue",
                "Red,Red,Red,Green,White,White,White",
                "Red,Yellow,Yellow,Yellow,Yellow,Blue,Blue,Blue",
                "Yellow,Yellow,Yellow,White,White,White,Blue,Blue",
                "Yellow,White,White,Blue,Blue,Blue",//30
                "Yellow,White,White,Yellow,Green",
                "White,White,White,Blue,Blue,Blue,Green",
                "Yellow,Red,Red,Green",
                "White,White,White,White,Blue,Blue,Red,Red",
                "White,White,White,Yellow,Blue,Blue,Blue,Yellow",
                "White,White,Red,Yellow,Red",
                "White,White,White,Yellow,Yellow,Blue,Green,Red,Red",
                "Yellow,Yellow,Yellow,Yellow,Green,Red",// 38
                "Yellow,Yellow,Yellow,White,White,Green,Green",
                "Blue,White,White,White,Green,Green",//40
                "Yellow,Yellow,Yellow,Yellow,Blue,Blue,Green",
                "Red,Red,Red,Yellow,Red,Green",
                "Yellow,Yellow,Yellow,Yellow,Red,Green,Green",
                "Blue,Blue,Blue,Red,Red,Red,White",
                "Yellow,Yellow,Blue,Blue,Red,Red,Green,Green",
                "Blue,Blue,Blue,Blue,Red,Yellow,White",
                "Blue,Blue,Blue,Red,Red,Yellow,Yellow",
                "Yellow,Yellow,Yellow,Blue,Red,White,Green,Green",
            };
            var page = 1;
            var images = new List<Bitmap>();
            for (var i = 0; i < kolors48.Count; i++)
            {
                var image = GetBitmap(kolors48[i], (i + 1).ToString());
                images.Add(image);
                if (images.Count >= 20)
                {
                    SaveImages(images, $"48s_{page++:D2}");
                    images = new List<Bitmap>();
                }
            }
            if (images.Count > 0)
            {
                SaveImages(images, $"48s_{page++:D2}");
            }
        }

        private void CardBookGroupAndOrder()
        {
            PopLocations();
            Print48();
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
                    card.Level = ++smartLevel;
                }
                else
                {
                    card.Level = smartLevel;
                }

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

            Tocsv();
 

            var images1 = new List<Bitmap>();
            var images48 = new List<Bitmap>();
            var images2 = new List<Bitmap>();
            var page = 1;
            var odd = true;
            foreach (var cg in CardBook.ColorGroups)
            {
                var cardDescription = new CardDescription()
                {
                    ColorList = cg.ColorCode,
                    Id = cg.GroupID,
                    Level = cg.Level,
                    Variations = cg.VariationsOfSameColors.Count
                };
                var bmp = GetBitmap(cardDescription);
                bmp.Save($"{cardDescription.Id:D4}.png");

                if (odd)
                    images1.Add(bmp);
                else
                    images2.Add(bmp);
                odd = !odd;
                if (images1.Count >= 20)
                {
                    SaveImages(images1, $"A_{page:D2}");
                    images1 = new List<Bitmap>();
                }
                if (images2.Count >= 20)
                {
                    SaveImages(images2, $"B_{page++:D2}", true);
                    images2 = new List<Bitmap>();
                }
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
            if (images1.Count > 0)
            {
                SaveImages(images1, $"A_{page:D2}");
            }
            if (images2.Count > 0)
            {
                SaveImages(images2, $"B_{page++:D2}", true);
            }
            CardBook.Cards = null;
        }

        void SaveImages(List<Bitmap> bitmaps, string name, bool reverse = false)
        {
            if (bitmaps.Count > 20)
                throw new Exception("dont try to save more than 20 images");
            var destBitmap = new Bitmap(1024, 786);
            using (Graphics g = Graphics.FromImage(destBitmap)) { g.Clear(Color.White); }
            var srcRegion = Rectangles[0];
            for (var i = 0; i < bitmaps.Count; i++)
            {
                var destRecIndex = i;
                if (reverse)
                {
                    destRecIndex = ReverseImageIndex[i];
                }
                    
                CopyRegionIntoImage(bitmaps[i], srcRegion, ref destBitmap, Rectangles[destRecIndex]);
            }
            destBitmap.Save($"{name}.png");
        }

        void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
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
                _windowsLeafs = JsonConvert.DeserializeObject<bool[][][]>(fileContent);
                streamReader.Close();

                streamReader = new StreamReader(_backgroundColorsFileName);
                fileContent = streamReader.ReadToEnd();
                _backgroundColors = JsonConvert.DeserializeObject<Kolor[][][]>(fileContent);
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
        private static Dictionary<int, string> BoolsToStrList(bool[][][] holes)
        {
            var retStrList = new Dictionary<int, string>();

            for (var t = 0; t < holes.Length; t++)
            {
                var hl = new List<char>();
                for (var p = 0; p < holes[t].Length; p++)
                {
                    for (var r = 0; r < holes[t][p].Length; r++)
                    {
                        var b = holes[t][p][r];
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
        private static List<Card4s> CardParser(List<List<List<CardStat>>> cccc)
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
                                    Windows = new List<CardStat> { a, b, c, d },
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
                            arraysInt.Add(new[] { a, b, c, d });
                            arrays.Add(s);
                        }
                    }
                }
            }

            return arraysInt;
        }


        private static List<CardStat> DistinctColorOfOneArea(int windowIndex, int cardIndex, Kolor[][] paintedColors,
            IReadOnlyList<bool[,]> holesList)
        {
            var cardStatList = new List<CardStat>();
            var listStr = new List<string>();
            for (var i = 0; i < holesList.Count; i++)
            {
                var holes = holesList[i];
                var hl = new List<char>();
                for (var p = 0; p < holes.GetLength(0); p++)
                {
                    for (var r = 0; r < holes.GetLength(1); r++)
                    {
                        var b = holes[p, r];
                        hl.Add(b ? 'O' : 'X');
                    }

                    if (p < 2)
                        hl.Add(' ');
                }

                var holesStr = string.Join("", hl);
                var colorList = new List<string>();

                for (var y = 0; y < paintedColors.Length; y++)
                {
                    for (var x = 0; x < paintedColors[y].Length; x++)
                    {
                        if (holes[y, x] && paintedColors[y][x] != Kolor.None)
                        {
                            colorList.Add(paintedColors[y][x].ToString());
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
        private List<List<bool[,]>> GetDistinctHoles(bool[][][] blocks)
        {
            var holes = new List<List<bool[,]>>();
            for (var i3 = 0; i3 < 4; i3++)
            {
                var v = GenerateHoles(new[,]
                {
                    {blocks[i3][0][0],blocks[i3][0][1],blocks[i3][0][2]},
                    {blocks[i3][1][0],blocks[i3][1][1],blocks[i3][1][2]},
                    {blocks[i3][2][0],blocks[i3][2][1],blocks[i3][2][2]}
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
            var bools = new[] { true, false };
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
            var newHoles = new bool[holes.GetLength(0), holes.GetLength(1)];
            for (var r = 0; r < holes.GetLength(0); r++)
            {
                for (var c = 0; c < holes.GetLength(1); c++)
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
            if(holes1.Rank!=holes2.Rank || holes1.Length!=holes2.Length)
                return false;
            for (var r = 0; r < holes1.GetLength(0); r++) // r = row
            {
                for (var c = 0; c < holes1.GetLength(1); c++) // c = column
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
                for (var y = 0; y < rotetedHoles.GetLength(0); y++)
                {
                    for (var x = 0; x < rotetedHoles.GetLength(1); x++)
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

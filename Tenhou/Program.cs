using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Tenhou
{
    class Program
    {
        static void Main(string[] args)
        {
            string dirPath = "C:\\Documents and Settings\\YongxinWang\\My Documents\\My Tenhou\\log\\200910";
            string dirName = "200910";
            string[] files = Directory.GetFiles(dirPath, "*.mjlog");
            foreach (string filePath in files)
            {
                TenhouAna.processMjlog(dirName, filePath);
            }
        }
    }
    class TenhouAna
    {
        private static string[] TableLvl = 
        {
            "(一般)",
            "(上級)",
            "(特上)",
            ""
        };
        private static string[] TableType =
        {
            "喰アリ赤",
            "喰断アリ",
            "喰ナシ赤",
            "喰断ナシ"
        };
        private static string[] Dan =
        {
  	        "新人","９級","８級","７級","６級","５級","４級","３級","２級","１級",
	        "初段","二段","三段","四段","五段","六段","七段","八段","九段","十段",
	        "RESERVED..."
        };
        private static string[] Ba =
        {
            "東1", "東2", "東3", "東4",
            "南1", "南2", "南3", "南4",
            "西1", "西2", "西3", "西4",
            "北1", "北2", "北3", "北4"
        };
        private static string[] ManGan =
        {
            ""," 満貫"," 跳満"," 倍満"," 三倍満"," 役満"
        };
        private static string[] Yaku =
        {
            //// 一飜
	        "門前清自摸和","立直","一発","槍槓","嶺上開花",
	        "海底摸月","河底撈魚","平和","断幺九","一盃口",
	        "自風東","自風南","自風西","自風北",
	        "場風東","場風南","場風西","場風北",
	        "役牌白","役牌發","役牌中",
	        //// 二飜
	        "両立直","七対子","混全帯幺九","一気通貫","三色同順",
	        "三色同刻","三槓子","対々和","三暗刻","小三元","混老頭",
	        //// 三飜
	        "二盃口","純全帯幺九","混一色",
	        //// 六飜
	        "清一色",
	        //// 満貫
	        "人和",
	        //// 役満
	        "天和","地和","大三元","四暗刻","四暗刻単騎","字一色",
	        "緑一色","清老頭","九蓮宝燈","純正九蓮宝燈","国士無双",
	        "国士無双１３面","大四喜","小四喜","四槓子",
	        //// 懸賞役
	        "ドラ","裏ドラ","赤ドラ"
        };
        static bool[] flagReach = new bool[4];
        static bool flagRPrint = false;
        static string splitLine = "  ----------------------------------------------";
        static public void processMjlog(string dirName, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            Console.WriteLine(fileName);
            //try
            //{
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                using (GZipStream gzStream = new GZipStream(fs, CompressionMode.Decompress))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.PreserveWhitespace = true;
                    xmlDoc.Load(gzStream);
                    XmlNode root = xmlDoc.DocumentElement;

                    XmlNode go = root.SelectSingleNode("GO");
                    int iTableType = int.Parse(go.Attributes["type"].Value);
                    string tableType = ((iTableType & 0x0008) != 0) ? "東南戦: " : "東風戦: ";
                    tableType += TableLvl[(iTableType & 0x0020) >> 4 | (iTableType & 0x0080) >> 7];
                    if ((iTableType & 0x0010) != 0) tableType += "三";
                    tableType += TableType[(iTableType & 0x06) >> 1];
                    if ((iTableType & 0x0040) != 0) tableType += "速";
                    string playTime = fileName.Substring(0, 4) + "-" + fileName.Substring(4, 2) + "-" + fileName.Substring(6, 2) + " " + fileName.Substring(8, 2) + ":**:**";
                    Console.WriteLine("==== " + tableType + " " + playTime + " ====");

                    XmlNode players = root.SelectSingleNode("UN");
                    string[] playerName = new string[4];
                    string[] playerDan = players.Attributes["dan"].Value.Split(',');
                    string[] playerRate = players.Attributes["rate"].Value.Split(',');
                    string[] playerSx = players.Attributes["sx"].Value.Split(',');
                    //Console.Write("  ");
                    for (int i = 0; i < 4; i++)
                    {
                        string utf8EncStr = players.Attributes["n" + i.ToString()].Value;
                        string[] utf8Vals = utf8EncStr.Split('%');
                        byte[] chrArr = new byte[utf8Vals.Length-1];
                        for (int j = 1; j < utf8Vals.Length; j++)
                        {
                            chrArr[j-1] = (byte)Convert.ToInt32(utf8Vals[j], 16);
                        }
                        playerName[i] = new UTF8Encoding().GetString(chrArr);
                        playerDan[i] = Dan[int.Parse(playerDan[i])];
                        Console.Write("  [" + i.ToString() + "](" + playerDan[i] + " R" + playerRate[i] + ") " + playerName[i] + "(" + playerSx[i] + ")");
                    }
                    Console.WriteLine("");

                    XmlNode node = players.NextSibling;

                    while (node != null)
                    {
                        switch (node.Name)
                        {
                        case "TAIKYOKU":
                            string oya = node.Attributes["oya"].Value;
                            //Console.WriteLine("  起家:" + oya);
                            Console.WriteLine("\t\t[0]\t[1]\t[2]\t[3]");
                            break;
                        case "INIT":
                            Console.WriteLine(splitLine);
                            string[] seed = node.Attributes["seed"].Value.Split(',');
                            Console.Write("  " + Ba[int.Parse(seed[0])] + "," + seed[1] + "(" + seed[2] + ")\t");
                            string[] ten = node.Attributes["ten"].Value.Split(',');
                            Console.WriteLine(ten[0] + "00\t" + ten[1] + "00\t" + ten[2] + "00\t" + ten[3] + "00");
                            flagReach[0] = flagReach[1] = flagReach[2] = flagReach[3] = false;
                            flagRPrint = false;
                            break;
                        //case "T":case "D":case "U":case "E":case "V":case "F":case "W":case "G":
                        case "N":case "DORA":case "BYE":case "UN":
                            break;
                        case "REACH":
                            if (node.Attributes["step"].Value == "2")
                            {
                                flagReach[int.Parse(node.Attributes["who"].Value)] = true;
                            }
                            break;
                        case "AGARI":
                            ten = node.Attributes["ten"].Value.Split(',');
                            string fu = ten[0]; string soten = ten[1]; string man = ManGan[int.Parse(ten[2])];
                            int fan = 0;
                            string yaku = "";
                            string[] nYaku = node.Attributes["yaku"].Value.Split(',');
                            for (int j = 0; j < nYaku.Length; j += 2)
                            {
                                yaku += Yaku[int.Parse(nYaku[j])] + "(" + nYaku[j + 1] + ") ";
                                fan += int.Parse(nYaku[j + 1]);
                            }
                            yaku += fu + "符" + fan.ToString() + "飜" + man + "(" + soten + ")";

                            // Reach
                            PrintReach();

                            // ten
                            PrintTen(node, yaku);

                            // final result
                            PrintResult(node);

                            break;
                        case "RYUUKYOKU":
                            PrintReach();
                            string type = "流局";
                            bool[] fhai = new bool[4];
                            int flag = -1;
                            for (int k = 0; k < 4; k++)
                            {
                                if (node.Attributes["hai" + k.ToString()] != null)
                                {
                                    fhai[k] = true;
                                    flag = k;
                                }
                                else
                                {
                                    fhai[k] = false;
                                }
                            }
                            if (node.Attributes["type"] == null)
                            {
                                if (flag == -1)
                                {
                                    type += "(ノーテン)";
                                }
                            }
                            else switch (node.Attributes["type"].Value)
                            {
                            case "yao9":
                                type = "九種九牌[" + flag.ToString() + "]";
                                break;
                            default:
                                throw(new IOException());
                                break;
                            }
                            PrintTen(node, type);
                            PrintResult(node);
                            break;
                        default:
                            if (((node.Name[0] >= 'D' && node.Name[0] <= 'G') || (node.Name[0] >= 'T' && node.Name[0] <= 'W')) && (node.Name[1] >= '0' && node.Name[1] <='9'))
                            {
                                ;
                            }
                            else
                            {
                                throw(new IOException());
                            }
                            break;
                        }
                        node = node.NextSibling;
                    }

                }
            //}
            //catch (Exception ex)
            //{
            //}
        }

        static private void PrintReach()
        {
            if (flagRPrint == false)
            {
                Console.Write("\t");
                for (int j = 0; j < 4; j++)
                {
                    Console.Write("\t" + (flagReach[j] ? "立直" : ""));
                }
                Console.WriteLine();
                flagRPrint = true;
            }
        }

        static private void PrintTen(XmlNode node, string yaku)
        {
            string[] sc = node.Attributes["sc"].Value.Split(',');
            Console.Write("\t");
            for (int j = 0; j < 4; j++)
            {
                string shiten = sc[j * 2 + 1];
                if (shiten[0] == '0')
                {
                    shiten = "";
                }
                else if (shiten[0] != '-')
                {
                    shiten = "+" + shiten + "00";
                }
                else
                {
                    shiten = shiten + "00";
                }
                Console.Write("\t" + shiten);
            }
            Console.WriteLine("\t" + yaku);
        }

        static private void PrintResult(XmlNode node)
        {
            if (node.Attributes["owari"] != null)
            {
                string[] fnlTen = node.Attributes["owari"].Value.Split(',');
                Console.WriteLine(splitLine);
                Console.Write("  結果\t");
                for (int j = 0; j < 4; j++)
                {
                    Console.Write("\t" + fnlTen[j * 2] + "00");
                }
                Console.WriteLine();
                Console.Write("\t");
                for (int j = 0; j < 4; j++)
                {
                    string shiten = fnlTen[j * 2 + 1];
                    if (shiten[0] != '0' && shiten[0] != '-')
                    {
                        shiten = "+" + shiten;
                    }
                    Console.Write("\t" + shiten);
                }
                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}

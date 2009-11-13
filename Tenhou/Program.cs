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
            string dirPath = null;
            bool verbose = false;
            foreach (string arg in args)
            {
                switch (arg)
                {
                case "-verbose":
                    verbose = true;
                    break;
                default:
                    dirPath = arg;
                    break;
                }
            }
            if (dirPath == null)
            {
                return;
            }
            string[] files = Directory.GetFiles(dirPath, "*.mjlog");
            TenhouAna tenhouAna = new TenhouAna();
            foreach (string filePath in files)
            {
                tenhouAna.processMjlog(filePath, verbose);
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
        private static string[] Reach =
        {
            "",
            "立直",
            "副露"
        };
        private static string[] Hai =
        {
            "1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m",
            "1p", "2p", "3p", "4p", "5p", "6p", "7p", "8p", "9p",
            "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s",
            "東", "南", "西", "北", "白", "発", "中"
        };
        private static string splitLine = "  ----------------------------------------------";
        bool[] flagReach = new bool[4];
        int[] nReachJun = new int[4];
        int[] nFuro = new int[4];
        int[] nFuroJun = new int[4];
        bool flagRPrint = false;
        bool flagSyantenP = false;
        
        private bool flagCheckHai = false;
        public void processMjlog(string filePath, bool verbose)
        {
            flagCheckHai = verbose;
            string fileName = Path.GetFileName(filePath);
            Console.WriteLine(fileName);

            //bool[][] tehai136 = new bool[136];
            int[][] tehai34 = new int[4][];
            int[][] nhai34 = new int[4][];
            int[] lastAction = new int[4];
            int[] jun = new int[4];
            int[] lastTsumo = new int[4];
            int lastKiri = -1;
            for (int i = 0; i < 4; i++)
            {
                tehai34[i] = new int[34];
                nhai34[i] = new int[34];
            }
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

                int me = int.Parse(fileName.Substring(35, 1));

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
                    byte[] chrArr = new byte[utf8Vals.Length - 1];
                    for (int j = 1; j < utf8Vals.Length; j++)
                    {
                        chrArr[j - 1] = (byte)Convert.ToInt32(utf8Vals[j], 16);
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
                        flagRPrint = false;
                        flagSyantenP = false;
                        //for (int i = 0; i < 136; i++)
                        //{
                        //    tehai136[i] = false;
                        //}
                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 34; j++)
                            {
                                tehai34[i][j] = 0;
                                nhai34[i][j] = 0;
                            }
                            lastAction[i] = -1;
                            jun[i] = 0;
                            lastTsumo[i] = -1;
                            flagReach[i] = false;
                            nReachJun[i] = -1;
                            nFuro[i] = 0;
                            nFuroJun[i] = -1;
                        }
                        lastKiri = -1;
                        Console.WriteLine(splitLine);
                        string[] seed = node.Attributes["seed"].Value.Split(',');
                        Console.Write("  " + Ba[int.Parse(seed[0])] + "," + seed[1] + "(" + seed[2] + ")\t");
                        string[] ten = node.Attributes["ten"].Value.Split(',');
                        Console.WriteLine(ten[0] + "00\t" + ten[1] + "00\t" + ten[2] + "00\t" + ten[3] + "00");
                        for (int i = 0; i < 4; i++)
                        {
                            nhai34[i][int.Parse(seed[5]) / 4]++; //Dora
                            string[] strHaipai = node.Attributes["hai" + i].Value.Split(',');
                            foreach (string pai in strHaipai)
                            {
                                nhai34[i][int.Parse(pai) / 4]++;
                                tehai34[i][int.Parse(pai) / 4]++;
                            }
                        }
                        break;
                    //case "T":case "D":case "U":case "E":case "V":case "F":case "W":case "G":
                    case "N":
                        int who = int.Parse(node.Attributes["who"].Value);
                        int m = int.Parse(node.Attributes["m"].Value);
                        int fromWho = (m & 0x0003 + me) % 4;
                        bool flagFuro = true;
                        if (who == me && flagCheckHai)
                        {
                            Console.Write("[" + me + "]");
                        }
                        if ((m & 0x0004) != 0)
                        {
                            lastAction[who] = 0;
                            int type6 = (m & 0xfc00) >> 10;
                            int kuiHai = type6 % 3;
                            type6 /= 3;
                            int startHai = type6 / 7 * 9 + type6 % 7;

                            for (int i = 0; i < 3; i++)
                            {
                                if (i != kuiHai)
                                {
                                    tehai34[who][startHai + i]--;
                                }
                            }
                            if (who == me && flagCheckHai)
                            {
                                Console.Write("チー[" + fromWho + "](" + Hai[startHai + kuiHai] + "-" +
                                Hai[startHai] + Hai[startHai + 1] + Hai[startHai + 2] + "):");
                                printTehai(tehai34[me], -1);
                                Console.Write(" ");
                                SyantenCheck(tehai34[me], nhai34[me]);
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                if (k != who)
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        if (i != kuiHai)
                                        {
                                            nhai34[k][startHai + i]++;
                                        }
                                    }
                                }
                            }
                        }
                        else if ((m & 0x0008) != 0)
                        {
                            lastAction[who] = 0;
                            int type7 = (m & 0xfe00) >> 9;
                            type7 /= 3;
                            tehai34[who][type7] -= 2;
                            if (who == me && flagCheckHai)
                            {
                                Console.Write("ポン[" + fromWho + "](" + Hai[type7] + "):");
                                printTehai(tehai34[me], -1);
                                Console.Write(" ");
                                SyantenCheck(tehai34[me], nhai34[me]);
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                if (k != who)
                                {
                                    nhai34[k][type7] += 2;
                                }
                            }
                        }
                        else if ((m & 0x003c) == 0)
                        {
                            int type8 = (m & 0xff00) >> 8;
                            type8 /= 4;

                            if (lastAction[who] == 0)
                            {
                                tehai34[who][type8] -= 4;
                                if (who == me && flagCheckHai)
                                {
                                    Console.Write("暗槓(" + Hai[type8] + "):");
                                }
                                flagFuro = false;
                            }
                            else
                            {
                                tehai34[who][type8] -= 3;
                                if (who == me && flagCheckHai)
                                {
                                    Console.Write("明槓[" + fromWho + "](" + Hai[type8] + "):");
                                }
                            }
                            if (who == me && flagCheckHai)
                            {                                        //printTehai(tehai34, -1);
                                Console.WriteLine("");
                                //SyantenCheck(tehai34, nhai34);
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                nhai34[k][type8] = 4;
                            }
                            lastAction[who] = 2;
                        }
                        else if ((m & 0x0010) != 0)
                        {
                            lastAction[who] = 2;
                            int type7 = (m & 0xfe00) >> 9;
                            type7 /= 3;
                            tehai34[who][type7] -= 1;
                            flagFuro = false;
                            if (who == me && flagCheckHai)
                            {
                                Console.Write("加槓(" + Hai[type7] + "):");
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                nhai34[k][type7] = 4;
                            }
                        }
                        if (flagFuro == true)
                        {
                            if (nFuro[who] == 0)
                            {
                                nFuroJun[who] = jun[who];
                            }
                            nFuro[who]++;
                        }
                        break;
                    case "DORA":
                        int hai = int.Parse(node.Attributes["hai"].Value) / 4;
                        for (int k = 0; k < 4; k++)
                        {
                            nhai34[k][hai]++;
                        }
                        break;
                    case "BYE":
                    case "UN":
                        break;
                    case "REACH":
                        if (node.Attributes["step"].Value == "2")
                        {
                            who = int.Parse(node.Attributes["who"].Value);
                            flagReach[who] = true;
                            nReachJun[who] = jun[who];
                            if (flagCheckHai)
                            {
                                Console.Write("リーチ");
                                if (who != me)
                                {
                                    Console.Write("[" + who + "]");
                                }
                                Console.WriteLine("");
                            }
                        }
                        break;
                    case "AGARI":
                        ten = node.Attributes["ten"].Value.Split(',');
                        string fu = ten[0]; string soten = ten[1]; string man = ManGan[int.Parse(ten[2])];
                        int fan = 0;
                        string yaku = "";

                        who = int.Parse(node.Attributes["who"].Value);
                        fromWho = int.Parse(node.Attributes["fromWho"].Value);
                        if (who == fromWho)
                        {
                            yaku = "[" + who + "]自摸  ";
                        }
                        else
                        {
                            yaku = "[" + who + "]栄[" + fromWho + "] ";
                        }

                        if (node.Attributes["yaku"] != null)
                        {
                            string[] nYaku = node.Attributes["yaku"].Value.Split(',');
                            for (int j = 0; j < nYaku.Length; j += 2)
                            {
                                yaku += Yaku[int.Parse(nYaku[j])] + "(" + nYaku[j + 1] + ") ";
                                fan += int.Parse(nYaku[j + 1]);
                            }
                            yaku += fu + "符" + fan.ToString() + "飜" + man + "(" + soten + ")";
                        }
                        else
                        {
                            string[] nYaku = node.Attributes["yakuman"].Value.Split(',');
                            for (int j = 0; j < nYaku.Length; j++)
                            {
                                yaku += Yaku[int.Parse(nYaku[j])] + " ";
                            }
                            yaku += "(" + soten + ")";
                        }
                        // Reach
                        PrintReach();
                        // Syanten
                        PrintSyanten(node, who, fromWho, tehai34, nhai34, lastTsumo);
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
                        else
                        {
                            switch (node.Attributes["type"].Value)
                            {
                            case "yao9":
                                type = "九種九牌[" + flag.ToString() + "]";
                                break;
                            case "kaze4":
                                type = "四風連打";
                                break;
                            default:
                                throw (new IOException());
                                break;
                            }
                        }
                        PrintTen(node, type);
                        PrintSyanten(node, -1, -1, tehai34, nhai34, lastTsumo);
                        PrintResult(node);
                        break;
                    default:
                        if (node.Name[0] >= 'T' && node.Name[0] <= 'W')
                        {
                            who = node.Name[0] - 'T';
                            if (lastAction[who] != 2)
                            {
                                jun[who]++;
                            }
                            lastAction[who] = 0;
                            int pai = int.Parse(node.Name.Substring(1)) / 4;
                            lastTsumo[who] = pai;
                            if (who == me && flagCheckHai)
                            {
                                Console.Write("[" + me + "]:{" + jun[me] + "}");
                                printTehai(tehai34[me], pai);
                            }
                            tehai34[who][pai]++;
                            nhai34[who][pai]++;
                            if (who == me && flagCheckHai)
                            {
                                Console.Write(" ");
                                SyantenCheck(tehai34[me], nhai34[me]);
                            }
                        }
                        else if (node.Name[0] >= 'D' && node.Name[0] <= 'G')
                        {
                            who = node.Name[0] - 'D';
                            lastAction[who] = 1;
                            int pai = int.Parse(node.Name.Substring(1)) / 4;
                            tehai34[who][pai]--;
                            if (who == me && flagCheckHai)
                            {
                                Console.WriteLine("-" + Hai[pai]);
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                if (k != who)
                                {
                                    nhai34[k][pai]++;
                                }
                            }
                            lastKiri = who;
                        }
                        else
                        {
                            throw (new IOException());
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

        private void SyantenCheck(int[] tehai34, int[] nhai34)
        {
            Syanten syanten = new Syanten();
            int syanten_org = syanten.getSyanTen(tehai34, nhai34);
            Console.WriteLine(syanten_org);
            if (syanten_org < 0)
            {
                return;
            }
            List<SyantenMachi> v = new List<SyantenMachi>();
            {
                int syanten_bound = syanten_org;
                for (int i = 0; i < 34; ++i)
                {
                    if (tehai34[i] == 0)
                    {
                        continue;
                    }
                    tehai34[i]--; // 打
                    //v[i]=c_enum_machi34(c);
                    SyantenMachi smachi = new SyantenMachi();
                    smachi.kiri = i;

                    for (int j = 0; j < 34; ++j)
                    {
                        if (i == j || nhai34[j] >= 4)
                        {
                            continue;
                        }
                        tehai34[j]++; // 摸
                        nhai34[j]++;

                        if (syanten.getSyanTen(tehai34, nhai34) < syanten_bound)
                        {
                            smachi.machihai.Insert(0, j);
                        }
                        tehai34[j]--;
                        nhai34[j]--;
                    }
                    if (smachi.machihai.Count > 0)
                    {
                        for (int j = 0; j < smachi.machihai.Count; j++)
                        {
                            smachi.num += 4 - nhai34[smachi.machihai[j]];
                        }
                        smachi.machihai.Reverse();
                        v.Insert(0, smachi);
                    }
                    tehai34[i]++;
                }
                v.Sort();
                foreach (SyantenMachi sm in v)
                {
                    Console.Write("    -" + Hai[sm.kiri] + ":+");
                    foreach (int p in sm.machihai)
                    {
                        Console.Write(Hai[p]);
                    }
                    Console.WriteLine(" : " + sm.num);
                }
            }
        }

        private void printTehai(int[] tehai, int tsumo)
        {
            for (int i = 0; i < 34; i++)
            {
                for (int j = 0; j < tehai[i]; j++)
                {
                    Console.Write(Hai[i]);
                }
            }
            if (tsumo >= 0)
            {
                Console.Write("+" + Hai[tsumo]);
            }
        }

        private void PrintReach()
        {
            if (flagRPrint == false)
            {
                Console.Write("\t");
                for (int j = 0; j < 4; j++)
                {
                    Console.Write("\t");
                    if (flagReach[j])
                    {
                        Console.Write("立{" + nReachJun[j] + "}");
                    }
                    else if (nFuro[j] > 0)
                    {
                        Console.Write(nFuro[j] + "露{" + nFuroJun[j] + "}");
                    }
                }
                Console.WriteLine();
                flagRPrint = true;
            }
        }
        private void PrintSyanten(XmlNode node, int who, int fromWho, int[][] tehai34, int[][] nhai34, int[] lastTsumo)
        {
            if (flagSyantenP == true)
            {
                return;
            }
            flagSyantenP = true;
            Syanten syanten = new Syanten();
            Console.Write("\t");
            if (who == fromWho && who >= 0)
            {
                tehai34[who][lastTsumo[who]]--;
            }
            for (int j = 0; j < 4; j++)
            {
                Console.Write("\t");
                int org = 0;
                if (j != who)
                {
                    org = syanten.getSyanTen(tehai34[j], nhai34[j]);
                }
                if (org != 0)
                {
                    Console.Write(org + "向聴");
                }
                else
                {
                    SyantenMachi smachi = new SyantenMachi();
                    for (int k = 0; k < 34; ++k)
                    {
                        if (tehai34[j][k] >= 4)
                        {
                            continue;
                        }
                        tehai34[j][k]++; // 摸
                        nhai34[j][k]++;

                        if (syanten.getSyanTen(tehai34[j], nhai34[j]) < 0)
                        {
                            smachi.machihai.Insert(0, k);
                        }
                        tehai34[j][k]--;
                        nhai34[j][k]--;
                    }
                    int numMachi = 0;
                    int numHai = 0;
                    foreach (int hai in smachi.machihai)
                    {
                        numMachi++;
                        numHai += 4 - nhai34[j][hai];
                    }
                    if (j == who)
                    {
                        Console.Write("[" + numMachi + "," + (numHai + 1) + "]");
                    }
                    else
                    {
                        Console.Write("(" + numMachi + "," + numHai + ")");
                    }
                }

            }
            Console.WriteLine();
        }
        private void PrintTen(XmlNode node, string yaku)
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

        private void PrintResult(XmlNode node)
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

    class SyantenMachi : IComparable<SyantenMachi>
    {
        public List<int> machihai = new List<int>();
        public int num = 0;
        public int kiri = -1;

        public int CompareTo(SyantenMachi right)
        {
            return right.num - num;
        }
    }
}

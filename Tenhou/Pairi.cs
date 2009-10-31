using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tenhou
{
    class Pairi
    {
    }

    class Syanten
    {
        private int[] tehai;
        private int[] nhai;
        
        private int numhai = 0;
        private int min_syanten = 14;
        private int f_n4 = 0;
        private int n_mentsu = 0;
        private int n_toitsu = 0;
        private int n_tatsu = 0;
        private int f_koritsu = 0;
        private int n_jidahai = 0;
        private int n_eval = 0;
        public Syanten (){}

        private void Init()
        {
            numhai = 0;
            min_syanten = 14;
            f_n4 = 0;
            n_mentsu = 0;
            n_toitsu = 0;
            n_tatsu = 0;
            f_koritsu = 0;
            n_jidahai = 0;
            n_eval = 0;
        }

        public int getSyanTen(int[] te_hai, int[] n_hai)
        {
            Init();
            tehai = (int[])te_hai.Clone();
            nhai = (int[])n_hai.Clone();
            numhai = CountHai();
            n_mentsu = (14 - numhai) / 3;
            if (numhai >= 13)
            {
                scanChiitoiKokushi(); // １３枚より下の手牌は評価できない 
            }
            removeJihai();
            scanNormal();
            return min_syanten;
        }

        private void Run(int depth)
        {
            // ネストは高々１４回 
            n_eval++;
            if (min_syanten == -1)
            {
                return; // 和了は１つ見つければよい 
            }

            while (depth < 27 && tehai[depth] == 0)
            {
                depth++;
            }// skip

            if (depth >= 27)
            {
                updateResult();
                return;
            }

            int i = depth;
            if (i > 8) i -= 9;
            if (i > 8) i -= 9; // mod_9_in_27
            switch (tehai[depth])
            {
            case 4:
                // 暗刻＋順子|搭子|孤立
                i_anko(depth);
                if (i < 7 && tehai[depth + 2] !=0)
                {
                    if (tehai[depth + 1] != 0)
                    {
                        i_syuntsu(depth);
                        Run(depth + 1);
                        d_syuntsu(depth); // 順子
                    }
                    i_tatsu_k(depth);
                    Run(depth + 1);
                    d_tatsu_k(depth); // 嵌張搭子 
                }
                if (i < 8 && tehai[depth + 1] != 0)
                {
                    i_tatsu_r(depth); Run(depth + 1); d_tatsu_r(depth); // 両面搭子 
                }
                i_koritsu(depth);
                Run(depth + 1);
                d_koritsu(depth); // 孤立 

                d_anko(depth);
                // 対子＋順子系 // 孤立が出てるか？ // 対子＋対子は不可 
                i_toitsu(depth);
                if (i < 7 && tehai[depth + 2] != 0)
                {
                    if (tehai[depth + 1] != 0)
                    {
                        i_syuntsu(depth);
                        Run(depth);
                        d_syuntsu(depth); // 順子＋他 
                    }
                    i_tatsu_k(depth);
                    Run(depth + 1);
                    d_tatsu_k(depth); // 搭子は２つ以上取る必要は無い -> 対子２つでも同じ 
                }
                if (i < 8 && tehai[depth + 1] != 0)
                {
                    i_tatsu_r(depth);
                    Run(depth + 1);
                    d_tatsu_r(depth);
                }
                d_toitsu(depth);
                break;
            case 3:
                // 暗刻のみ 
                i_anko(depth);
                Run(depth + 1);
                d_anko(depth);

                // 対子＋順子|搭子 
                i_toitsu(depth);
                if (i < 7 && tehai[depth + 1] != 0 && tehai[depth + 2] != 0)
                {
                    i_syuntsu(depth);
                    Run(depth + 1);
                    d_syuntsu(depth); // 順子 
                }
                else
                { // 順子が取れれば搭子はその上でよい 
                    if (i < 7 && tehai[depth + 2] != 0)
                    {
                        i_tatsu_k(depth);
                        Run(depth + 1);
                        d_tatsu_k(depth); // 嵌張搭子は２つ以上取る必要は無い -> 対子２つでも同じ 
                    }
                    if (i < 8 && tehai[depth + 1] != 0)
                    {
                        i_tatsu_r(depth);
                        Run(depth + 1);
                        d_tatsu_r(depth); // 両面搭子 
                    }
                }
                d_toitsu(depth);
                // 順子系 
                if (i < 7 && tehai[depth + 2] >= 2 && tehai[depth + 1] >= 2)
                {
                    i_syuntsu(depth);
                    i_syuntsu(depth);
                    Run(depth);
                    d_syuntsu(depth);
                    d_syuntsu(depth); // 順子＋他 
                }
                break;
            case 2:
                // 対子のみ 
                i_toitsu(depth);
                Run(depth + 1);
                d_toitsu(depth);
                // 順子系
                if (i < 7 && tehai[depth + 2] != 0 && tehai[depth + 1] != 0)
                {
                    i_syuntsu(depth);
                    Run(depth);
                    d_syuntsu(depth); // 順子＋他 
                }
                break;
            case 1:
                // 孤立牌は２つ以上取る必要は無い -> 対子のほうが向聴数は下がる -> ３枚 -> 対子＋孤立は対子から取る 
                // 孤立牌は合計８枚以上取る必要は無い 
                if (i < 6 && tehai[depth + 1] == 1 && tehai[depth + 2] != 0 && tehai[depth + 3] != 4)
                { // 延べ単 
                    i_syuntsu(depth);
                    Run(depth + 2);
                    d_syuntsu(depth); // 順子＋他 
                }
                else
                {
                    //				if (n_koritsu<8) i_koritsu(depth), Run(depth+1), d_koritsu(depth);
                    i_koritsu(depth);
                    Run(depth + 1);
                    d_koritsu(depth);
                    // 順子系
                    if (i < 7 && tehai[depth + 2] != 0)
                    {
                        if (tehai[depth + 1] != 0)
                        {
                            i_syuntsu(depth);
                            Run(depth + 1);
                            d_syuntsu(depth); // 順子＋他 
                        }
                        i_tatsu_k(depth);
                        Run(depth + 1);
                        d_tatsu_k(depth); // 搭子は２つ以上取る必要は無い -> 対子２つでも同じ 
                    }
                    if (i < 8 && tehai[depth + 1] != 0)
                    {
                        i_tatsu_r(depth);
                        Run(depth + 1);
                        d_tatsu_r(depth);
                    }
                }
                break;
            }
        }

        private void updateResult()
        {

            int ret_syanten = 8 - n_mentsu * 2 - n_tatsu - n_toitsu;
            int n_mentsu_kouho = n_mentsu + n_tatsu;
            if (n_toitsu != 0)
            {
                n_mentsu_kouho += n_toitsu - 1;
            }
            else if (f_n4 != 0 && f_koritsu != 0)
            {
                if ((f_n4 | f_koritsu) == f_n4)
                {
                    ++ret_syanten; // 対子を作成できる孤立牌が無い
                }
            }
            if (n_mentsu_kouho > 4)
            {
                ret_syanten += (n_mentsu_kouho - 4);
            }
            if (ret_syanten != -1 && ret_syanten < n_jidahai)
            {
                ret_syanten = n_jidahai;
            }
            if (ret_syanten < min_syanten)
            {
                min_syanten = ret_syanten;
            }
        }

        private void scanNormal()
        {
            for (int i = 0; i < 27; i++)
            {
                if (nhai[i] == 4)
                {
                    f_n4 |= 1 << i;
                }
            }
            Run(0);
        }

        private void removeJihai()
        {
		    int j_n4=0; // 7bitを字牌で使用 
            int j_koritsu = 0; // 孤立牌 
            for (int i = 27; i < 34; ++i)
            {
                switch (tehai[i])
                {
                case 4:
                    n_mentsu++;
                    j_koritsu |= (1 << (i - 27));
                    n_jidahai++;
                    break;
                case 3:
                    n_mentsu++;
                    break;
                case 2:
                    n_toitsu++;
                    break;
                case 1:
                    j_koritsu |= (1 << (i - 27));
                    break;
                }
                if (nhai[i] == 4)
                {
                    j_n4 |= (1 << (i - 27));
                }
            }
            if (n_jidahai !=0 && (numhai % 3) == 2)
            {
                n_jidahai--;
            }

            if (j_koritsu != 0)
            { // 孤立牌が存在する
                f_koritsu |= (1 << 27);
                if ((j_n4 | j_koritsu) == j_n4)
                {
                    f_n4 |= (1 << 27); // 対子を作成できる孤立牌が無い
                }
            }
        }


        private void scanChiitoiKokushi()
        {
            int n13 = // 幺九牌の対子候補の数
                (tehai[0] >= 2 ? 1 : 0) + (tehai[8] >= 2 ? 1 : 0) +
                (tehai[9] >= 2 ? 1 : 0) + (tehai[17] >= 2 ? 1 : 0) +
                (tehai[18] >= 2 ? 1 : 0) + (tehai[26] >= 2 ? 1 : 0) +
                (tehai[27] >= 2 ? 1 : 0) + (tehai[28] >= 2 ? 1 : 0) + (tehai[29] >= 2 ? 1 : 0) + (tehai[30] >= 2 ? 1 : 0) +
                (tehai[31] >= 2 ? 1 : 0) + (tehai[32] >= 2 ? 1 : 0) + (tehai[33] >= 2 ? 1 : 0);
            int m13 = // 幺九牌の種類数
                (tehai[0] != 0 ? 1 : 0) + (tehai[8] != 0 ? 1 : 0) +
                (tehai[9] != 0 ? 1 : 0) + (tehai[17] != 0 ? 1 : 0) +
                (tehai[18] != 0 ? 1 : 0) + (tehai[26] != 0 ? 1 : 0) +
                (tehai[27] != 0 ? 1 : 0) + (tehai[28] != 0 ? 1 : 0) + (tehai[29] != 0 ? 1 : 0) + (tehai[30] != 0 ? 1 : 0) +
                (tehai[31] != 0 ? 1 : 0) + (tehai[32] != 0 ? 1 : 0) + (tehai[33] != 0 ? 1 : 0);
            int n7 = n13 + // 対子候補の数
                (tehai[1] >= 2 ? 1 : 0) + (tehai[2] >= 2 ? 1 : 0) + (tehai[3] >= 2 ? 1 : 0) + (tehai[4] >= 2 ? 1 : 0) +
                (tehai[5] >= 2 ? 1 : 0) + (tehai[6] >= 2 ? 1 : 0) + (tehai[7] >= 2 ? 1 : 0) +
                (tehai[10] >= 2 ? 1 : 0) + (tehai[11] >= 2 ? 1 : 0) + (tehai[12] >= 2 ? 1 : 0) + (tehai[13] >= 2 ? 1 : 0) +
                (tehai[14] >= 2 ? 1 : 0) + (tehai[15] >= 2 ? 1 : 0) + (tehai[16] >= 2 ? 1 : 0) +
                (tehai[19] >= 2 ? 1 : 0) + (tehai[20] >= 2 ? 1 : 0) + (tehai[21] >= 2 ? 1 : 0) + (tehai[22] >= 2 ? 1 : 0) +
                (tehai[23] >= 2 ? 1 : 0) + (tehai[24] >= 2 ? 1 : 0) + (tehai[25] >= 2 ? 1 : 0);
            int m7 = m13 + // 牌の種類数
                (tehai[1] != 0 ? 1 : 0) + (tehai[2] != 0 ? 1 : 0) + (tehai[3] != 0 ? 1 : 0) + (tehai[4] != 0 ? 1 : 0) +
                (tehai[5] != 0 ? 1 : 0) + (tehai[6] != 0 ? 1 : 0) + (tehai[7] != 0 ? 1 : 0) +
                (tehai[10] != 0 ? 1 : 0) + (tehai[11] != 0 ? 1 : 0) + (tehai[12] != 0 ? 1 : 0) + (tehai[13] != 0 ? 1 : 0) +
                (tehai[14] != 0 ? 1 : 0) + (tehai[15] != 0 ? 1 : 0) + (tehai[16] != 0 ? 1 : 0) +
                (tehai[19] != 0 ? 1 : 0) + (tehai[20] != 0 ? 1 : 0) + (tehai[21] != 0 ? 1 : 0) + (tehai[22] != 0 ? 1 : 0) +
                (tehai[23] != 0 ? 1 : 0) + (tehai[24] != 0 ? 1 : 0) + (tehai[25] != 0 ? 1 : 0);
            { // 七対子
                int ret_syanten = 6 - n7 + (m7 < 7 ? 7 - m7 : 0);
                if (ret_syanten < min_syanten) min_syanten = ret_syanten;
            }
            { // 国士無双
                int ret_syanten = 13 - m13 - (n13 != 0 ? 1 : 0);
                if (ret_syanten < min_syanten) min_syanten = ret_syanten;
            }

        }

        private int CountHai()
        {
            return tehai[0] + tehai[1] + tehai[2] + tehai[3] + tehai[4] + tehai[5] + tehai[6] + tehai[7] + tehai[8] +
            tehai[9] + tehai[10] + tehai[11] + tehai[12] + tehai[13] + tehai[14] + tehai[15] + tehai[16] + tehai[17] +
            tehai[18] + tehai[19] + tehai[20] + tehai[21] + tehai[22] + tehai[23] + tehai[24] + tehai[25] + tehai[26] +
            tehai[27] + tehai[28] + tehai[29] + tehai[30] + tehai[31] + tehai[32] + tehai[33];
        }

        private void i_anko(int k) { tehai[k] -= 3; n_mentsu++; }
        private void d_anko(int k) { tehai[k] += 3; n_mentsu--; }
        private void i_toitsu(int k) { tehai[k] -= 2; ++n_toitsu; }
        private void d_toitsu(int k) { tehai[k] += 2; --n_toitsu; }
        private void i_syuntsu(int k) { --tehai[k]; --tehai[k + 1]; --tehai[k + 2]; ++n_mentsu; }
        private void d_syuntsu(int k) { ++tehai[k]; ++tehai[k + 1]; ++tehai[k + 2]; --n_mentsu; }
        private void i_tatsu_r(int k) { --tehai[k]; --tehai[k + 1]; ++n_tatsu; }
        private void d_tatsu_r(int k) { ++tehai[k]; ++tehai[k + 1]; --n_tatsu; }
        private void i_tatsu_k(int k) { --tehai[k]; --tehai[k + 2]; ++n_tatsu; }
        private void d_tatsu_k(int k) { ++tehai[k]; ++tehai[k + 2]; --n_tatsu; }
        private void i_koritsu(int k) { --tehai[k]; f_koritsu |= (1 << k); }
        private void d_koritsu(int k) { ++tehai[k]; f_koritsu &= ~(1 << k); }

    }
}

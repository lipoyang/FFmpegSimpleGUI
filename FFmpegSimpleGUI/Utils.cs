using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSimpleGUI
{
    // 便利クラス
    class Utils
    {
        // 文字列⇒整数 変換
        public static int StrToInt(string str, int defautValue)
        {
            int val;
            if(int.TryParse(str, out val)) {
                return val;
            } else {
                return defautValue;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO; // ファイル
using Newtonsoft.Json;  // JSON
using System.Collections.ObjectModel; // ObservableCollection

namespace FFmpegSimpleGUI
{
    // 変換オプション
    class FFmpegOption
    {
        // 入力パス
        public string InputPath = "";
        // 出力パス
        public string OutputPath = "";
        // フレームレート
        public bool   FrameRateOption = false;
        public string FrameRate = "30";
        // リサイズ
        public bool ResizeOption = false;
        public string Width    = "1280";
        public string Height   = "720";
        public bool WidthAuto  = false;
        public bool HeightAuto = false;
        // 音量オプション
        public bool VolumeOption = false;
        public string Volume     = "0";
        public bool   VolumeAuto = false;
        public bool   NoSound    = false;
        // 切り出しオプション
        public bool CutOption = false;
        public bool EndSelect = true;
        public string StartPos = "0";
        public string EndPos   = "0";
        public string Length   = "0";
        public bool FromFirst = false;
        public bool ToLast    = false;
        // ノイズ除去
        public bool NoiseOption = false;
        public string LmsDelay = "32";
        public string LmsOrder = "128";
        public string LmsLeak  = "0.0005";
        public string LmsMu    = ".5";
        // 追加オプション
        public bool AddOption = false;
        public string AdditionalOption = "";
        // コンソール表示
        public bool ShowConsole = false;
        // オプション表示
        public bool ShowOption = false;
        // 出力パス (連結)
        public string OutputPath2 = "";
        // リエンコードしない (連結)
        public bool NotReencode = false;

        // セーブ
        public bool Save(string path)
        {
            try {
                string json = 
                    JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
                return true;
            } catch {
                return false;
            }
        }

        // ロード
        public static FFmpegOption Load(string path)
        {
            try {
                string json = File.ReadAllText(path);
                FFmpegOption data = 
                    JsonConvert.DeserializeObject<FFmpegOption>(json);
                return data;
            } catch {
                return new FFmpegOption();
            }
        }

        // [変換] オプション文字列に変換
        public string GetConvertOption()
        {
            List<string> options = new List<string>();

            // 入力ファイル
            string strInputPath = InputPath.Trim();
            if(strInputPath.IndexOf(" ") >= 0) {
                strInputPath = "\"" + strInputPath + "\"";
            }
            strInputPath = "-i " + strInputPath;
            options.Add(strInputPath);

            // フレームレート
            string strFrameRate = "";
            if (FrameRateOption) {
                strFrameRate = "-r " + FrameRate;
            }
            options.Add(strFrameRate);

            // リサイズ
            string strResize = "";
            if (ResizeOption) {
                string width  = WidthAuto ?  "-1" : Width;
                string height = HeightAuto ? "-1" : Height;
                strResize = "-vf \"scale = " + width + ":" + height + "\"";
            }
            options.Add(strResize);

            // 音量レベル
            string strVolume = "";
            if (VolumeOption) {
                if (NoSound) {
                    strVolume = "-an";
                }
                else if (VolumeAuto) {
                    strVolume = "-af dynaudnorm";
                }
                else {
                    strVolume = "-af volume=" + Volume + "dB";
                }
            }
            options.Add(strVolume);

            // 切り出し
            string strCut = "";
            if (CutOption) {
                string start = FromFirst ? "0" : StartPos;
                string end   = ToLast ? "0" : EndPos;
                if (EndSelect) {
                    strCut = "-ss " + start + " -to " + end;
                } else {
                    strCut = "-ss " + start + " -t " + Length;
                }
            }
            options.Add(strCut);

            // ノイズ除去
            string strNoise = "";
            if (NoiseOption) {
                strNoise =
                    "-af \"asplit[a][b],[a]adelay=" +
                    LmsDelay + "S|" + LmsDelay + "S" +
                    "[a],[b][a]" +
                    "anlms=order=" + LmsOrder +
                    ":leakage=" + LmsLeak +
                    ":mu=" + LmsMu +
                    ":out_mode=o,dynaudnorm\"";
            }
            options.Add(strNoise);

            // 追加オプション
            string strAdditional = "";
            if (AddOption) {
                strAdditional = AdditionalOption;
            }
            options.Add(strAdditional);

            // 出力ファイル
            string strOutputPath = OutputPath.Trim();
            if(strOutputPath.IndexOf(" ") >= 0) {
                strOutputPath = "\"" + strOutputPath + "\"";
            }
            options.Add(strOutputPath);

            // コマンド文字列を返す
            string command = "";
            for(int i = 0; i < options.Count; i++)
            {
                if(options[i] != "") command += options[i] + " ";
            }
            return command;
        }

        // [連結] オプション文字列に変換
        // list: 入力ファイルリスト
        public string GetConcatOption(ObservableCollection<PathInfo> list)
        {
            // ファイルリストの作成
            const string LIST_FILE = "list.txt";
            try {
                using (var sw = new StreamWriter(LIST_FILE, false))//, Encoding.UTF8)) // TODO UTF8でよいか？
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var pathInfo = list[i];
                        string path = Path.Combine(pathInfo.DirName, pathInfo.FileName);
                        path.Replace('\\', '/');
                        path = path.Trim();
                        path = "'" + path + "'";

                        sw.WriteLine("file " + path);
                    }
                }
            } catch {
                return "";
            }

            // オプション文字列の生成
            string ffmpegOptoin = "-f concat -safe 0 -i " + LIST_FILE;

            if (this.NotReencode) {
                ffmpegOptoin += " -c copy";
            }

            ffmpegOptoin += " " + this.OutputPath2;

            return ffmpegOptoin;
        }
    }
}

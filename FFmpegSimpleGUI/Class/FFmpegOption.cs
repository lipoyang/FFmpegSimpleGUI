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
    // FFmpegのオプション
    class FFmpegOption
    {
        // [変換] 入力パス
        public string InputPath = "";
        // [変換] 出力パス
        public string OutputPath = "";
        // [変換] フレームレート
        public bool   FrameRateOption = false;
        public string FrameRate = "30";
        // [変換] リサイズ
        public bool ResizeOption = false;
        public string Width    = "1280";
        public string Height   = "720";
        public bool WidthAuto  = false;
        public bool HeightAuto = false;
        // [変換] 音量オプション
        public bool VolumeOption = false;
        public string Volume     = "0";
        public bool   VolumeAuto = false;
        public bool   NoSound    = false;
        // [変換] 切り出しオプション
        public bool CutOption = false;
        public bool EndSelect = true;
        public string StartPos = "0";
        public string EndPos   = "0";
        public string Length   = "0";
        public bool FromFirst = false;
        public bool ToLast    = false;
        // [変換] ノイズ除去
        public bool NoiseOption = false;
        public string LmsDelay = "32";
        public string LmsOrder = "128";
        public string LmsLeak  = "0.0005";
        public string LmsMu    = ".5";
        // [変換] 追加オプション
        public bool AddOption = false;
        public string AdditionalOption = "";
        // [変換] コマンドライン表示
        public bool ShowCommand = false;
        // [変換][連結] コンソール表示
        public bool ShowConsole = false;
        // [連結] 出力パス
        public string OutputPath2 = "";
        // [連結] リエンコードしない
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

            // 切り出し (開始位置)
            string strCut1 = "";
            if (CutOption) {
                string start = FromFirst ? "0" : StartPos;
                strCut1 = "-ss " + start;
            }
            options.Add(strCut1);

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

            // 切り出し (終了位置 or 長さ)
            string strCut2 = "";
            if (CutOption) {
                if (EndSelect) {
                    if (ToLast) {
                        strCut2 = "";
                    } else {
                        strCut2 = "-to " + EndPos;
                    }
                } else {
                    strCut2 = "-t " + Length;
                }
            }
            options.Add(strCut2);

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

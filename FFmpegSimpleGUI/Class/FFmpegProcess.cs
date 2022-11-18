using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; // MessageBox
using System.Diagnostics; // Process
using System.ComponentModel; // Win32Exception
using System.Text.RegularExpressions; // 正規表現
using System.Collections.ObjectModel; // ObservableCollection
using System.IO; // Path

namespace FFmpegSimpleGUI
{
    // FFmpegプロセス
    class FFmpegProcess
    {
        // エラー
        private int ErrorCode;
        const int ERROR_FFMPEG_NOT_FOUND = 1;
        const int ERROR_FFMPEG_FAILED = 2;
        const int ERROR_INPUT_LIST = 3;
        const int ERROR_UNEXPECTED = 4;

        // 終了コード
        private int ExitCode;

        // 時間表示イベント (処理中の動画の時間位置)
        public class TimeEventArgs : EventArgs
        {
            public string Time;
            public TimeEventArgs(string time) { Time = time; }
        }
        public event EventHandler TimeChanged = delegate { };

        // 動画の変換
        // option: オプション
        // return: 成否
        public bool Convert(FFmpegOption option)
        {
            // ffmpegの引数
            string ffmpegOptoin = option.GetConvertOption();
            // ffpmegの実行
            return ExecuteFFmpeg(ffmpegOptoin, option.ShowConsole);
        }

        // 動画の連結
        // list: 入力ファイルのリスト
        // showConsole: コンソールを表示するか
        // return: 成否
        public bool Concat(FFmpegOption option, ObservableCollection<PathInfo> list)
        {
            // ffmpegの引数
            string ffmpegOptoin = option.GetConcatOption(list);
            if(ffmpegOptoin == "")
            {
                ErrorCode = ERROR_INPUT_LIST;
                return false;
            }
            // ffpmegの実行
            return ExecuteFFmpeg(ffmpegOptoin, option.ShowConsole);
        }

        // FFpmegの実行
        // FFmpegOptoin: FFmpegのオプション
        // showConsole: コンソールを表示するか
        private bool ExecuteFFmpeg(string ffmpegOptoin, bool showConsole)
        {
            // コンソール表示時は終了後もコンソールを閉じないようにする
            string command, commandOption;
            if (showConsole) {
                command = "CMD.EXE";
                commandOption = "/K ffmpeg.exe " + ffmpegOptoin;
            } else {
                command = "ffmpeg.exe";
                commandOption = ffmpegOptoin;
            }

            // FFpmegの実行
            try {
                var p = new Process();
                if (!showConsole) {
                    // ログのハンドル
                    // ※ ffmpegのログは標準出力ではなくエラー出力に出ることに注意
                    //p.StartInfo.RedirectStandardOutput = true;
                    //p.OutputDataReceived += OutputDataReceived;
                    p.StartInfo.RedirectStandardError = true;
                    p.ErrorDataReceived += OutputDataReceived;
                }
                p.StartInfo.FileName = command;        // コマンド
                p.StartInfo.Arguments = commandOption; // コマンドオプション
                p.StartInfo.CreateNoWindow = !showConsole; // コンソール表示
                p.StartInfo.UseShellExecute = false;
                p.Start();
                if (!showConsole) {
                    //p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }
                p.WaitForExit();

                // 成否を返す
                if (showConsole)
                {
                    // コンソール表示時は常にtrueを返す (GUI側で成否を表示しない)
                    return true;
                } else {
                    if(p.ExitCode == 0) {
                        return true;
                    } else {
                        ErrorCode = ERROR_FFMPEG_FAILED;
                        ExitCode = p.ExitCode;
                        return false;
                    }
                }
            }
            catch(Exception exp) {
                if(exp.GetType() == typeof(Win32Exception)) {
                    ErrorCode = ERROR_FFMPEG_NOT_FOUND;
                } else {
                    ErrorCode = ERROR_UNEXPECTED;
                }
                return false;
            }
        }

        // 標準出力に行が出力されるたびに呼ばれる
        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // hh:mm:ss.xx の形式に一致する文字列があればイベント発行
            if (e.Data == null) return;
            var match = Regex.Match(e.Data, @"[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2}");
            if (match.Success)
            {
                string time = match.Value;
                var timeEvent = new TimeEventArgs(time);
                TimeChanged(this, timeEvent);
            }
        }

        // エラーメッセージの取得
        public string GetErrorMessage()
        {
            string message;
            switch (ErrorCode)
            {
                case ERROR_FFMPEG_NOT_FOUND:
                    message = "ffmpegが実行できません。";
                    break;
                case ERROR_FFMPEG_FAILED:
                    message = "変換に失敗しました。\n" +
                        "終了コード = " + ExitCode;
                    break;
                case ERROR_INPUT_LIST:
                    message = "入力リストの作成に失敗しました。";
                    break;
                case ERROR_UNEXPECTED:
                    message = "予期しないエラー";
                    break;
                default:
                    message = "ありえないエラー";
                    break;
            }
            return message;
        }
    }
}

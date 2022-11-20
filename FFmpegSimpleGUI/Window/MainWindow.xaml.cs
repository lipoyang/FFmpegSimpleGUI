using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.IO; // Path
using Microsoft.Win32; // OpenFileDialog
using System.Globalization; // DateTimeStyles
using System.Collections.ObjectModel; // ObservableCollection
using System.Windows.Threading; // DispatcherTimer

namespace FFmpegSimpleGUI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // 設定ファイル名
        const string SettingFile = "setting.json";

        // ファイルフィルタ
        const string FileFilter = 
            "MP4ファイル|*.mp4|MOVファイル|*.mov|WMVファイル|*.wmv|AVIファイル|*.avi|全てのファイル|*.*";

        // ファイルフィルタの選択インデックス
        private int FilterIndex = 0;

        // [変換] 追加オプション
        private string AdditionalOption = "";
        
        // [変換] ノイズ除去詳細オプション
        private string LmsDelay = "";
        private string LmsOrder = "";
        private string LmsLeak = "";
        private string LmsMu = "";

        // [再生] タイマ
        private DispatcherTimer TickTimer;

        // [再生] フラグ類
        private bool isSeekBarDragging = false; // シークバー操作中か
        private bool isPlaying  = false; // 再生中か
        private bool wasPlaying = false; // シークバー操作前に再生中だったか

        // [連結] ファイルパス情報 (入力ファイル名のグリッド用)
        private ObservableCollection<PathInfo> PathInfoList = new ObservableCollection<PathInfo>();

        public MainWindow()
        {
            InitializeComponent();

            // [連結] 入力ファイル名のグリッド用データを
            dataGrid.ItemsSource = PathInfoList;

            // [再生] タイマの設定
            TickTimer = new DispatcherTimer();
            TickTimer.Interval = TimeSpan.FromMilliseconds(200); // 0.2秒周期
            TickTimer.Tick += new EventHandler(TickTimer_Tick);
        }

        // 起動時
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 設定のロード
            var option = FFmpegOption.Load(SettingFile);
            SetOption(option);

            // [変換] ハンドラの追加
            TextBox[] textBoxes = new TextBox[9];
            textBoxes[0] = textInputPath;
            textBoxes[1] = textOutputPath;
            textBoxes[2] = textFrameRate;
            textBoxes[3] = textWidth;
            textBoxes[4] = textHeight;
            textBoxes[5] = textVolume;
            textBoxes[6] = textStartPos;
            textBoxes[7] = textEndPos;
            textBoxes[8] = textLength;
            foreach(var textbox in textBoxes) {
                textbox.LostFocus += textBox_LostFocus;
                textbox.KeyDown   += textBox_KeyDown;
            }
            // [変換] プログレスバーは非表示
            progressBar.Visibility = Visibility.Collapsed;
            labelProgress.Visibility = Visibility.Collapsed;

            // [再生] 再生時間表示をクリア
            labelPlayTime.Content = "";

            // [再生] 動画の表示
            mediaElement.LoadedBehavior = MediaState.Manual;
            mediaElement.ScrubbingEnabled = true;
            if(textInputPath.Text != "")
            {
                mediaElement_Init();
            }
        }

        // 閉じる前
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 設定のセーブ
            var option = GetOption();
            option.Save(SettingFile);
        }

        // オプションを画面に設定
        private void SetOption(FFmpegOption option)
        {
            // 入出力パス
            textInputPath.Text  = option.InputPath;
            textOutputPath.Text = option.OutputPath;
            // フレームレート
            checkFrameRate.IsChecked = option.FrameRateOption;
            textFrameRate.Text       = option.FrameRate;
            // リサイズ
            checkResize.IsChecked     = option.ResizeOption;
            checkWidthAuto.IsChecked  = option.WidthAuto;
            checkHeightAuto.IsChecked = option.HeightAuto;
            textWidth.Text            = option.Width;
            textHeight.Text           = option.Height;
            // 音声
            checkVolume.IsChecked     = option.VolumeOption;
            checkVolumeAuto.IsChecked = option.VolumeAuto;
            checkNoSound.IsChecked    = option.NoSound;
            textVolume.Text           = option.Volume;
            // 切り出し
            checkCut.IsChecked       = option.CutOption;
            checkFromFirst.IsChecked = option.FromFirst;
            checkToLast.IsChecked    = option.ToLast;
            radioEndPos.IsChecked    =  option.EndSelect;
            radioLength.IsChecked    = !option.EndSelect;
            textStartPos.Text        = option.StartPos;
            textEndPos.Text          = option.EndPos;
            textLength.Text          = option.Length;
            // ノイズ除去
            checkNoise.IsChecked = option.NoiseOption;
            LmsDelay             = option.LmsDelay;
            LmsOrder             = option.LmsOrder;
            LmsLeak              = option.LmsLeak;
            LmsMu                = option.LmsMu;
            // 追加オプション
            checkOption.IsChecked = option.AddOption;
            AdditionalOption      = option.AdditionalOption;
            // コンソール表示
            checkConsole.IsChecked    = option.ShowConsole;
            // オプション表示
            checkShowCommand.IsChecked = option.ShowCommand;
            // 連結
            textOutputPath2.Text = option.OutputPath2;
            checkNotReencode.IsChecked = option.NotReencode;

            // 表示
            checkFrameRate_Checked(null, null);
            checkResize_Checked(null, null);
            checkVolume_Checked(null, null);
            checkCut_Checked(null, null);
            checkNoise_Checked(null, null);
            checkOption_Checked(null, null);
            checkConsole_Checked(null, null);
            checkShowCommand_Checked(null, null);
        }

        // オプションを画面から取得
        private FFmpegOption　GetOption()
        {
            var option = new FFmpegOption();

            // 入出力パス
            option.InputPath  = textInputPath.Text;
            option.OutputPath = textOutputPath.Text;
            // フレームレート
            option.FrameRateOption = (bool)checkFrameRate.IsChecked;
            option.FrameRate       = textFrameRate.Text;
            // リサイズ
            option.ResizeOption = (bool)checkResize.IsChecked;
            option.WidthAuto    = (bool)checkWidthAuto.IsChecked;
            option.HeightAuto   = (bool)checkHeightAuto.IsChecked;
            option.Width        = textWidth.Text;
            option.Height       = textHeight.Text;
            // 音声
            option.VolumeOption = (bool)checkVolume.IsChecked;
            option.VolumeAuto   = (bool)checkVolumeAuto.IsChecked;
            option.NoSound      = (bool)checkNoSound.IsChecked;
            option.Volume       = textVolume.Text;
            // 切り出し
            option.CutOption = (bool)checkCut.IsChecked;
            option.FromFirst = (bool)checkFromFirst.IsChecked;
            option.ToLast    = (bool)checkToLast.IsChecked;
            option.EndSelect = (bool)radioEndPos.IsChecked;
            option.StartPos  = textStartPos.Text;
            option.EndPos    = textEndPos.Text;
            option.Length    = textLength.Text;
            // ノイズ除去
            option.NoiseOption = (bool)checkNoise.IsChecked;
            option.LmsDelay    = LmsDelay;
            option.LmsOrder    = LmsOrder;
            option.LmsLeak     = LmsLeak;
            option.LmsMu       = LmsMu;
            // 追加オプション
            option.AddOption        = (bool)checkOption.IsChecked;
            option.AdditionalOption = AdditionalOption;
            // コンソール表示
            option.ShowConsole = (bool)checkConsole.IsChecked;
            // オプション表示
            option.ShowCommand = (bool)checkShowCommand.IsChecked;
            // 連結
            option.OutputPath2 = textOutputPath2.Text;
            option.NotReencode = (bool)checkNotReencode.IsChecked;

            return option;
        }

        // [変換] 入力ファイル選択
        private void buttonInputPath_Click(object sender, RoutedEventArgs e)
        {
            string path = textInputPath.Text;
            string dir = "";
            string file = "";
            if(path != "")
            {
                dir = Path.GetDirectoryName(path);
                file = Path.GetFileName(path);
            }

            var dialog = new OpenFileDialog();
            dialog.Filter = FileFilter;
            if(dir  != "") dialog.InitialDirectory = dir;
            if(file != "") dialog.FileName = file;
            dialog.FilterIndex = FilterIndex;
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == true)
            {
                textInputPath.Text = dialog.FileName;
                FilterIndex = dialog.FilterIndex;

                // 動画の表示
                mediaElement_Init();
            }
        }

        // [変換] 入力ファイルのドラッグ＆ドロップ
        private void textInputPath_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop)) ?
                DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }
        private void textInputPath_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                textInputPath.Text = paths[0];

                // 動画の表示
                mediaElement_Init();
            }
        }

        // [変換] 出力ファイル選択
        private void buttonOutputPath_Click(object sender, RoutedEventArgs e)
        {
            string pathOutput = textOutputPath.Text;
            string pathInput  = textInputPath.Text;
            string dir = "";
            string file = "";
            if (pathOutput != "")
            {
                dir = Path.GetDirectoryName(pathOutput);
                file = Path.GetFileName(pathOutput);
            } else {
                if (pathInput != ""){
                    dir = Path.GetDirectoryName(pathInput);
                }
            }

            var dialog = new SaveFileDialog();
            dialog.Filter = FileFilter;
            if (dir  != "") dialog.InitialDirectory = dir;
            if (file != "") dialog.FileName = file;
            dialog.RestoreDirectory = true;
            dialog.OverwritePrompt = false; // ここではあえて上書き確認は出さない
            if (dialog.ShowDialog() == true)
            {
                textOutputPath.Text = dialog.FileName;
            }
        }

        // [変換] ノイズ除去詳細設定
        private void buttonNoise_Click(object sender, RoutedEventArgs e)
        {
            var form = new NoiseWindow();
            form.Delay = this.LmsDelay;
            form.Order = this.LmsOrder;
            form.Leak  = this.LmsLeak;
            form.Mu    = this.LmsMu;
            bool result = (bool)form.ShowDialog();
            if (result) {
                this.LmsDelay = form.Delay;
                this.LmsOrder = form.Order;
                this.LmsLeak  = form.Leak;
                this.LmsMu    = form.Mu;
            }
        }

        // [変換] 追加オプション設定
        private void buttonOption_Click(object sender, RoutedEventArgs e)
        {
            var form = new OptionWindow();
            form.Option = this.AdditionalOption;
            bool result = (bool)form.ShowDialog();
            if (result)
            {
                this.AdditionalOption = form.Option;
                ShowCommandLine();
            }
        }

        // [変換] フレームレート
        private void checkFrameRate_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (bool)checkFrameRate.IsChecked;
            textFrameRate.IsEnabled = check;
        }

        // [変換] リサイズ
        private void checkResize_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (bool)checkResize.IsChecked;
            if (check)
            {
                bool widthAuto  = (bool)checkWidthAuto.IsChecked;
                bool heightAuto = (bool)checkHeightAuto.IsChecked;
                // 幅自動と高さ自動は排他
                checkWidthAuto.IsEnabled  = !heightAuto;
                checkHeightAuto.IsEnabled = !widthAuto;
                // 幅自動のとき幅無効、高さ自動のとき高さ無効
                textWidth.IsEnabled  = !widthAuto;
                textHeight.IsEnabled = !heightAuto;
            }
            else
            {
                checkWidthAuto.IsEnabled  = false;
                checkHeightAuto.IsEnabled = false;
                textWidth.IsEnabled       = false;
                textHeight.IsEnabled      = false;
            }
            ShowCommandLine();
        }

        // [変換] 音量
        private void checkVolume_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (bool)checkVolume.IsChecked;
            if (check)
            {
                bool volAuto = (bool)checkVolumeAuto.IsChecked;
                bool noSound = (bool)checkNoSound.IsChecked;
                // 自動調整と音声無しは排他
                checkVolumeAuto.IsEnabled = !noSound;
                checkNoSound.IsEnabled    = !volAuto;
                // 自動調整か音声無しなら音量無効
                textVolume.IsEnabled = !(noSound || volAuto);
            }
            else
            {
                checkVolumeAuto.IsEnabled = false;
                checkNoSound.IsEnabled    = false;
                textVolume.IsEnabled      = false;
            }
            ShowCommandLine();
        }

        // [変換] 切り出し
        private void checkCut_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (bool)checkCut.IsChecked;
            if (check)
            {
                // 「最初から」と「終了 or 長さ」は有効
                checkFromFirst.IsEnabled = true;
                radioEndPos.IsEnabled    = true;
                radioLength.IsEnabled    = true;

                bool endSelect = (bool)radioEndPos.IsChecked;
                bool fromFirst = (bool)checkFromFirst.IsChecked;
                bool toLast    = (bool)checkToLast.IsChecked;

                // 「最初から」なら 開始時刻は無効
                textStartPos.IsEnabled = !fromFirst;

                if (endSelect) // 「終了」の場合
                {
                    // 「最後まで」は有効、最後までなら終了時刻無効
                    checkToLast.IsEnabled = true;
                    textEndPos.IsEnabled = !toLast;
                    // 長さ指定無効
                    textLength.IsEnabled = false;
                }
                else // 「長さ」の場合
                {
                    // 「最後まで」と終了時刻無効
                    checkToLast.IsEnabled = false;
                    textEndPos.IsEnabled  = false;
                    // 長さ指定有効
                    textLength.IsEnabled = true;
                }
            }
            else
            {
                radioEndPos.IsEnabled    = false;
                radioLength.IsEnabled    = false;
                checkFromFirst.IsEnabled = false;
                checkToLast.IsEnabled    = false;
                textStartPos.IsEnabled   = false;
                textEndPos.IsEnabled     = false;
                textLength.IsEnabled     = false;
            }
            ShowCommandLine();
        }

        // [変換] ノイズ除去
        private void checkNoise_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (bool)checkNoise.IsChecked;
            buttonNoise.IsEnabled = check;
            ShowCommandLine();
        }

        // [変換] 追加オプション
        private void checkOption_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (bool)checkOption.IsChecked;
            buttonOption.IsEnabled = check;
            ShowCommandLine();
        }

        // [変換] コマンドライン表示の有効無効切り替え
        private void checkShowCommand_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)checkShowCommand.IsChecked) {
                this.Height = 580;
                textCommand.Visibility = Visibility.Visible;
                buttonCopy.IsEnabled = true;
                ShowCommandLine();
            } else {
                this.Height = 500;
                textCommand.Visibility = Visibility.Collapsed;
                buttonCopy.IsEnabled = false;
            }
        }

        // [変換] コマンドライン表示
        private void ShowCommandLine()
        {
            var option = GetOption();
            textCommand.Text = "ffmpeg.exe " + option.GetConvertOption();
        }

        // [変換] テキストボックス更新時のコマンドライン表示の更新
        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ShowCommandLine();
        }
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                ShowCommandLine();
            }
        }

        // [変換] コマンドライン文字列をクリップボードにコピー
        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(textCommand.Text);
        }

        // [変換] 変換処理
        private async void buttonConvert_Click(object sender, RoutedEventArgs e)
        {
            // パラメータチェック
            if (ConvertParamCheck())
            {
                // パラメータ取得
                var option = GetOption();

                this.tabControl.IsEnabled = false;
                progressBar.Visibility = Visibility.Visible;
                labelProgress.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true; // マーキー表示

                // 処理時間位置の表示更新処理
                var ffmpeg = new FFmpegProcess();
                ffmpeg.TimeChanged += (_sender, _event) => {
                    Dispatcher.BeginInvoke((Action)(() => {
                        labelProgress.Content = ((FFmpegProcess.TimeEventArgs)_event).Time;
                    }));
                };
                // ffmpeg実行
                bool result = await Task.Run(() => ffmpeg.Convert(option));

                progressBar.Visibility = Visibility.Collapsed;
                labelProgress.Visibility = Visibility.Collapsed;
                this.tabControl.IsEnabled = true;
                this.Activate();
                
                // コンソール非表示なら結果をダイアログで表示
                if (!option.ShowConsole)
                {
                    if (result) {
                        MessageBox.Show("変換が完了しました。");
                    } else {
                        MessageBox.Show(ffmpeg.GetErrorMessage());
                    }
                }
            }
        }

        // [変換] パラメータチェック
        private bool ConvertParamCheck()
        {
            // 入力ファイル
            if(textInputPath.Text == "") {
                MessageBox.Show("入力ファイルを指定してください");
                return false;
            }
            if (!File.Exists(textInputPath.Text)) {
                MessageBox.Show("入力ファイルが見つかりません");
                return false;
            }
            // 出力ファイル
            string path = textOutputPath.Text;
            if (path == "") {
                MessageBox.Show("出力ファイルを指定してください");
                return false;
            }
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);
            if (file == "") {
                MessageBox.Show("出力ファイルを指定してください");
                return false;
            }
            if (!Directory.Exists(dir)) {
                MessageBox.Show("出力先のフォルダが見つかりません");
                return false;
            }
            if (File.Exists(path))
            {
                var result = MessageBox.Show(
                    file + "\nを上書きしますか？",
                    "確認",
                    MessageBoxButton.YesNo
                );
                if(result == MessageBoxResult.Yes) {
                    File.Delete(path);
                } else {
                    return false;
                }
            }
            // フレームレート
            if ((bool)checkFrameRate.IsChecked) {
                if (!double.TryParse(textFrameRate.Text, out _)) {
                    MessageBox.Show("フレームレートの指定が不正です");
                    return false;
                }
            }
            // リサイズ
            if ((bool)checkResize.IsChecked) {
                if (!(bool)checkWidthAuto.IsChecked) {
                    if(!int.TryParse(textWidth.Text, out _)) {
                        MessageBox.Show("幅の指定が不正です");
                        return false;
                    }
                }
                if (!(bool)checkHeightAuto.IsChecked) {
                    if (!int.TryParse(textHeight.Text, out _)) {
                        MessageBox.Show("高さの指定が不正です");
                        return false;
                    }
                }
            }
            // 音量
            if((bool)checkVolume.IsChecked && 
              !(bool)checkVolumeAuto.IsChecked &&
              !(bool)checkNoSound.IsChecked)
            {
                if(!int.TryParse(textVolume.Text, out _)) {
                    MessageBox.Show("音量レベルの指定が不正です");
                    return false;
                }
            }
            // 切り出し
            if ((bool)checkCut.IsChecked) {
                if (!(bool)checkFromFirst.IsChecked) {
                    if (!CheckTimeString(textStartPos.Text)) {
                        MessageBox.Show("開始位置の指定が不正です");
                        return false;
                    }
                }
                if ((bool)radioEndPos.IsChecked) {
                    if (!(bool)checkToLast.IsChecked) {
                        if (!CheckTimeString(textEndPos.Text)) {
                            MessageBox.Show("終了位置の指定が不正です");
                            return false;
                        }
                    }
                } else {
                    if (!CheckTimeString(textLength.Text)) {
                        MessageBox.Show("長さの指定が不正です");
                        return false;
                    }
                }
            }
            return true;
        }

        // [変換] 時間指定文字列のチェック
        private bool CheckTimeString(string str)
        {
            // 整数値の秒数に換算できるならOK
            if (int.TryParse(str, out _)) return true;

            // hh:mm:ss形式ならOK
            if(DateTime.TryParseExact(
                str, "HH:mm:ss", null, DateTimeStyles.None, out _))
            {
                return true;
            }
            // hh:mm:ss.xxx形式ならOK
            string[] split = str.Split('.');
            if(split.Length == 2) {
                if (DateTime.TryParseExact(split[0], "HH:mm:ss",
                        null, DateTimeStyles.None, out _))
                {
                    if(split[1].Length == 3) { 
                        int msec;
                        if(int.TryParse(split[1], out msec)) {
                            if(msec >= 0 && msec <= 999) return true;
                        }
                    }
                }
            }
            // それ以外はNG
            return false;
        }

        // [変換][連結] コンソール表示
        private void checkConsole_Checked(object sender, RoutedEventArgs e)
        {
            ShowCommandLine(); // ここではコマンド表示の変更のみ
        }

        // [連結] パラメータチェック
        private bool ConcatParamCheck()
        {
            // 出力ファイル
            string path = textOutputPath2.Text;
            if (path == "")
            {
                MessageBox.Show("出力ファイルを指定してください");
                return false;
            }
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);
            if (file == "")
            {
                MessageBox.Show("出力ファイルを指定してください");
                return false;
            }
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("出力先のフォルダが見つかりません");
                return false;
            }
            if (File.Exists(path))
            {
                var result = MessageBox.Show(
                    file + "\nを上書きしますか？",
                    "確認",
                    MessageBoxButton.YesNo
                );
                if (result == MessageBoxResult.Yes)
                {
                    File.Delete(path);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        // [連結] 選択中の動画を前へ
        private void buttonBefore_Click(object sender, RoutedEventArgs e)
        {
            int index = dataGrid.SelectedIndex;
            if (index >= 0) {
                if (index >= 1) {
                    var item = PathInfoList[index];
                    PathInfoList.RemoveAt(index);
                    PathInfoList.Insert(index - 1, item);
                    dataGrid.SelectedIndex = index - 1;
                }
            } else {
                MessageBox.Show("ファイルが選択されていません");
            }
        }

        // [連結] 選択中の動画を後へ
        private void buttonAfter_Click(object sender, RoutedEventArgs e)
        {
            int index = dataGrid.SelectedIndex;
            if (index >= 0) {
                if (index < PathInfoList.Count - 1) {
                    var item = PathInfoList[index];
                    PathInfoList.RemoveAt(index);
                    PathInfoList.Insert(index + 1, item);
                    dataGrid.SelectedIndex = index + 1;
                }
            } else {
                MessageBox.Show("ファイルが選択されていません");
            }
        }

        // [連結] 動画の追加
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = FileFilter;
            //dialog.InitialDirectory = dir;
            //dialog.FileName = file;
            dialog.FilterIndex = FilterIndex;
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == true)
            {
                string dir = Path.GetDirectoryName(dialog.FileName);
                string file = Path.GetFileName(dialog.FileName);
                FilterIndex = dialog.FilterIndex;

                var item = new PathInfo { FileName = file, DirName = dir };
                //int index = dataGrid.SelectedIndex;
                //if (index >= 0) {
                //    pathInfo.Insert(index, item);
                //} else {
                    PathInfoList.Add(item);
                //}
            }
        }

        // [連結] 動画の削除
        private void buttonDel_Click(object sender, RoutedEventArgs e)
        {
            int index = dataGrid.SelectedIndex;
            if (index >= 0) {
                PathInfoList.RemoveAt(index);
            } else {
                MessageBox.Show("ファイルが選択されていません");
            }
        }

        // [連結] 動画ファイルのドラッグ＆ドロップ
        private void dataGrid_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop)) ?
                DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }
        private void dataGrid_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                foreach (string path in paths)
                {
                    string dir = Path.GetDirectoryName(path);
                    string file = Path.GetFileName(path);

                    var item = new PathInfo { FileName = file, DirName = dir };
                    PathInfoList.Add(item);
                }
            }
        }

        // [連結] 出力ファイル選択
        private void buttonOutputPath2_Click(object sender, RoutedEventArgs e)
        {
            string path = textOutputPath2.Text;
            string dir = "";
            string file = "";
            if (path != "")
            {
                dir = Path.GetDirectoryName(path);
                file = Path.GetFileName(path);
            }

            var dialog = new SaveFileDialog();
            dialog.Filter = FileFilter;
            if(dir  != "") dialog.InitialDirectory = dir;
            if(file != "") dialog.FileName = file;
            if (dialog.ShowDialog() == true)
            {
                textOutputPath2.Text = dialog.FileName;
            }
        }

        // [連結] 連結処理
        private async void buttonConcat_Click(object sender, RoutedEventArgs e)
        {
            // パラメータチェック
            if (ConcatParamCheck())
            {
                // パラメータ取得
                var option = GetOption();

                this.tabControl.IsEnabled = false;
                progressBar.Visibility = Visibility.Visible;
                labelProgress.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true; // マーキー表示

                // 処理時間位置の表示更新処理
                var ffmpeg = new FFmpegProcess();
                ffmpeg.TimeChanged += (_sender, _event) => {
                    Dispatcher.BeginInvoke((Action)(() => {
                        labelProgress.Content = ((FFmpegProcess.TimeEventArgs)_event).Time;
                    }));
                };
                // ffmpeg実行
                bool result = await Task.Run(() => ffmpeg.Concat(option, PathInfoList));

                progressBar.Visibility = Visibility.Collapsed;
                labelProgress.Visibility = Visibility.Collapsed;
                this.tabControl.IsEnabled = true;
                this.Activate();

                // コンソール非表示なら結果をダイアログで表示
                if (!(bool)checkConsole.IsChecked)
                {
                    if (result) {
                        MessageBox.Show("連結が完了しました。");
                    } else {
                        MessageBox.Show(ffmpeg.GetErrorMessage());
                    }
                }
            }
        }

        // [再生] 再生
        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Play();
            TickTimer.Start();
            isPlaying = true;
        }

        // [再生] 一時停止
        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            TickTimer.Stop();
            mediaElement.Pause();
            isPlaying = false;

            // 時間表示
            TimeSpan ts = mediaElement.Position;
            labelPlayTime.Content = ts.ToString(@"hh\:mm\:ss\.ff");
        }

        // [再生] 停止
        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            TickTimer.Stop();
            mediaElement.Stop();
            isPlaying = false;

            TimeSpan ts = TimeSpan.FromSeconds(0);
            mediaElement.Position = ts;
            labelPlayTime.Content = ts.ToString(@"hh\:mm\:ss\.ff");
            seekBar.Value = 0;
        }

        // [再生] コマ戻し
        private void buttonPrevFrame_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying) return;

            double pos = mediaElement.Position.TotalSeconds;
            pos = Math.Floor(pos * 10.0 + 0.5) / 10.0;
            if (pos >= 0.1) pos -= 0.1;
            TimeSpan ts = TimeSpan.FromSeconds(pos);
            mediaElement.Position = ts;

            // 時間表示
            labelPlayTime.Content = ts.ToString(@"hh\:mm\:ss\.ff");
        }

        // [再生] コマ送り
        private void buttonNextFrame_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying) return;

            double max = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            double pos = mediaElement.Position.TotalSeconds;
            pos = Math.Floor(pos * 10.0 + 0.5) / 10.0;
            if (pos <= max - 0.1) pos += 0.1;
            TimeSpan ts = TimeSpan.FromSeconds(pos);
            mediaElement.Position = ts;

            // 時間表示
            labelPlayTime.Content = ts.ToString(@"hh\:mm\:ss\.ff");
        }


        // [再生] スライダ
        private void seekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isSeekBarDragging = true; // シークバーのドラッグ開始
            wasPlaying = isPlaying;
            mediaElement.Pause();
        }
        private void seekBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isSeekBarDragging = false; // シークバーのドラッグ終了
            if (wasPlaying) mediaElement.Play();
        }
        private void seekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isSeekBarDragging)
            {
                // 動画の秒数位置→シークバーの位置に設定
                TimeSpan ts = TimeSpan.FromSeconds(seekBar.Value);
                mediaElement.Position = ts;

                // 時間表示
                labelPlayTime.Content = ts.ToString(@"hh\:mm\:ss\.ff");
            }
        }

        // [再生] タイマーハンドラ
        private void TickTimer_Tick(object sender, EventArgs e)
        {
            // シークバーの位置→動画の秒数位置に設定
            seekBar.Value = mediaElement.Position.TotalSeconds;

            // 時間表示
            labelPlayTime.Content = mediaElement.Position.ToString(@"hh\:mm\:ss\.ff");
        }

        // [再生] 動画の初期表示
        private void mediaElement_Init()
        {
            mediaElement.Source = new Uri(textInputPath.Text, UriKind.Absolute);
            mediaElement.Stop();
            isPlaying = false;

            // 時間表示
            labelPlayTime.Content = mediaElement.Position.ToString(@"hh\:mm\:ss\.ff");
        }
        // [再生] 動画を開いたときに長さを取得してシークバーに設定
        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                // シークバーの長さは動画の秒数、1秒きざみで変化
                TimeSpan ts = mediaElement.NaturalDuration.TimeSpan;
                seekBar.Maximum = ts.TotalSeconds;
                seekBar.SmallChange = 1;
                seekBar.LargeChange = Math.Min(10, ts.TotalSeconds / 10);

                // おまじない
                mediaElement.Play ();
                mediaElement.Pause();
            }
        }
    }
}

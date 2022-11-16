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
using System.Windows.Shapes;

namespace FFmpegSimpleGUI
{
    /// <summary>
    /// NoiseWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NoiseWindow : Window
    {
        public string Delay { get; set; }
        public string Order { get; set; }
        public string Leak  { get; set; }
        public string Mu    { get; set; }

        public NoiseWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textDelay.Text = Delay;
            textOrder.Text = Order;
            textLeak.Text  = Leak;
            textMu.Text    = Mu;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if(!int.TryParse(textDelay.Text, out _)) {
                MessageBox.Show("時間差の指定が不正です。");
                return;
            }
            if(!int.TryParse(textOrder.Text, out _)) {
                MessageBox.Show("次数の指定が不正です。");
                return;
            }
            if(!double.TryParse(textLeak.Text, out _)) {
                MessageBox.Show("漏れ係数の指定が不正です。");
                return;
            }
            if(!double.TryParse(textMu.Text, out _)) {
                MessageBox.Show("ステップサイズの指定が不正です。");
                return;
            }
            Delay = textDelay.Text;
            Order = textOrder.Text;
            Leak  = textLeak.Text;
            Mu    = textMu.Text;
            DialogResult = true;
            this.Close();
        }
    }
}

using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using System;
using System.IO;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for NamesInputWindow.xaml
    /// </summary>
    public partial class NamesInputWindow : Window
    {
        /// <summary>
        /// 名单导入窗口构造函数。
        /// </summary>
        public NamesInputWindow()
        {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                if (mainWindow.GetMainWindowTheme() == "Light")
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Light-PopupWindow.xaml", UriKind.Relative) };
                    Application.Current.Resources.MergedDictionaries.Add(rd);
                }
                else
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Dark-PopupWindow.xaml", UriKind.Relative) };
                    Application.Current.Resources.MergedDictionaries.Add(rd);
                }
            }
        }

        string originText = "";

        /// <summary>
        /// 窗口加载时读取已保存名单。
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(App.RootPath + "Names.txt"))
            {
                TextBoxNames.Text = File.ReadAllText(App.RootPath + "Names.txt");
                originText = TextBoxNames.Text;
            }
        }

        /// <summary>
        /// 窗口关闭前询问是否保存名单修改。
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (originText != TextBoxNames.Text)
            {
                var result = MessageBox.Show("是否保存？", "名单导入", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    File.WriteAllText(App.RootPath + "Names.txt", TextBoxNames.Text);
                }
            }
        }

        /// <summary>
        /// 点击关闭按钮。
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

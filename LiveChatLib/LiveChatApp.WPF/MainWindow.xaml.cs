using CefSharp;
using CefSharp.Wpf;
using LiveChatApp.CefSharpHandlers;
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

namespace LiveChatApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var h_Menu = new ManuHandler();
            this.Browser.MenuHandler = h_Menu;
            this.Browser.FrameLoadEnd += Browser_FrameLoadEnd;
        }

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e) {
            if (e.Url.Contains("ajax/miniLogin/redirect"))
            {
                var cmon = Cef.GetGlobalCookieManager();
                var visitor = new CookiesCollector();
                var bili_jct = "";
                visitor.SendCookie += cookie =>
                {
                    if (cookie.Name.Equals("bili_jct") && cookie.Domain.Equals(".bilibili.com") && cookie.Path.Equals("/"))
                    {
                        bili_jct = cookie.Value;
                        MessageBox.Show("Hit Page: " + e.Url + "\nbili_jct: " + bili_jct);
                    }
                };
                cmon.VisitAllCookies(visitor);
            }
        }
    }
}

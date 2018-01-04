using Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TimFlyBrain.Managers;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TimFlyBrain
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GlobalManager.Instance.OnNewMotorsValues += OnGlobalManagerNewMotorsValues;
        }

        private async void OnGlobalManagerNewMotorsValues(object sender, string message)
        {
            List<string> positions = message.Split('|').ToList();

            if (positions.Count == 4)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        Txt_ValueMotorTopLeft.Text = positions[0];
                        Txt_ValueMotorTopRight.Text = positions[1];
                        Txt_ValueMotorBottomLeft.Text = positions[2];
                        Txt_ValueMotorBottomRight.Text = positions[3];
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            GlobalManager.Instance.OnNewMotorsValues += OnGlobalManagerNewMotorsValues;
        }
    }
}

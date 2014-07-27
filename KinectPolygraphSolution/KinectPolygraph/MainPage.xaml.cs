using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using Microsoft.Kinect;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace KinectPolygraph
{
    //azure machine learning
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private KinectSensor _sensor;
        private FacialMicroExpressions _microExpressions;
        private int _eyeContactLostCount = 0;
        private int _extendedEyeBlink = 0;



        public int LostContactCount
        {
            get { return (int)GetValue(LostContactCountProperty); }
            set { SetValue(LostContactCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LostContactCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LostContactCountProperty =
            DependencyProperty.Register("LostContactCount", typeof(int), typeof(MainPage), new PropertyMetadata(0));

        

        public int EyeBlinkCount
        {
            get { return (int)GetValue(EyeBlinkCountProperty); }
            set { SetValue(EyeBlinkCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EyeBlinkCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EyeBlinkCountProperty =
            DependencyProperty.Register("EyeBlinkCount", typeof(int), typeof(MainPage), new PropertyMetadata(0));

        
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;
           
            this.DataContext = this;
        }

        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _sensor.Close();
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
           
            _sensor.Open();
            _microExpressions = new FacialMicroExpressions(_sensor);

            _microExpressions.LossOfEyeContactArrived += _microExpressions_LossOfEyeContactArrived;
            _microExpressions.ExtendedBlinkArrived += _microExpressions_ExtendedBlinkArrived;

        }

        void _microExpressions_ExtendedBlinkArrived(object sender, ExtendedBlinkArrivedEventArgs args)
        {
            _extendedEyeBlink++;
            EyeBlinkCount = _extendedEyeBlink;
        }

        void _microExpressions_LossOfEyeContactArrived(object sender, LossOfEyeContactArrivedEventArgs args)
        {
            //throw new NotImplementedException();
            _eyeContactLostCount++;
            LostContactCount = _eyeContactLostCount;
        }


    }
}

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
        private BodyFrameReader _bodyReader;
        private VerbalAnalysis _verbalAnalysis;
        private BodyAnalysis _bodyAnalysis;
        private MultiSourceFrame _msFrame;
        private MultiSourceFrameReader _msReader;
        private AudioSource _audioSource;




        public int MouthCoverCount
        {
            get { return (int)GetValue(MouthCoverCountProperty); }
            set { SetValue(MouthCoverCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MouthCoverCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MouthCoverCountProperty =
            DependencyProperty.Register("MouthCoverCount", typeof(int), typeof(MainPage), new PropertyMetadata(0));

        

        public int NoseTouchCount
        {
            get { return (int)GetValue(NoseTouchCountProperty); }
            set { SetValue(NoseTouchCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NoseTouchCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NoseTouchCountProperty =
            DependencyProperty.Register("NoseTouchCount", typeof(int), typeof(MainPage), new PropertyMetadata(0));

        

        public int SpeechGapCount
        {
            get { return (int)GetValue(SpeechGapCountProperty); }
            set { SetValue(SpeechGapCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeechGapCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeechGapCountProperty =
            DependencyProperty.Register("SpeechGapCount", typeof(int), typeof(MainPage), new PropertyMetadata(0));

        

        public int EyeBrowCount
        {
            get { return (int)GetValue(EyeBrowCountProperty); }
            set { SetValue(EyeBrowCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EyeBrowCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EyeBrowCountProperty =
            DependencyProperty.Register("EyeBrowCount", typeof(int), typeof(MainPage), new PropertyMetadata(0));

        
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
            _msReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body| FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared );
            _msReader.MultiSourceFrameArrived += _msReader_MultiSourceFrameArrived;
            _sensor.Open();
            _bodyReader = _sensor.BodyFrameSource.OpenReader();
            _bodyReader.FrameArrived +=_bodyReader_FrameArrived;
            _audioSource = _sensor.AudioSource;
            _microExpressions = new FacialMicroExpressions(_sensor);

            _microExpressions.LossOfEyeContactArrived += _microExpressions_LossOfEyeContactArrived;
            _microExpressions.ExtendedBlinkArrived += _microExpressions_ExtendedBlinkArrived;

            _microExpressions.EyebrowsDrawnUpArrived += _microExpressions_EyebrowsDrawnUpArrived;

            //_verbalAnalysis = new VerbalAnalysis(_audioSource );
            //_verbalAnalysis.SpeechPauseArrived += _verbalAnalysis_SpeechPauseArrived;

            _bodyAnalysis = new BodyAnalysis(_sensor);
            _bodyAnalysis.NoseTouchArrived += _bodyAnalysis_NoseTouchArrived;
            _bodyAnalysis.MouthCoverArrived += _bodyAnalysis_MouthCoverArrived;
        }

        void _bodyAnalysis_MouthCoverArrived(object sender, MouthCoverArrivedEventArgs args)
        {
            MouthCoverCount++;
        }

        void _msReader_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        {
            
        }

        void _bodyAnalysis_NoseTouchArrived(object sender, NoseTouchArrivedEventArgs args)
        {
            NoseTouchCount++;
        }

        void _verbalAnalysis_SpeechPauseArrived(object sender, SpeechPauseArrivedEventArgs args)
        {
            SpeechGapCount++;
        }

        void _microExpressions_EyebrowsDrawnUpArrived(object sender, EyebrowsDrawnUpArrivedEventArgs args)
        {
            EyeBrowCount++;
        }

        void _bodyReader_FrameArrived(BodyFrameReader sender, BodyFrameArrivedEventArgs args)
        {
           using (var bodyFrame = args.FrameReference.AcquireFrame())
           {
               if (bodyFrame != null)
               {
                   Body[] bodies = new Body[bodyFrame.BodyCount];
                   bodyFrame.GetAndRefreshBodyData(bodies);
                   foreach(var body in bodies)
                   {
                       if (body.IsTracked)
                       {
                           _microExpressions.startCapture(body);
                           _bodyAnalysis.startCapture(body);
                           return;
                       }
                       else
                           _microExpressions.stopCapture();
                   }
               }
           }
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

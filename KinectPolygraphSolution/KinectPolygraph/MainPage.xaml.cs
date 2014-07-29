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
using Windows.UI.Xaml.Media.Imaging;

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
        TextBlock[] StatusTexts = new TextBlock[6];
        private WriteableBitmap wBitmap = null;
        private Stream stream = null;
        private byte[] colorPixels = null;
        private const int cDepthWidth = 512;
        private const int cDepthHeight = 424;
        private const int cInfraredWidth = 512;
        private const int cInfraredHeight = 424;
        private const int cColorWidth = 1920;
        private const int cColorHeight = 1080;
        private  uint bytesPerPixel;
        private string[] questions;
        private int questionIndex = -1;
        private Dictionary<string, float> baseResult;
        private Dictionary<string, float> baseLieResult;
        private Dictionary<string, float> results;

        public string LieDetection
        {
            get { return (string)GetValue(LieDetectionProperty); }
            set { SetValue(LieDetectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LieDetection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LieDetectionProperty =
            DependencyProperty.Register("LieDetection", typeof(string), typeof(MainPage), new PropertyMetadata(""));

        
        public string CurrentQuestion
        {
            get { return (string)GetValue(CurrentQuestionProperty); }
            set { SetValue(CurrentQuestionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentQuestion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentQuestionProperty =
            DependencyProperty.Register("CurrentQuestion", typeof(string), typeof(MainPage), new PropertyMetadata(""));

        
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
          //  gifAnimation.PlayOnLoad = false;
            //gifAnimation.ImageUrl = "/Images/piconocio-nolie-o.gif";

            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;           
            this.DataContext = this;

            //TODO: Load questions from a repository
            this.questions = new string[10];
            questions[0] = "Are you a Male gender?";
            questions[1] = "Are you 2 feet tall?";
            questions[2] = "Are you physically at Microsoft?";
            //questions[3] = "Can you see me (Kinect)?";

            questions[3] = "Are you 9 Feet Tall?";
            questions[4] = "Were you born in a spaceship?";
            questions[5] = "Did your mother name you 12345?";

            questions[6] = "Did you ever steal a piece of candy?";

            questions[7] = "Did you ever eat a Rat?";
            questions[8] = "Have you ever been accused of lying?";
            questions[9] = "Did you have a good experience at the Hackathon?";
            baseResult = new Dictionary<string, float>();
            baseLieResult = new Dictionary<string, float>();
            results = new Dictionary<string, float>();

           // baseResult.Add("LookAway", 0f);
           // baseResult.Add("EyeBrows", 0f);
           // baseResult.Add("")
               

            
        }

        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _sensor.Close();
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
           
            this.bytesPerPixel = _sensor.ColorFrameSource.FrameDescription.BytesPerPixel;
            this.colorPixels = new byte[cColorWidth * cColorHeight * this.bytesPerPixel];
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

            _verbalAnalysis = new VerbalAnalysis(_audioSource );
            _verbalAnalysis.SpeechPauseArrived += _verbalAnalysis_SpeechPauseArrived;

            _bodyAnalysis = new BodyAnalysis(_sensor);
            _bodyAnalysis.NoseTouchArrived += _bodyAnalysis_NoseTouchArrived;
            _bodyAnalysis.MouthCoverArrived += _bodyAnalysis_MouthCoverArrived;

           
            
         //   this.wBitmap = new WriteableBitmap(_sensor.ColorFrameSource.FrameDescription.Width, _sensor.ColorFrameSource.FrameDescription.Height);
         //   this.stream = this.wBitmap.PixelBuffer.AsStream();

            StartInitialAnimations();
        }

        void _bodyAnalysis_MouthCoverArrived(object sender, MouthCoverArrivedEventArgs args)
        {
            MouthCoverCount++;
        }

        void _msReader_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        {
           
        }
         private void RenderColorPixels(byte[] pixels)
        {
             //TODO: add a video view of the user
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(pixels, 0, pixels.Length);
            wBitmap.Invalidate();
           
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
                           PersonFound.Text = string.Format("Tracked Id: {0}" , body.TrackingId );
                           _microExpressions.startCapture(body);
                           _bodyAnalysis.startCapture(body);
                           return;
                       }
                       else
                       {
                           _microExpressions.stopCapture();
                           PersonFound.Text = "Body not found";
                       }
                   }
               }
               
           }
        }

        void StartInitialAnimations()
        {
            //was going to use some form of animation 
            //but decided against it...
            // Start the animation
            //VisualStateManager.GoToState(this, VSG_Attract.Name, true);
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

        
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (questionIndex == -1) return;

            
            if (questionIndex <= 2)
            {
                //get results from normal truths
                baseResult.Add("LookAway" + questionIndex.ToString(), LostContactCount);
                LostContactCount = 0;
                baseResult.Add("MouthTouch" + questionIndex.ToString(), MouthCoverCount);
                MouthCoverCount = 0;
                baseResult.Add("NoseTouch" + questionIndex.ToString(), NoseTouchCount);
                NoseTouchCount = 0;
                baseResult.Add("SpeechGap" + questionIndex.ToString(), SpeechGapCount);
                SpeechGapCount = 0;
                baseResult.Add("EyeBrows" + questionIndex.ToString(), EyeBrowCount);
                EyeBrowCount = 0;
                baseResult.Add("EyeBlink" + questionIndex.ToString(), EyeBlinkCount);
                EyeBlinkCount = 0;
               
            }

            if (questionIndex >= 3 && questionIndex <=5 )
            {
                baseLieResult.Add("LookAway" + questionIndex.ToString(), LostContactCount);
                LostContactCount = 0;
                baseLieResult.Add("MouthTouch" + questionIndex.ToString(), MouthCoverCount);
                MouthCoverCount = 0;
                baseLieResult.Add("NoseTouch" + questionIndex.ToString(), NoseTouchCount);
                NoseTouchCount = 0;
                baseLieResult.Add("SpeechGap" + questionIndex.ToString(), SpeechGapCount);
                SpeechGapCount = 0;
                baseLieResult.Add("EyeBrows" + questionIndex.ToString(), EyeBrowCount);
                EyeBrowCount = 0;
                baseLieResult.Add("EyeBlink" + questionIndex.ToString(), EyeBlinkCount);
                EyeBlinkCount = 0;
                
            }

            //TODO: add a more sophisticated form of calculating lie detection
            if (questionIndex >= 6)
            {
                var strMouthTouchResultIndex = string.Format("MouthTouch{0}", questionIndex );
                var strLookAwayResultIndex = string.Format("LookAway{0}", questionIndex);
                var strNoseTouchResultIndex = string.Format("NoseTouch{0}", questionIndex);
                var strSpeechResultIndex = string.Format("SpeechGap{0}", questionIndex );
                var strEyeBrowsResultIndex = string.Format("EyeBrows{0}", questionIndex );
                var strEyeBlinkResultIndex = string.Format("EyeBlink{0}", questionIndex);


                //Compare results
                results.Add(strLookAwayResultIndex, LostContactCount);
                LostContactCount = 0;
                results.Add(strMouthTouchResultIndex, MouthCoverCount);
                MouthCoverCount = 0;
                results.Add(strNoseTouchResultIndex, NoseTouchCount);
                NoseTouchCount = 0;
                results.Add(strSpeechResultIndex, SpeechGapCount);
                SpeechGapCount = 0;
                results.Add(strEyeBrowsResultIndex, EyeBrowCount);
                EyeBrowCount = 0;
                results.Add(strEyeBlinkResultIndex, EyeBlinkCount);
                EyeBlinkCount = 0;

                int MouthLieTouchCompare = 0;
                int LookAwayLieCompare = 0;
                int EyeBrowsLieCompare = 0;
                int EyeBlinkLieCompare = 0;
                int NoseTouchLieCompare = 0;
                int SpeechGapLieCompare = 0;

                for (int i = 3; i<6; i++)
                {
                    var strMouthIndex = string.Format("MouthTouch{0}", i);
                    MouthLieTouchCompare += (int)baseLieResult[strMouthIndex];

                    var strLookAwayIndex = string.Format("LookAway{0}", i);
                    LookAwayLieCompare += (int)baseLieResult[strLookAwayIndex];

                    var strEyeBrowsIndex = string.Format("EyeBrows{0}", i);
                    EyeBrowsLieCompare += (int)baseLieResult[strEyeBrowsIndex];

                    var strEyeBlinkIndex = string.Format("EyeBlink{0}", i);
                    EyeBlinkLieCompare += (int)baseLieResult[strEyeBlinkIndex];

                    var strNoseTouchIndex = string.Format("NoseTouch{0}", i);
                    NoseTouchLieCompare += (int)baseLieResult[strNoseTouchIndex];

                    var strSpeechGapIndex = string.Format("SpeechGap{0}", i);
                    SpeechGapLieCompare += (int)baseLieResult[strSpeechGapIndex];

                }

                var avgMouthTouchLieCount = MouthLieTouchCompare / 3;
                var avgLookAwayLieCount = LookAwayLieCompare / 3;
                var avgEyeBrowLieCount = EyeBrowsLieCompare / 3;
                var avgEyeBlinkLieCount = EyeBlinkLieCompare / 3;
                var avgNoseLieCount = NoseTouchLieCompare / 3;
                var avgSpeechLieCount = SpeechGapLieCompare / 3;

                if (System.Math.Abs((int)results[strMouthTouchResultIndex] - (int)avgMouthTouchLieCount) <= 1)
                {
                    LieDetection += "It appears as though you lied based on your mouth touching. ";
                    webView.Navigate(new Uri("ms-appx-web:///Assets/liar.html"));

                }
                else
                {
                    webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
                }

                if (System.Math.Abs((int)results[strNoseTouchResultIndex] - (int)avgNoseLieCount) <= 1)
                {
                    LieDetection += " It appears as though you lied based on your nose touching. ";
                    webView.Navigate(new Uri("ms-appx-web:///Assets/liar.html"));

                }
                else
                {
                    webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
                }

                if (System.Math.Abs((int)results[strEyeBlinkResultIndex] - (int)avgEyeBlinkLieCount) <= 1)
                {
                    LieDetection += " It appears as though you lied based on your extended eye blinking. ";
                    webView.Navigate(new Uri("ms-appx-web:///Assets/liar.html"));

                }
                else
                {
                    webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
                }

                if (System.Math.Abs((int)results[strEyeBrowsResultIndex] - (int)avgEyeBrowLieCount) <= 1)
                {
                    LieDetection = " It appears as though you lied based on your eye brow movement. ";
                    webView.Navigate(new Uri("ms-appx-web:///Assets/liar.html"));

                }
                else
                {
                    webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
                }

                if (System.Math.Abs((int)results[strSpeechResultIndex] - (int)avgSpeechLieCount) <= 1)
                {
                    LieDetection += " It appears as though you lied based on your speech analysis. ";
                    webView.Navigate(new Uri("ms-appx-web:///Assets/liar.html"));

                }
                else
                {
                    webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
                }

            }
            questionIndex++;
            if (questionIndex < 10)
                CurrentQuestion = questions[questionIndex];
            else
            {
                CurrentQuestion = string.Empty;
                LieDetection = string.Empty;
                questionIndex = -1;
                baseResult.Clear();
                baseLieResult.Clear();
                results.Clear();
                btnNext.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            questionIndex = 0;
            CurrentQuestion = questions[0];
            webView.Navigate(new Uri("ms-appx-web:///Assets/default.html"));
            btnNext.Visibility = Windows.UI.Xaml.Visibility.Visible;

            EyeBlinkCount = 0;
            EyeBrowCount = 0;
            LostContactCount = 0;
            SpeechGapCount = 0;
            MouthCoverCount = 0;
            NoseTouchCount = 0;
        }


    }
}

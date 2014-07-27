using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Windows.Foundation;

namespace KinectPolygraph
{  
 
    public delegate void EyebrowsDrawnUpArrivedEventHandler(object sender, EyebrowsDrawnUpArrivedEventArgs args);

    public delegate void ExtendedBlinkArrivedEventHandler(object sender, ExtendedBlinkArrivedEventArgs args);

    public delegate void LossOfEyeContactArrivedEventHandler(object sender, LossOfEyeContactArrivedEventArgs args);

    public delegate void GulpingArrivedEventHandler(object sender, GulpingArrivedEventArgs args);


    public class FacialMicroExpressions : baseAnalysis
    {
        KinectSensor _sensor;
        private ulong _trackingId;
        private MultiSourceFrameReader _msReader;
        private FaceFrameSource _faceSource;
        private FaceFrameReader _faceReader;
        private int _eyeClosedTickCount;
        private EyesState _eyesState;
        private FaceAlignment _faceAlignment;
        private HighDefinitionFaceFrameSource _hdSource;
        private HighDefinitionFaceFrameReader _hdReader;
        private float _leftBrowDelta;
        private float _rightBrowDelta;
        private float _leftBrow;
        private float _rightBrow;
        public FacialMicroExpressions(KinectSensor sensor)
        {
            _sensor = sensor;
            _eyesState = EyesState.Opened ;
            _msReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Depth | FrameSourceTypes.Infrared);
            _faceSource = new FaceFrameSource(_sensor, 0, FaceFrameFeatures.FaceEngagement | FaceFrameFeatures.LeftEyeClosed | FaceFrameFeatures.LookingAway | FaceFrameFeatures.FaceEngagement | FaceFrameFeatures.Happy | FaceFrameFeatures.MouthMoved | FaceFrameFeatures.MouthOpen | FaceFrameFeatures.RightEyeClosed | FaceFrameFeatures.RotationOrientation);

            _faceReader = _faceSource.OpenReader();
            _faceReader.FrameArrived += _faceReader_FrameArrived;
            _msReader.MultiSourceFrameArrived += _msReader_MultiSourceFrameArrived;

            //TODO: Use HDFace to dermine gulping, Eyebrows
            _hdSource = new HighDefinitionFaceFrameSource(_sensor);
            _hdReader = _hdSource.OpenReader();
            _hdReader.FrameArrived += _hdReader_FrameArrived;
            
            
        }

        void _hdReader_FrameArrived(HighDefinitionFaceFrameReader sender, HighDefinitionFaceFrameArrivedEventArgs args)
        {

            using (var hdFaceFrame = args.FrameReference.AcquireFrame())
            {
                hdFaceFrame.GetAndRefreshFaceAlignmentResult(this._faceAlignment);
                var animationUnits = this._faceAlignment.AnimationUnits;
                float leftValue = 0.0f;
                float rightValue = 0.0f;

                foreach (var animUnit in animationUnits
                    )
                {

                    if (animUnit.Key == FaceShapeAnimations.LefteyebrowLowerer)
                    {
                        leftValue = animUnit.Value;
                        // StatusText = string.Format("LeftEyebrow: {0}", val);

                    }

                    if (animUnit.Key == FaceShapeAnimations.RighteyebrowLowerer)
                    {
                        rightValue = animUnit.Value;
                        //StatusText = string.Format("RightEyebrow: {0}", val);

                    }

                    _rightBrowDelta = rightValue > _rightBrow ?  rightValue / _rightBrow : _rightBrow / rightValue ;
                    _leftBrowDelta = leftValue > _leftBrow ? leftValue / _leftBrow : _leftBrow /leftValue ;

                    if (_rightBrowDelta >= .5 && _leftBrowDelta >= .5 )
                    {
                        OnEyebrowsDrawnUpArrived(new EyebrowsDrawnUpArrivedEventArgs(){ Confidence= 1.0f });

                    }
                    _rightBrow = rightValue;
                    _leftBrow = leftValue;

                }
            }
        }

        void _faceReader_FrameArrived(FaceFrameReader sender, FaceFrameArrivedEventArgs args)
        {
            //now look for loss of eyecontact and extended blinking
            //get a face frame
            using (var faceFrame = args.FrameReference.AcquireFrame())
            {
                if (faceFrame != null )
                {
                    //we have a face frame first check for loss of eye contact...
                    
                    // Retrieve the face frame result
                    FaceFrameResult frameResult = faceFrame.FaceFrameResult;

                    //We'll use the Engaged property along with the Looking Away to determine if user has lost eye contact
                    //Note---- This does not do eye tracking so if the uses simply moves they eyes without moving their head the data will not be recorded!!
                    //TODO: use openCV or HDFace to track eye movement
                   var userIsEngaged =  frameResult.FaceProperties[FaceProperty.Engaged];
                   var lookingAway = frameResult.FaceProperties[FaceProperty.LookingAway];

                    if (userIsEngaged == DetectionResult.No && lookingAway == DetectionResult.Yes  )
                    {
                        //here we can use a more customized algorithm to time it and look for false positives.
                        //but for this demo we will simply raise the LossEyeContact Event
                        OnLossOfEyeContactArrived(new LossOfEyeContactArrivedEventArgs() { Confidence = 1.0f });

                        //no need to check for eyes becuase user is looking away
                        return;
                    }
                   
                    //For extended eye blinks we'll need some timer so I'll use GetTickCounts
                    //when both eyes are closed

                    var isLeftEyeClosed = frameResult.FaceProperties[FaceProperty.LeftEyeClosed] == DetectionResult.Yes;
                    var isRightEyeClosed = frameResult.FaceProperties[FaceProperty.RightEyeClosed] == DetectionResult.Yes ;
                    var bothEyesClosed = isLeftEyeClosed && isRightEyeClosed;

                    if (bothEyesClosed && _eyesState == EyesState.Opened )
                    {
                        _eyeClosedTickCount = System.Environment.TickCount;
                        _eyesState = EyesState.Closed;
                    }
                    else if ( _eyesState == EyesState.Closed )
                    {
                        var differenceTickCount = System.Environment.TickCount - _eyeClosedTickCount;
                        if (differenceTickCount > 40)
                       {
                            //Raise an extended eye blink
                            OnExtendedBlinkArrived(new ExtendedBlinkArrivedEventArgs() { Confidence = 1.0f });
                            
                        }
                        // set _eyesState  start
                        _eyesState = EyesState.Opened;
                        
                    }
                    else
                    {
                        // set _eyesState  start
                        _eyesState = EyesState.Opened;
                    }

                    

                }
            }
            
        }

        public event EyebrowsDrawnUpArrivedEventHandler EyebrowsDrawnUpArrived;


        protected virtual void OnEyebrowsDrawnUpArrived(EyebrowsDrawnUpArrivedEventArgs e)
        {
            if (EyebrowsDrawnUpArrived != null)
            {
                EyebrowsDrawnUpArrived(this, e);
            }

        }

        public event GulpingArrivedEventHandler GulpingArrived;


        protected virtual void OnGulpingArrived(GulpingArrivedEventArgs e)
        {
            if (GulpingArrived != null)
            {
                GulpingArrived(this, e);
            }

        }

        public event ExtendedBlinkArrivedEventHandler ExtendedBlinkArrived;


        protected virtual void OnExtendedBlinkArrived(ExtendedBlinkArrivedEventArgs e)
        {
            if (ExtendedBlinkArrived != null)
            {
                ExtendedBlinkArrived(this, e);
            }

        }

        public event LossOfEyeContactArrivedEventHandler LossOfEyeContactArrived;


        protected virtual void OnLossOfEyeContactArrived(LossOfEyeContactArrivedEventArgs e)
        {
            if (LossOfEyeContactArrived != null)
            {
                LossOfEyeContactArrived(this, e);
            }

        }

        void _msReader_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        {
            using (var msFrame = args.FrameReference.AcquireFrame() )
            {
                if (msFrame != null)
                {
                    // get body 
                    using (var bodyFrame = msFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null)
                        {
                            // now get tracking Id from body
                            Body[] pBodies = new Body[bodyFrame.BodyCount];

                            bodyFrame.GetAndRefreshBodyData(pBodies);
                            foreach(var body in pBodies)
                            {
                                if (body.IsTracked && _trackingId == 0 )
                                {
                                    _trackingId = body.TrackingId;
                                    
                                    //now set the face tracking to determine eye blinking and loss of eyecontact
                                    _faceSource.TrackingId = _trackingId;
                                    _hdSource.TrackingId = _trackingId;

                                }
                            }

                            
                           
                        }
                        else
                            _trackingId = 0;
                    }
                }
            }
        }

       
    }
    public class EyebrowsDrawnUpArrivedEventArgs : FaceExpressionsArrivedEventArgs 
    {
       
    }

    public class ExtendedBlinkArrivedEventArgs : FaceExpressionsArrivedEventArgs
    { }

    public class LossOfEyeContactArrivedEventArgs :
        FaceExpressionsArrivedEventArgs
    { }

    public class GulpingArrivedEventArgs : FaceExpressionsArrivedEventArgs
    { }

    public class FaceExpressionsArrivedEventArgs: baseAnalysisEventArgs
    {
       
    }

    public enum EyesState
    {
        Closed,
        Opened,
        Start
    }
}

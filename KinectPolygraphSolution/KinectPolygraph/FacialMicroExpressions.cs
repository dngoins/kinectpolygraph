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

  

    public class FacialMicroExpressions : baseAnalysis
    {
        KinectSensor _sensor;
        private ulong _trackingId;
        private MultiSourceFrame _sourceFrame;
        private MultiSourceFrameReader _msReader;
        private FaceFrameSource _faceSource;
        private FaceFrameReader _faceReader;
        private int _eyeClosedTickCount;
        private EyesState _eyesState;
        private FaceAlignment _faceAlignment;
        private HighDefinitionFaceFrameSource _hdSource;
        private HighDefinitionFaceFrameReader _hdReader;
        private float[] _leftBrowDelta;
        private float[] _rightBrowDelta;
        private float[] _leftBrow;
        private float[] _rightBrow;
        private int browToleranceCount;
        private int mouthCoverToleranceCount;
        private int lookAwayToleranceCount;
        
        private bool _captureStarted;
        private int ndx; 
        public FacialMicroExpressions(KinectSensor sensor)
        {
            _sensor = sensor;
          //  _msReader = source;
            _eyesState = EyesState.Opened;
            this._faceAlignment = new FaceAlignment();
            this._leftBrow = new float[30];
            this._rightBrow = new float[30];
            this._leftBrowDelta = new float[30];
            this._rightBrowDelta = new float[30];

            _faceSource = new FaceFrameSource(_sensor, 0, FaceFrameFeatures.FaceEngagement | FaceFrameFeatures.LeftEyeClosed | FaceFrameFeatures.LookingAway | FaceFrameFeatures.FaceEngagement | FaceFrameFeatures.Happy | FaceFrameFeatures.MouthMoved | FaceFrameFeatures.MouthOpen | FaceFrameFeatures.RightEyeClosed | FaceFrameFeatures.RotationOrientation);
            _faceReader = _faceSource.OpenReader();
            _faceReader.FrameArrived += _faceReader_FrameArrived;
            // _msReader.MultiSourceFrameArrived += _msReader_MultiSourceFrameArrived;

            //TODO: Use HDFace to dermine gulping, Eyebrows
            _hdSource = new HighDefinitionFaceFrameSource(_sensor);
            _hdReader = _hdSource.OpenReader();
            _hdReader.FrameArrived += _hdReader_FrameArrived;
            
            
        }

        public void stopCapture()
        {
            _captureStarted = false;
        }

        public void startCapture(Body body)
        {
            if (_captureStarted) return;

            _captureStarted = true;
            if (body.IsTracked && _trackingId == 0)
            {
                _trackingId = body.TrackingId;

                //now set the face tracking to determine eye blinking and loss of eyecontact
                _faceSource.TrackingId = _trackingId;
                _hdSource.TrackingId = _trackingId;

            }
            
           
        }

        void _hdReader_FrameArrived(HighDefinitionFaceFrameReader sender, HighDefinitionFaceFrameArrivedEventArgs args)
        {

            using (var hdFaceFrame = args.FrameReference.AcquireFrame())
            {
                if (hdFaceFrame != null && _hdSource.TrackingId != 0)
                {
                    hdFaceFrame.GetAndRefreshFaceAlignmentResult(this._faceAlignment);
                    var animationUnits = this._faceAlignment.AnimationUnits;
                    
                    if (_faceAlignment.Quality == FaceAlignmentQuality.High)
                    {
                        foreach (var animUnit in animationUnits)
                        {
                           
                            if (animUnit.Key == FaceShapeAnimations.LefteyebrowLowerer)
                            {
                                _leftBrow[ndx] = animUnit.Value;

                            }

                            if (animUnit.Key == FaceShapeAnimations.RighteyebrowLowerer)
                            {
                                _rightBrow[ndx] = animUnit.Value;

                            }
                            ndx++;
                            if (ndx == 30)
                            {
                                ndx = 0;
                                //get average brow movements
                                var leftBrowMovementSum = 0.0f;
                                var rightBrowMovementSum = 0.0f;
                                for (int i = 0; i < 30; i++ )
                                {
                                    leftBrowMovementSum+= _leftBrow[i];
                                    rightBrowMovementSum += _rightBrow[i];
                                    
                                }
                                _rightBrowDelta[0] = _rightBrowDelta[1];
                                _leftBrowDelta[0] = _leftBrowDelta[1];
                                _rightBrowDelta[1] = rightBrowMovementSum / 30;
                                _leftBrowDelta[1] = leftBrowMovementSum / 30;                   

                            }

                            var rightBrowDiff = Math.Abs(_rightBrowDelta[1] * _rightBrowDelta[1] - _rightBrowDelta[0] * _rightBrowDelta[0]);
                            var leftBrowDiff = Math.Abs(_leftBrowDelta[1] * _leftBrowDelta[1] - _leftBrowDelta[0] * _leftBrowDelta[0]);

                            if (leftBrowDiff > 0.0015 && rightBrowDiff > 0.0015)
                            {
                                browToleranceCount++;
                                if (browToleranceCount > 350)
                                {
                                    OnEyebrowsDrawnUpArrived(new EyebrowsDrawnUpArrivedEventArgs() { Confidence = 1.0f });
                                    browToleranceCount = 0;
                                }
                            }
                           

                        }
                    }
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
                        lookAwayToleranceCount++;
                        if (lookAwayToleranceCount >= 30)
                        {
                            OnLossOfEyeContactArrived(new LossOfEyeContactArrivedEventArgs() { Confidence = 1.0f });
                            lookAwayToleranceCount = 0;
                        }
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

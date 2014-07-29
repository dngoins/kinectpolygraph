using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace KinectPolygraph
{
    public delegate void IncongruenceArrivedEventHandler(object sender, IncongruenceArrivedEventArgs args);
        
    public delegate void NoseTouchArrivedEventHandler(object sender, NoseTouchArrivedEventArgs args);

    public delegate void MouthCoverArrivedEventHandler(object sender, MouthCoverArrivedEventArgs args);

    public class BodyAnalysis : baseAnalysis
    {
        private KinectSensor _sensor;
        private MultiSourceFrameReader _msReader;
        private CoordinateMapper _coordinateMapper;
        private FaceFrameReader _faceReader;
        private FaceFrameSource _faceSource;
        private bool _isCapturing;
        private HighDefinitionFaceFrameSource _hdSource;
        private HighDefinitionFaceFrameReader _hdReader;
        private FaceAlignment _faceAlignment;
        private ushort[] depthFrameData = null;
        private int _depthWidth = 0;
        private int MouthCoverToleranceCount;
        private int TouchNoseToleranceCount;
        public BodyAnalysis(KinectSensor sensor)
        {
            _sensor = sensor;
            FrameDescription depthFrameDescription = _sensor.DepthFrameSource.FrameDescription;

            _depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            // allocate space to put the pixels being received and converted
            this.depthFrameData = new ushort[_depthWidth * depthHeight];
            //_msReader = reader;
            //_msReader.MultiSourceFrameArrived += _msReader_MultiSourceFrameArrived;
            //reader.FrameArrived += reader_FrameArrived;
            _coordinateMapper = _sensor.CoordinateMapper;
            _faceSource = new FaceFrameSource(_sensor, 0, FaceFrameFeatures.BoundingBoxInColorSpace | FaceFrameFeatures.BoundingBoxInInfraredSpace | FaceFrameFeatures.FaceEngagement | FaceFrameFeatures.Glasses | FaceFrameFeatures.Happy | FaceFrameFeatures.LeftEyeClosed | FaceFrameFeatures.LookingAway | FaceFrameFeatures.MouthMoved | FaceFrameFeatures.MouthOpen | FaceFrameFeatures.PointsInColorSpace | FaceFrameFeatures.PointsInInfraredSpace | FaceFrameFeatures.RightEyeClosed | FaceFrameFeatures.RotationOrientation );
            _faceReader = _faceSource.OpenReader();
            _faceReader.FrameArrived += _faceReader_FrameArrived;
            _faceAlignment = new FaceAlignment();

            _hdSource = new HighDefinitionFaceFrameSource(_sensor);
            _hdReader = _hdSource.OpenReader();
            _hdReader.FrameArrived += _hdReader_FrameArrived;
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
                               // _leftBrow[ndx] = animUnit.Value;

                            }

                            if (animUnit.Key == FaceShapeAnimations.RighteyebrowLowerer)
                            {
                               // _rightBrow[ndx] = animUnit.Value;

                            }
                            //ndx++;
                            //if (ndx == 200) ndx = 0;
                            ////  _rightBrowDelta = _rightBrow /rightValue;
                            //   _leftBrowDelta = _leftBrow / leftValue;

                            //if (( (1.0f > _rightBrowDelta ) && (_rightBrowDelta >= .5f ) )&& ((1.0f > _leftBrowDelta) && (_leftBrowDelta >= .5f)))
                            //{
                            //    OnEyebrowsDrawnUpArrived(new EyebrowsDrawnUpArrivedEventArgs() { Confidence = 1.0f });

                            //}
                            //  _rightBrow = rightValue;
                            // _leftBrow = leftValue;

                        }
                    }
                }
            }
        }


        public void startCapture(Body body)
        {
            if (_isCapturing) return;

            _isCapturing = true;
            if (body.IsTracked)
            {
                _faceSource.TrackingId = body.TrackingId;
                return;
            }
        }
        void reader_FrameArrived(BodyFrameReader sender, BodyFrameArrivedEventArgs args)
        {
            using (var bodyFrame = args.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    Body[] bodies = new Body[bodyFrame.BodyCount];
                    foreach (var body in bodies)
                    {
                        if (body == null) continue;
                        if (body.IsTracked)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                            return;
                        }
                    }

                }
            }
        }

        void _faceReader_FrameArrived(FaceFrameReader sender, FaceFrameArrivedEventArgs args)
        {
            using(var faceFrame = args.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    using (var bodyFrame = faceFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null)
                        {
                           using(var depthFrame = faceFrame.DepthFrameReference.AcquireFrame())
                           {

                               if (depthFrame != null)
                               {
                                   depthFrame.CopyFrameDataToArray(this.depthFrameData);
                               }
                           }
                            
                            //get hands and check for touching nose and/or covering mouth
                            Body[] bodies = new Body[bodyFrame.BodyCount];
                            bodyFrame.GetAndRefreshBodyData(bodies);
                            foreach(var body in bodies)
                            {
                                if (body.IsTracked )
                                {
                                    //get nose and mouth positions
                                    var noseLocation = faceFrame.FaceFrameResult.FacePointsInInfraredSpace[FacePointType.Nose];

                                    var mouthLeftLocation = faceFrame.FaceFrameResult.FacePointsInInfraredSpace[FacePointType.MouthCornerLeft];

                                    var mouthRightLocation = faceFrame.FaceFrameResult.FacePointsInInfraredSpace[FacePointType.MouthCornerRight];
                                    //get the depthSpacePoint from the noseLocation
                                    // calculate index into depth array
                                    int noseDepthIndex = ((int)noseLocation.Y * _depthWidth) + (int)noseLocation.X;
                                    int mouthLeftIndex = ((int)mouthLeftLocation.Y * _depthWidth) + (int)mouthLeftLocation.X;

                                    int mouthRightIndex = ((int)mouthRightLocation.Y * _depthWidth) + (int)mouthRightLocation.X;

                                    var depthSpaceNosePoint = new DepthSpacePoint() { X = (float)noseLocation.X, Y = (float)noseLocation.Y };

                                    var depthSpaceMouthLeftPoint = new DepthSpacePoint() { X = (float)mouthLeftLocation.X, Y = (float)mouthLeftLocation.Y };

                                    var depthSpaceMouthRightPoint = new DepthSpacePoint() { X = (float)mouthRightLocation.X, Y = (float)mouthRightLocation.Y };

                                    var noseCamera = _coordinateMapper.MapDepthPointToCameraSpace(depthSpaceNosePoint, depthFrameData[noseDepthIndex]);

                                    var mouthLeftCamera = _coordinateMapper.MapDepthPointToCameraSpace(depthSpaceMouthLeftPoint, depthFrameData[mouthLeftIndex]);
                                    var mouthRightCamera = _coordinateMapper.MapDepthPointToCameraSpace(depthSpaceMouthRightPoint, depthFrameData[mouthRightIndex]);

                                            var leftHandLocation = body.Joints[JointType.HandTipLeft].Position;
                                    var rightHandLocation = body.Joints[JointType.HandTipRight].Position;


                                    if (noseCamera.X != 0 || noseCamera.Y != 0)
                                    {

                                        var isLeftHandTouchingNose = (Math.Abs((leftHandLocation.X * leftHandLocation.X - noseCamera.X * noseCamera.X)) <= 0.01f) && (Math.Abs((leftHandLocation.Y * leftHandLocation.Y - noseCamera.Y * noseCamera.Y)) <= 0.01f) && (Math.Abs((leftHandLocation.Z * leftHandLocation.Z - noseCamera.Z * noseCamera.Z)) <= 0.010f);

                                        var isRightHandTouchingNose = (Math.Abs((rightHandLocation.X * rightHandLocation.X - noseCamera.X * noseCamera.X)) <= 0.01f) && (Math.Abs((rightHandLocation.Y * rightHandLocation.Y - noseCamera.Y * noseCamera.Y)) <= 0.01f) && (Math.Abs((rightHandLocation.Z * rightHandLocation.Z - noseCamera.Z * noseCamera.Z)) <= 0.010f);

                                        if (isLeftHandTouchingNose || isRightHandTouchingNose)
                                        {
                                            TouchNoseToleranceCount++;
                                            if (TouchNoseToleranceCount >= 1)
                                            {
                                                OnNoseTouchArrived(new NoseTouchArrivedEventArgs() { Confidence = .90f });
                                                TouchNoseToleranceCount = 0;
                                            }
                                        }
                                    }

                                    if (mouthLeftCamera.X != 0 || mouthLeftCamera.Y != 0 || mouthRightCamera.X != 0 || mouthRightCamera.Y != 0)
                                    {

                                        var isLeftHandTouchingMouthLeft = (Math.Abs((leftHandLocation.X * leftHandLocation.X - mouthLeftCamera.X * mouthLeftCamera.X)) <= 0.01f) && (Math.Abs((leftHandLocation.Y * leftHandLocation.Y - mouthLeftCamera.Y * mouthLeftCamera.Y)) <= 0.01f) && (Math.Abs((leftHandLocation.Z * leftHandLocation.Z - mouthLeftCamera.Z * mouthLeftCamera.Z)) <= 0.010f);

                                        var isRightHandTouchingMouthLeft = (Math.Abs((rightHandLocation.X * rightHandLocation.X - mouthLeftCamera.X * mouthLeftCamera.X)) <= 0.01f) && (Math.Abs((rightHandLocation.Y * rightHandLocation.Y - mouthLeftCamera.Y * mouthLeftCamera.Y)) <= 0.01f) && (Math.Abs((rightHandLocation.Z * rightHandLocation.Z - mouthLeftCamera.Z * mouthLeftCamera.Z)) <= 0.010f);

                                        var isLeftHandTouchingMouthRight = (Math.Abs((leftHandLocation.X * leftHandLocation.X - mouthRightCamera.X * mouthRightCamera.X)) <= 0.01f) && (Math.Abs((leftHandLocation.Y * leftHandLocation.Y - mouthRightCamera.Y * mouthRightCamera.Y)) <= 0.01f) && (Math.Abs((leftHandLocation.Z * leftHandLocation.Z - mouthRightCamera.Z * mouthRightCamera.Z)) <= 0.010f);

                                        var isRightHandTouchingMouthRight = (Math.Abs((rightHandLocation.X * rightHandLocation.X - mouthRightCamera.X * mouthRightCamera.X)) <= 0.01f) && (Math.Abs((rightHandLocation.Y * rightHandLocation.Y - mouthRightCamera.Y * mouthRightCamera.Y)) <= 0.01f) && (Math.Abs((rightHandLocation.Z * rightHandLocation.Z - mouthRightCamera.Z * mouthRightCamera.Z)) <= 0.010f);

                                        if (isLeftHandTouchingMouthRight || isRightHandTouchingMouthRight || isLeftHandTouchingMouthLeft || isRightHandTouchingMouthLeft)
                                        {
                                            MouthCoverToleranceCount++;
                                            if (MouthCoverToleranceCount >= 1)
                                            {
                                                OnMouthCoverArrived(new MouthCoverArrivedEventArgs() { Confidence = .90f });
                                                MouthCoverToleranceCount = 0;
                                            }

                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }

        void _msReader_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        {
            using (var msFrame = args.FrameReference.AcquireFrame())
            {
                if (msFrame != null )
                {
                    using (var bodyFrame = msFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null)
                        {
                            Body[] bodies = new Body[bodyFrame.BodyCount];
                            foreach(var body in bodies)
                            {
                                if (body == null) continue; 
                                if (body.IsTracked )
                                {
                                    _faceSource.TrackingId = body.TrackingId;
                                    return;
                                }
                            }
                            
                        }
                    }
                }
            }
        }
        public event IncongruenceArrivedEventHandler IncongruenceArrived;


        protected virtual void OnIncongruenceArrived(IncongruenceArrivedEventArgs e)
        {
            if (IncongruenceArrived != null)
            {
                IncongruenceArrived(this, e);
            }

        }

        public event NoseTouchArrivedEventHandler NoseTouchArrived;


        protected virtual void OnNoseTouchArrived(NoseTouchArrivedEventArgs e)
        {
            if (NoseTouchArrived != null)
            {
                NoseTouchArrived(this, e);
            }

        }
        
        public event MouthCoverArrivedEventHandler MouthCoverArrived;


        protected virtual void OnMouthCoverArrived(MouthCoverArrivedEventArgs e)
        {
            if (MouthCoverArrived != null)
            {
                MouthCoverArrived(this, e);
            }

        }

    }

    public class NoseTouchArrivedEventArgs: BodyAnalysisEventArgs
    { }

    public class IncongruenceArrivedEventArgs : BodyAnalysisEventArgs { }

    public class MouthCoverArrivedEventArgs : BodyAnalysisEventArgs
    { }

    public class BodyAnalysisEventArgs: baseAnalysisEventArgs
    { }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;
using Microsoft.Kinect;


namespace KinectPolygraph
{
    public delegate void IncongruenceArrivedEventHandler(object sender, IncongruenceArrivedEventArgs args);
        
    public delegate void NoseTouchArrivedEventHandler(object sender, NoseTouchArrivedEventArgs args);

    public delegate void MouthCoverArrivedEventHandler(object sender, MouthCoverArrivedEventArgs args);

    public class BodyAnalysis : baseAnalysis
    {
        private KinectSensor _sensor;
        public BodyAnalysis(KinectSensor sensor)
        {
            _sensor = sensor;
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

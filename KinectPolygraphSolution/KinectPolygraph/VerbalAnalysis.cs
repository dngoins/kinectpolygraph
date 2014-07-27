using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;
using Microsoft.Kinect;


namespace KinectPolygraph
{
    public delegate void RepeatExactWordsArrivedEventHandler(object sender, RepeatExactWordsArrivedEventArgs args);

    public delegate void AvoidContractionsArrivedEventHandler(object sender, AvoidContractionsArrivedEventArgs args);

    public delegate void NegativePositiveAssertionsArrivedEventHandler(object sender, NegativePositiveAssertionsArrivedEventArgs args);

 
    public class VerbalAnalysis : baseAnalysis
    {
        KinectSensor _sensor;
        public VerbalAnalysis(KinectSensor sensor)
        {
            _sensor = sensor;
        }

        public event RepeatExactWordsArrivedEventHandler RepeatExactWordsArrived;


        protected virtual void OnRepeatExactWordsArrived(RepeatExactWordsArrivedEventArgs e)
        {
            if (RepeatExactWordsArrived != null)
            {
                RepeatExactWordsArrived(this, e);
            }

        }

        public event AvoidContractionsArrivedEventHandler AvoidContractionsArrived;


        protected virtual void OnAvoidContractionsArrived(AvoidContractionsArrivedEventArgs e)
        {
            if (AvoidContractionsArrived != null)
            {
                AvoidContractionsArrived(this, e);
            }

        }

        public event NegativePositiveAssertionsArrivedEventHandler NegativePositiveAssertionsArrived;


        protected virtual void OnNegativePositiveAssertionsArrived(NegativePositiveAssertionsArrivedEventArgs e)
        {
            if (NegativePositiveAssertionsArrived != null)
            {
                NegativePositiveAssertionsArrived(this, e);
            }

        }

     
    }

    public class RepeatExactWordsArrivedEventArgs : VerbalAnalysisEventArgs
    { }


    public class NegativePositiveAssertionsArrivedEventArgs:VerbalAnalysisEventArgs
    { }

    public class AvoidContractionsArrivedEventArgs : VerbalAnalysisEventArgs
    { }
    public class VerbalAnalysisEventArgs: baseAnalysisEventArgs
    { }
}

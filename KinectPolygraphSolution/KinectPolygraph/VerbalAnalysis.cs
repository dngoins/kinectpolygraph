using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;
using Microsoft.Kinect;
using AForge.Math;


namespace KinectPolygraph
{
    public delegate void RepeatExactWordsArrivedEventHandler(object sender, RepeatExactWordsArrivedEventArgs args);

    public delegate void AvoidContractionsArrivedEventHandler(object sender, AvoidContractionsArrivedEventArgs args);

    public delegate void NegativePositiveAssertionsArrivedEventHandler(object sender, NegativePositiveAssertionsArrivedEventArgs args);

    public delegate void SpeechPauseArrivedEventHandler(object sender, SpeechPauseArrivedEventArgs args);

    public class VerbalAnalysis : baseAnalysis
    {
        KinectSensor _sensor;
        AudioSource _audioSource;
        AudioBeamFrameReader _audioReader;
        byte[] _audioBuffer;
        private const int _BytesPerSample = sizeof(float);
        private float _accumulatedSquareSum;
        private int _accumulatedSampleCount;
        private int _energyIndex;
        private const int _SamplesPerMillisecond = 16;

        private const int _SamplesPerColumn = 40;

        private const int _MinEnergy = -90;
        private float _previousAvgSampleDecibel;
        
        private const int _EnergyBitmapWidth = 780;
        private readonly object energyLock = new object();
        private readonly float[] energy = new float[(uint)(_EnergyBitmapWidth * 1.25)];
        private int energyIndex;

        private int _newEnergyAvailable;

        private float _energyError;
               
        private DateTime? _lastEnergyRefreshTime;

        public VerbalAnalysis(AudioSource sensorSource)
        {
            //_sensor = sensor;
            _audioSource = sensorSource;
            _audioReader = _audioSource.OpenReader();
            _audioReader.FrameArrived += _audioReader_FrameArrived;
            _audioBuffer = new byte[_audioSource.SubFrameLengthInBytes];

        }

        void _audioReader_FrameArrived(AudioBeamFrameReader sender, AudioBeamFrameArrivedEventArgs args)
        {
            var frameList = args.FrameReference.AcquireBeamFrames();
            
            if(frameList != null)
            {
                // Only one audio beam is supported. Get the sub frame list for this beam
                IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                // Loop over all sub frames, extract audio buffer and beam information
                foreach (AudioBeamSubFrame subFrame in subFrameList)
                {
                    // Check if beam angle and/or confidence have changed
                    
                    // Process audio buffer
                    subFrame.CopyFrameDataToArray(this._audioBuffer);

                     float audioSampleTotal = 0.0f;
                    for (int i = 0; i < this._audioBuffer.Length; i += _BytesPerSample)
                    {
                        // Extract the 32-bit IEEE float sample from the byte array
                        float audioSample = BitConverter.ToSingle(this._audioBuffer, i);

                        this._accumulatedSquareSum += audioSample * audioSample;
                        ++this._accumulatedSampleCount;
                        audioSampleTotal += System.Math.Abs(audioSample);

                        if (this._accumulatedSampleCount < _SamplesPerColumn)
                        {
                            continue;
                        }

                        float meanSquare = this._accumulatedSquareSum / _SamplesPerColumn;

                        if (meanSquare > 1.0f)
                        {
                            // A loud audio source right next to the sensor may result in mean square values
                            // greater than 1.0. Cap it at 1.0f for display purposes.
                            meanSquare = 1.0f;
                        }

                        // Calculate energy in dB, in the range [MinEnergy, 0], where MinEnergy < 0
                        float energy = _MinEnergy;

                        if (meanSquare > 0)
                        {
                            energy = (float)(10.0 * Math.Log10(meanSquare));
                        }
                       

                        lock (this.energyLock)
                        {
                            // Normalize values to the range [0, 1] for display
                            this.energy[this._energyIndex] = (_MinEnergy - energy) / _MinEnergy;
                            this._energyIndex = (this._energyIndex + 1) % this.energy.Length;
                            ++this._newEnergyAvailable;
                        }

                        this._accumulatedSquareSum = 0;
                        this._accumulatedSampleCount = 0;
                    }
                     var currentAveSample = audioSampleTotal / _accumulatedSampleCount;
                    if(_previousAvgSampleDecibel != 0)
                    {
                        //if previous sample is more than 50% difference - speech is being paused
                        var difference = currentAveSample / _previousAvgSampleDecibel; 
                        if (difference   > .05 )
                        {
                            OnSpeechPauseArrived(new SpeechPauseArrivedEventArgs());
                        }
                    }
                     _previousAvgSampleDecibel = currentAveSample;

                }

            }
        }

        public event RepeatExactWordsArrivedEventHandler RepeatExactWordsArrived;


        protected virtual void OnRepeatExactWordsArrived(RepeatExactWordsArrivedEventArgs e)
        {
            if (RepeatExactWordsArrived != null)
            {
                RepeatExactWordsArrived(this, e);
            }

        }

        public event SpeechPauseArrivedEventHandler SpeechPauseArrived;


        protected virtual void OnSpeechPauseArrived(SpeechPauseArrivedEventArgs e)
        {
            if (SpeechPauseArrived != null)
            {
                SpeechPauseArrived(this, e);
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

    public class SpeechPauseArrivedEventArgs : VerbalAnalysisEventArgs
    { }

    public class NegativePositiveAssertionsArrivedEventArgs:VerbalAnalysisEventArgs
    { }

    public class AvoidContractionsArrivedEventArgs : VerbalAnalysisEventArgs
    { }
    public class VerbalAnalysisEventArgs: baseAnalysisEventArgs
    { }
}

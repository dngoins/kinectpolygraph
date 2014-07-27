#pragma once

#include "Kinect.h"

namespace KinectSpeechRT
{
    public ref class KinectSpeech sealed
    {
    public:
        KinectSpeech(WindowsPreview::Kinect::KinectSensor ^ sensor);
    };
}
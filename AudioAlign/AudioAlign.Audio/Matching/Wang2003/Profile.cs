﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    /// <summary>
    ///  The basic configuration profile for the Wang fingerprinting algorithm.
    ///  
    ///  This profile contains settings for configuring fingerprint generation
    ///  and lookup.
    /// </summary>
    public class Profile {

        public interface IThreshold {
            /// <summary>
            /// Returns, for a given x (time), a threshold value in the range of [0,1].
            /// </summary>
            /// <param name="x">the time instant to calculate the threshold for</param>
            /// <returns>the threshold for the given time instant</returns>
            double Calculate(double x);
        }

        /// <summary>
        /// An exponentially decaying threshold in the form of y=b^x.
        /// </summary>
        public class ExponentialDecayThreshold : IThreshold {
            public double Base { get; set; }
            public double WidthScale { get; set; }
            public double Height { get; set; }

            public double Calculate(double x) {
                return Math.Pow(Base, x / WidthScale) * Height;
            }
        }

        public Profile() {
            SamplingRate = 11025;
            WindowSize = 512;
            HopSize = 256;
            SpectrumTemporalSmoothingCoefficient = 0.05f;
            SpectrumSmoothingLength = 0;
            PeaksPerFrame = 3;
            PeakFanout = 5;
            TargetZoneDistance = 2;
            TargetZoneLength = 30;
            TargetZoneWidth = 63;

            double framesPerSecond = (double)SamplingRate / HopSize;
            MatchingMinFrames = 10;
            MatchingMaxFrames = (int)(framesPerSecond * 30);

            var threshold = new ExponentialDecayThreshold {
                Base = 0.5,
                WidthScale = 2,
                Height = 0.3
            };
            ThresholdAccept = threshold;
            ThresholdReject = new ExponentialDecayThreshold {
                Base = threshold.Base,
                WidthScale = threshold.WidthScale,
                Height = threshold.Height / 6
            };
        }

        /// <summary>
        /// The sampling rate at which the STFT is taken to generate the hashes.
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// The STFT window size in samples, which is the double of the spectral frame resolution.
        /// </summary>
        public int WindowSize { get; set; }

        /// <summary>
        /// The distance in samples from one STFT window to the next. Can be overlapping.
        /// </summary>
        public int HopSize { get; set; }

        /// <summary>
        /// The alpha coefficient for the exponential moving average of the spectrum bins over time.
        /// Subtracting this average from a spectral frame results in the residual spectrum from
        /// which the peaks are extracted.
        /// </summary>
        public float SpectrumTemporalSmoothingCoefficient { get; set; }

        /// <summary>
        /// The length in bins of the simple moving average filter to smooth a spectral frame.
        /// Can be used to low pass filter the spectrum and get rid of small peaks, but also 
        /// shifts the peaks a bit. Set to zero to disable.
        /// </summary>
        public int SpectrumSmoothingLength { get; set; }

        /// <summary>
        /// The maximum number of peaks to extract from a spectral frame.
        /// </summary>
        public int PeaksPerFrame { get; set; }

        /// <summary>
        /// The maximum number of peak pairs to generate for a peak. The peak pairs are what ultimately
        /// results in the fingerprint hashes.
        /// </summary>
        public int PeakFanout { get; set; }

        /// <summary>
        /// The temporal distance in frames from a peak to its target zone whose peaks are taken to build peak pairs.
        /// </summary>
        public int TargetZoneDistance { get; set; }

        /// <summary>
        /// The temporal length in frames of the target peak zone.
        /// </summary>
        public int TargetZoneLength { get; set; }

        /// <summary>
        /// The spectral width in peaks of the target peak zone.
        /// </summary>
        public int TargetZoneWidth { get; set; }

        /// <summary>
        /// The minimum length in frames to classify a match.
        /// </summary>
        public int MatchingMinFrames { get; set; }

        /// <summary>
        /// The maximum number of frames to scan for a match.
        /// </summary>
        public int MatchingMaxFrames { get; set; }

        /// <summary>
        /// The threshold that a match candidate's rate needs to exceed to be classified as a match.
        /// </summary>
        public IThreshold ThresholdAccept { get; set; }

        /// <summary>
        /// The threshold that rejects a match candidate and stops the matching process if it's matching rate drops below.
        /// </summary>
        public IThreshold ThresholdReject { get; set; }
    }
}

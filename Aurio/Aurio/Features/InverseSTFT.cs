﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Streams;

namespace Aurio.Features {
    /// <summary>
    /// Inverse Short-Time Fourier Tranformation.
    /// Takes a number of raw FFT frames and converts them to a continuous audio stream by using the overlap-add method.
    /// </summary>
    public class InverseSTFT : OLA {

        private float[] frameBuffer;
        private PFFFT.PFFFT fft;

        public InverseSTFT(IAudioWriterStream stream, int windowSize, int hopSize) 
            : base(stream, windowSize, hopSize) {
            frameBuffer = new float[WindowSize];
            fft = new PFFFT.PFFFT(WindowSize, PFFFT.Transform.Real);
        }

        /// <summary>
        /// Writes a raw FFT result frame into the output audio stream.
        /// </summary>
        /// <param name="fftResult">raw FFT frame as output by STFT in OutputFormat.Raw mode</param>
        public override void WriteFrame(float[] fftResult) {
            if (fftResult.Length != WindowSize) {
                throw new ArgumentException("the provided FFT result array has an invalid size");
            }

            OnFrameWrittenInverseSTFT(fftResult);

            // do inverse fourier transform
            fft.Backward(fftResult, frameBuffer);

            base.WriteFrame(frameBuffer);
        }

        /// <summary>
        /// Flushes remaining buffered data to the output audio stream.
        /// </summary>
        public override void Flush() {
            base.Flush();
        }

        protected virtual void OnFrameWrittenInverseSTFT(float[] frame) {
            // to be overridden
        }
    }
}

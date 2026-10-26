using System;
using System.IO;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// A small deterministic synthesizer for the game's music and sounds: bells, plucked strings, a marimba, a soft pad and a
    /// flute-like lead made from sine partials, filtered noise, filters, a convolution reverb and a WAV writer. Everything is
    /// computed from numbers and seeded random generators, so every run produces identical bytes. No recordings or samples
    /// of any kind are used. All buffers are mono floats in [-1, 1].
    /// </summary>
    internal static class AudioSynth
    {
        const double TwoPi = Math.PI * 2.0;

        public static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        public static float[] Buffer(float seconds, int rate) => new float[Mathf.Max(1, Mathf.CeilToInt(seconds * rate))];

        /// <summary>Adds a sound into a buffer at a time (seconds). Past the end it is dropped, or wrapped round to the start for a loop.</summary>
        public static void Mix(float[] target, float[] source, float start, float gain, int rate, bool wrap = false)
        {
            int offset = Mathf.RoundToInt(start * rate);
            int n = target.Length;
            for (int i = 0; i < source.Length; i++)
            {
                int j = offset + i;
                if (wrap) j = ((j % n) + n) % n;
                else if (j < 0) continue;
                else if (j >= n) break;
                target[j] += source[i] * gain;
            }
        }

        static float Smooth(float x) => x <= 0f ? 0f : x >= 1f ? 1f : x * x * (3f - 2f * x);

        /// <summary>
        /// Fades the last stretch of a note to silence so a note that is cut short (an interface tick, a pluck in a short effect)
        /// never ends in a click. Notes that have already died away are not changed audibly.
        /// </summary>
        static float[] Release(float[] y, int rate)
        {
            int n = Mathf.Min(y.Length / 4, Mathf.CeilToInt(0.03f * rate));
            for (int k = 0; k < n; k++) y[y.Length - 1 - k] *= Smooth(k / (float)n);
            return y;
        }

        /// <summary>A gentle attack and release envelope, 0..1.</summary>
        static float Window(float t, float seconds, float attack, float release) =>
            Smooth(t / Mathf.Max(0.0005f, attack)) * Smooth((seconds - t) / Mathf.Max(0.0005f, release));

        // ---- Instruments -------------------------------------------------------------------------------------------------------

        /// <summary>A bright, glassy bell: a few partials that die away faster the higher they are.</summary>
        public static float[] Bell(float freq, float seconds, int rate, float brightness = 1f, float decay = 2.2f)
        {
            float[] y = Buffer(seconds, rate);
            float[] ratio = { 1f, 2.01f, 3.0f, 4.16f };
            float[] weight = { 1f, 0.38f, 0.17f, 0.08f };
            for (int p = 0; p < ratio.Length; p++)
            {
                float f = freq * ratio[p];
                if (f > rate * 0.45f) break;
                float amplitude = weight[p] * (p == 0 ? 1f : brightness);
                float d = decay * (1f + p * 0.9f);
                double phase = 0.0;
                double step = TwoPi * f / rate;
                for (int i = 0; i < y.Length; i++)
                {
                    float t = i / (float)rate;
                    y[i] += (float)Math.Sin(phase) * amplitude * Mathf.Exp(-t * d) * Smooth(t / 0.004f);
                    phase += step;
                }
            }
            return Release(y, rate);
        }

        /// <summary>A plucked string (harp-like): harmonics whose strength follows where the string was plucked, each decaying at its own rate.</summary>
        public static float[] Pluck(float freq, float seconds, int rate, float decay = 1.6f, float pluckPosition = 0.18f)
        {
            float[] y = Buffer(seconds, rate);
            int harmonics = Mathf.Min(10, Mathf.FloorToInt(rate * 0.45f / freq));
            for (int h = 1; h <= harmonics; h++)
            {
                float amplitude = Mathf.Abs(Mathf.Sin(h * Mathf.PI * pluckPosition)) / Mathf.Pow(h, 1.15f);
                float f = h * freq * Mathf.Sqrt(1f + 0.0004f * h * h);   // a hair of string stiffness
                float d = decay * (1f + 0.55f * (h - 1));
                double phase = 0.0;
                double step = TwoPi * f / rate;
                for (int i = 0; i < y.Length; i++)
                {
                    float t = i / (float)rate;
                    y[i] += (float)Math.Sin(phase) * amplitude * Mathf.Exp(-t * d) * Smooth(t / 0.002f);
                    phase += step;
                }
            }
            return Release(y, rate);
        }

        /// <summary>A mellow wooden mallet note: a strong fundamental with a quickly fading overtone.</summary>
        public static float[] Marimba(float freq, float seconds, int rate)
        {
            float[] y = Buffer(seconds, rate);
            float[] ratio = { 1f, 3.9f, 9.4f };
            float[] weight = { 1f, 0.32f, 0.08f };
            float[] decay = { 4.2f, 11f, 22f };
            for (int p = 0; p < ratio.Length; p++)
            {
                double phase = 0.0;
                double step = TwoPi * freq * ratio[p] / rate;
                for (int i = 0; i < y.Length; i++)
                {
                    float t = i / (float)rate;
                    y[i] += (float)Math.Sin(phase) * weight[p] * Mathf.Exp(-t * decay[p]) * Smooth(t / 0.003f);
                    phase += step;
                }
            }
            return Release(y, rate);
        }

        /// <summary>A tubular wind chime: a free-bar spectrum (partials at 1, 2.76 and 5.4 times the pitch) that rings long and thin.</summary>
        public static float[] Chime(float freq, float seconds, int rate, float decay = 1.1f)
        {
            float[] y = Buffer(seconds, rate);
            float[] ratio = { 1f, 2.76f, 5.40f };
            float[] weight = { 1f, 0.45f, 0.18f };
            for (int p = 0; p < ratio.Length; p++)
            {
                float f = freq * ratio[p];
                if (f > rate * 0.44f) break;
                float d = decay * (1f + 1.7f * p);
                double phase = 0.0;
                double step = TwoPi * f / rate;
                for (int i = 0; i < y.Length; i++)
                {
                    float t = i / (float)rate;
                    y[i] += (float)Math.Sin(phase) * weight[p] * Mathf.Exp(-t * d) * Smooth(t / 0.002f);
                    phase += step;
                }
            }
            return Release(y, rate);
        }

        /// <summary>A warm, slowly swelling chord: each note is three slightly detuned voices with a soft overtone.</summary>
        public static float[] Pad(float[] freqs, float seconds, int rate, float attack, float release, float detuneCents = 6f)
        {
            float[] y = Buffer(seconds, rate);
            float[] envelope = new float[y.Length];
            for (int i = 0; i < envelope.Length; i++) envelope[i] = Window(i / (float)rate, seconds, attack, release);

            float[] cents = { -detuneCents, 0f, detuneCents };
            float scale = 1f / (cents.Length * freqs.Length);
            foreach (float freq in freqs)
            {
                for (int v = 0; v < cents.Length; v++)
                {
                    float f = freq * Mathf.Pow(2f, cents[v] / 1200f);
                    double phase = v * 1.3;
                    double step = TwoPi * f / rate;
                    double wander = TwoPi * (0.13 + 0.04 * v) / rate;   // a slow, slight wander in pitch
                    for (int i = 0; i < y.Length; i++)
                    {
                        double drift = 1.0 + 0.0007 * Math.Sin(wander * i + v);
                        y[i] += (float)(Math.Sin(phase) + 0.22 * Math.Sin(2.0 * phase) + 0.09 * Math.Sin(3.0 * phase)) * envelope[i] * scale;
                        phase += step * drift;
                    }
                }
            }
            return y;
        }

        /// <summary>A soft, breathy, flute-like melody voice with vibrato that arrives after the note has started.</summary>
        public static float[] Lead(float freq, float seconds, int rate, float attack = 0.07f, float release = 0.35f, int seed = 1)
        {
            float[] y = Buffer(seconds, rate);
            System.Random random = new System.Random(seed);
            double phase = 0.0;
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)rate;
                float vibrato = Smooth((t - 0.2f) / 0.4f) * 0.006f * Mathf.Sin(2f * Mathf.PI * 5.2f * t);
                phase += TwoPi * freq * (1f + vibrato) / rate;
                float tone = (float)(Math.Sin(phase) + 0.16 * Math.Sin(2.0 * phase) + 0.05 * Math.Sin(3.0 * phase));
                float breath = ((float)random.NextDouble() * 2f - 1f) * 0.018f;
                y[i] = (tone + breath) * Window(t, seconds, attack, release);
            }
            return y;
        }

        /// <summary>A sine that glides from one pitch to another while it fades (pops, thumps, blips and chirps).</summary>
        public static float[] Sweep(float f0, float f1, float seconds, int rate, float decay)
        {
            float[] y = Buffer(seconds, rate);
            double phase = 0.0;
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)rate;
                float k = t / seconds;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                phase += TwoPi * f / rate;
                y[i] = (float)Math.Sin(phase) * Mathf.Exp(-t * decay) * Smooth(t / 0.002f) * Smooth((seconds - t) / 0.01f);
            }
            return y;
        }

        /// <summary>A short bird note: a glide with a quick wobble and a rounded envelope.</summary>
        public static float[] Chirp(float f0, float f1, float seconds, int rate, float wobbleHz = 38f, float wobbleDepth = 0.025f)
        {
            float[] y = Buffer(seconds, rate);
            double phase = 0.0;
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)rate;
                float k = t / seconds;
                float f = Mathf.Lerp(f0, f1, k) * (1f + wobbleDepth * Mathf.Sin(2f * Mathf.PI * wobbleHz * t));
                phase += TwoPi * f / rate;
                float env = Mathf.Sin(Mathf.PI * k);
                y[i] = (float)(Math.Sin(phase) + 0.12 * Math.Sin(2.0 * phase)) * env * env;
            }
            return y;
        }

        public static float[] Noise(float seconds, int rate, int seed)
        {
            System.Random random = new System.Random(seed);
            float[] y = Buffer(seconds, rate);
            for (int i = 0; i < y.Length; i++) y[i] = (float)random.NextDouble() * 2f - 1f;
            return y;
        }

        // ---- Filters and shaping ------------------------------------------------------------------------------------------------

        public enum Filter { LowPass, HighPass, BandPass }

        /// <summary>A two-pole filter (RBJ cookbook), applied in place.</summary>
        public static float[] Biquad(float[] x, Filter type, float freq, float q, int rate)
        {
            double w0 = TwoPi * Math.Min(freq, rate * 0.45) / rate;
            double cos = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2.0 * q);
            double b0, b1, b2;
            switch (type)
            {
                case Filter.LowPass: b0 = (1 - cos) / 2; b1 = 1 - cos; b2 = (1 - cos) / 2; break;
                case Filter.HighPass: b0 = (1 + cos) / 2; b1 = -(1 + cos); b2 = (1 + cos) / 2; break;
                default: b0 = alpha; b1 = 0; b2 = -alpha; break;
            }
            double a0 = 1 + alpha;
            double a1 = -2 * cos;
            double a2 = 1 - alpha;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double input = x[i];
                double output = (b0 / a0) * input + (b1 / a0) * x1 + (b2 / a0) * x2 - (a1 / a0) * y1 - (a2 / a0) * y2;
                x2 = x1; x1 = input; y2 = y1; y1 = output;
                x[i] = (float)output;
            }
            return x;
        }

        /// <summary>
        /// A band-pass filter whose centre frequency follows a function of time (a Chamberlin state-variable filter): the wash of
        /// a stream, a gust of wind. The output is boosted by about q, so normalise it afterwards.
        /// </summary>
        public static float[] SweepBand(float[] x, int rate, Func<float, float> centreHz, float q)
        {
            float low = 0f;
            float band = 0f;
            float damp = 1f / q;
            for (int i = 0; i < x.Length; i++)
            {
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(centreHz(i / (float)rate), 20f, rate * 0.16f) / rate);
                low += f * band;
                float high = x[i] - low - damp * band;
                band += f * high;
                x[i] = band;
            }
            return x;
        }

        /// <summary>Multiplies a buffer by an envelope function of time.</summary>
        public static float[] Shape(float[] x, int rate, Func<float, float> envelope)
        {
            for (int i = 0; i < x.Length; i++) x[i] *= envelope(i / (float)rate);
            return x;
        }

        public static float[] Fade(float[] x, int rate, float fadeIn, float fadeOut)
        {
            float seconds = x.Length / (float)rate;
            for (int i = 0; i < x.Length; i++)
            {
                float t = i / (float)rate;
                x[i] *= Smooth(t / Mathf.Max(0.0005f, fadeIn)) * Smooth((seconds - t) / Mathf.Max(0.0005f, fadeOut));
            }
            return x;
        }

        /// <summary>Scales a buffer so its loudest sample sits at the given level (dBFS, so -6 is half scale).</summary>
        public static float[] Normalize(float[] x, float peakDb)
        {
            float peak = 0f;
            for (int i = 0; i < x.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(x[i]));
            if (peak < 1e-6f) return x;
            float gain = Mathf.Pow(10f, peakDb / 20f) / peak;
            for (int i = 0; i < x.Length; i++) x[i] *= gain;
            return x;
        }

        /// <summary>Scales a buffer to a loudness (RMS, dBFS) and then, if that made the peak too high, down to the peak limit.</summary>
        public static float[] NormalizeRms(float[] x, float rmsDb, float peakLimitDb)
        {
            double sum = 0;
            float peak = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                sum += x[i] * (double)x[i];
                peak = Mathf.Max(peak, Mathf.Abs(x[i]));
            }
            float rms = (float)Math.Sqrt(sum / Math.Max(1, x.Length));
            if (rms < 1e-7f) return x;
            float gain = Mathf.Pow(10f, rmsDb / 20f) / rms;
            float limit = Mathf.Pow(10f, peakLimitDb / 20f);
            if (peak * gain > limit) gain = limit / peak;
            for (int i = 0; i < x.Length; i++) x[i] *= gain;
            return x;
        }

        /// <summary>Removes any constant offset (so a loop does not click or drift).</summary>
        public static float[] RemoveOffset(float[] x)
        {
            double sum = 0;
            for (int i = 0; i < x.Length; i++) sum += x[i];
            float mean = (float)(sum / x.Length);
            for (int i = 0; i < x.Length; i++) x[i] -= mean;
            return x;
        }

        /// <summary>
        /// Turns a longer recording into a seamless loop: the last <paramref name="overlapSeconds"/> are cross-faded (equal power) into
        /// the first ones, so the end continues straight into the start.
        /// </summary>
        public static float[] MakeLoop(float[] longBuffer, float overlapSeconds, int rate)
        {
            int overlap = Mathf.RoundToInt(overlapSeconds * rate);
            int length = longBuffer.Length - overlap;
            float[] loop = new float[length];
            for (int i = 0; i < length; i++) loop[i] = longBuffer[i];
            for (int i = 0; i < overlap; i++)
            {
                float k = i / (float)overlap;
                loop[i] = longBuffer[i] * Mathf.Sin(k * Mathf.PI * 0.5f) + longBuffer[length + i] * Mathf.Cos(k * Mathf.PI * 0.5f);
            }
            return loop;
        }

        // ---- Space ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// A smooth room: the sound is convolved (by FFT) with a synthetic impulse response, a burst of noise that dies away
        /// exponentially, darker as it goes, after a handful of early reflections. Unlike a comb-filter reverb it does not ring
        /// at particular pitches, which matters for sine-based bells and pads. <paramref name="size"/> scales the decay time
        /// (about 2.1 seconds to -60 dB at 1.0). <paramref name="wet"/> is the level of the room relative to the dry sound. With
        /// <paramref name="loop"/> the tail that runs past the end wraps round onto the start, so a loop stays seamless.
        /// </summary>
        public static float[] Reverb(float[] dry, int rate, float wet, float size = 1f, bool loop = false)
        {
            float[] ir = ImpulseResponse(rate, 2.1f * size, 7 + Mathf.RoundToInt(size * 10f));
            int n = dry.Length;
            int fftSize = 1;
            while (fftSize < n + ir.Length) fftSize <<= 1;

            double[] aRe = new double[fftSize];
            double[] aIm = new double[fftSize];
            double[] bRe = new double[fftSize];
            double[] bIm = new double[fftSize];
            for (int i = 0; i < n; i++) aRe[i] = dry[i];
            for (int i = 0; i < ir.Length; i++) bRe[i] = ir[i];

            Fft(aRe, aIm, false);
            Fft(bRe, bIm, false);
            for (int i = 0; i < fftSize; i++)
            {
                double re = aRe[i] * bRe[i] - aIm[i] * bIm[i];
                double im = aRe[i] * bIm[i] + aIm[i] * bRe[i];
                aRe[i] = re;
                aIm[i] = im;
            }
            Fft(aRe, aIm, true);

            float[] mixed = new float[n];
            int convolvedLength = n + ir.Length - 1;
            for (int i = 0; i < n; i++)
            {
                double room = aRe[i];
                if (loop && i + n < convolvedLength) room += aRe[i + n];   // fold the tail back onto the start
                mixed[i] = dry[i] + wet * (float)room;
            }
            return mixed;
        }

        /// <summary>Unit-energy room response: early reflections, then noise that decays by 60 dB in <paramref name="rt60"/> seconds and loses its highs.</summary>
        static float[] ImpulseResponse(int rate, float rt60, int seed)
        {
            int preDelay = Mathf.RoundToInt(0.015f * rate);
            int tail = Mathf.CeilToInt(rt60 * rate);
            System.Random random = new System.Random(seed);
            float[] bright = new float[tail];
            float[] dark = new float[tail];
            for (int i = 0; i < tail; i++)
            {
                bright[i] = (float)random.NextDouble() * 2f - 1f;
                dark[i] = (float)random.NextDouble() * 2f - 1f;
            }
            Biquad(dark, Filter.LowPass, 1400f, 0.7f, rate);
            UnitRms(bright);
            UnitRms(dark);

            float[] ir = new float[preDelay + tail];
            for (int i = 0; i < tail; i++)
            {
                float t = i / (float)rate;
                float decay = Mathf.Exp(-6.9078f * t / rt60);
                float highs = Mathf.Exp(-t / 0.35f);                    // the bright part of the room dies in a third of a second
                float attack = Smooth(t / 0.006f);                      // no click at the start of the response
                ir[preDelay + i] = (bright[i] * highs * 0.7f + dark[i] * (1f - highs * 0.7f)) * decay * attack;
            }

            float[] earlyMs = { 11f, 19f, 27f, 36f, 47f };
            for (int k = 0; k < earlyMs.Length; k++)
            {
                int at = Mathf.RoundToInt(earlyMs[k] * rate / 1000f);
                if (at < ir.Length) ir[at] += (k % 2 == 0 ? 1f : -1f) * 2.2f / (k + 1.5f);
            }

            double energy = 0.0;
            for (int i = 0; i < ir.Length; i++) energy += ir[i] * (double)ir[i];
            float scale = (float)(1.0 / Math.Sqrt(Math.Max(energy, 1e-12)));
            for (int i = 0; i < ir.Length; i++) ir[i] *= scale;
            return ir;
        }

        static void UnitRms(float[] x)
        {
            double sum = 0.0;
            for (int i = 0; i < x.Length; i++) sum += x[i] * (double)x[i];
            float scale = (float)(1.0 / Math.Sqrt(Math.Max(sum / Math.Max(1, x.Length), 1e-12)));
            for (int i = 0; i < x.Length; i++) x[i] *= scale;
        }

        /// <summary>In-place radix-2 FFT (the length must be a power of two). The inverse transform divides by the length.</summary>
        static void Fft(double[] re, double[] im, bool inverse)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    double t = re[i]; re[i] = re[j]; re[j] = t;
                    t = im[i]; im[i] = im[j]; im[j] = t;
                }
            }

            for (int length = 2; length <= n; length <<= 1)
            {
                double angle = 2.0 * Math.PI / length * (inverse ? 1.0 : -1.0);
                double stepRe = Math.Cos(angle);
                double stepIm = Math.Sin(angle);
                int half = length >> 1;
                for (int start = 0; start < n; start += length)
                {
                    double wRe = 1.0;
                    double wIm = 0.0;
                    for (int k = 0; k < half; k++)
                    {
                        int a = start + k;
                        int b = a + half;
                        double tRe = re[b] * wRe - im[b] * wIm;
                        double tIm = re[b] * wIm + im[b] * wRe;
                        re[b] = re[a] - tRe;
                        im[b] = im[a] - tIm;
                        re[a] += tRe;
                        im[a] += tIm;
                        double nextRe = wRe * stepRe - wIm * stepIm;
                        wIm = wRe * stepIm + wIm * stepRe;
                        wRe = nextRe;
                    }
                }
            }

            if (!inverse) return;
            for (int i = 0; i < n; i++)
            {
                re[i] /= n;
                im[i] /= n;
            }
        }

        // ---- File format ----------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the 16-bit PCM mono WAV bytes this class writes back into floats (used by the tests to measure the files).</summary>
        public static float[] FromWav(byte[] bytes, out int rate)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.ReadBytes(12);                       // RIFF, size, WAVE
                rate = 0;
                int channels = 1;
                while (stream.Position + 8 <= stream.Length)
                {
                    string id = new string(reader.ReadChars(4));
                    int size = reader.ReadInt32();
                    if (id == "fmt ")
                    {
                        reader.ReadInt16();                 // format tag
                        channels = reader.ReadInt16();
                        rate = reader.ReadInt32();
                        reader.ReadBytes(size - 8);
                    }
                    else if (id == "data")
                    {
                        int count = size / 2 / Math.Max(1, channels);
                        float[] samples = new float[count];
                        for (int i = 0; i < count; i++)
                        {
                            samples[i] = reader.ReadInt16() / 32768f;
                            for (int c = 1; c < channels; c++) reader.ReadInt16();
                        }
                        return samples;
                    }
                    else
                    {
                        reader.ReadBytes(size);
                    }
                }
            }
            return new float[0];
        }

        /// <summary>16-bit PCM mono WAV bytes. Samples are clipped softly to the legal range first.</summary>
        public static byte[] ToWav(float[] samples, int rate)
        {
            using (MemoryStream stream = new MemoryStream(44 + samples.Length * 2))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);          // PCM
                writer.Write((short)1);          // mono
                writer.Write(rate);
                writer.Write(rate * 2);          // byte rate
                writer.Write((short)2);          // block align
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                for (int i = 0; i < samples.Length; i++)
                {
                    float s = samples[i];
                    s = s > 0.98f ? 0.98f + (float)Math.Tanh((s - 0.98f) * 8f) * 0.02f : s < -0.98f ? -0.98f + (float)Math.Tanh((s + 0.98f) * 8f) * 0.02f : s;
                    writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
                }
                return stream.ToArray();
            }
        }
    }
}

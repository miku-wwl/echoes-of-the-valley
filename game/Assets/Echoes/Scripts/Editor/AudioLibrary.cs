using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Echoes.EditorTools.AudioSynth;

namespace Echoes.EditorTools
{
    /// <summary>The generated clips by role, loaded back from the project after import.</summary>
    internal sealed class AudioSet
    {
        public AudioClip Bells, Harp, Marimba, Pad, Lead, Chimes, Wind, Water, Birds, Sails, Ending;
        public readonly Dictionary<SoundCue, AudioClip[]> Cues = new Dictionary<SoundCue, AudioClip[]>();
        public readonly List<string> Paths = new List<string>();

        public AudioClip Layer(string name)
        {
            switch (name)
            {
                case "bells": return Bells;
                case "harp": return Harp;
                case "marimba": return Marimba;
                case "pad": return Pad;
                case "lead": return Lead;
                case "chimes": return Chimes;
                case "wind": return Wind;
                case "water": return Water;
                case "birds": return Birds;
                case "sails": return Sails;
                default: return null;
            }
        }
    }

    /// <summary>
    /// All of the game's music and sound, composed and synthesised by code (see AudioSynth): no recordings, samples, loops or
    /// downloaded files. The music is one small piece in D major at 80 beats per minute (a four-chord loop, D - A/C# - Bm7 -
    /// Gmaj7, two bars each) split into stems that the AudioDirector fades in as the valley is restored. Everything is
    /// deterministic: the same code writes the same WAV files every time, and a file is only rewritten when its bytes change.
    /// </summary>
    internal static class AudioLibrary
    {
        public const int MusicRate = 32000;
        public const int BedRate = 22050;
        public const int SfxRate = 32000;

        public const float Beat = 0.75f;               // 80 beats per minute
        public const float Bar = 3f;                   // four beats
        public const float LoopSeconds = 24f;          // eight bars, so every stem loops in step
        public const float LeadLoopSeconds = 48f;      // the melody has two different eight-bar phrases
        public const float EndingSeconds = 43f;

        public const string MusicFolder = AssetPaths.Audio + "/Music";
        public const string AmbienceFolder = AssetPaths.Audio + "/Ambience";
        public const string SfxFolder = AssetPaths.Audio + "/Sfx";

        // ---- The composition (public so the tests can check it is in key) ---------------------------------------------------------

        public readonly struct Note
        {
            public readonly float Beat;
            public readonly float Length;
            public readonly int Midi;

            public Note(float beat, float length, int midi)
            {
                Beat = beat;
                Length = length;
                Midi = midi;
            }
        }

        /// <summary>The four chords (two bars each) as pad voicings: Dadd9, A over C#, Bm7, Gmaj9.</summary>
        public static readonly int[][] PadChords =
        {
            new[] { 50, 57, 62, 64, 66 },
            new[] { 49, 57, 61, 64, 69 },
            new[] { 47, 57, 62, 66, 69 },
            new[] { 43, 59, 62, 66, 69 }
        };

        public static readonly int[] EndingEmChord = { 52, 59, 62, 64, 67 };
        public static readonly int[] EndingAChord = { 45, 52, 57, 61, 64 };
        public static readonly int[] EndingFinalChord = { 38, 50, 57, 62, 66, 69 };

        /// <summary>One bar of the harp per entry: four quarter notes, rolling up and back down the chord.</summary>
        public static readonly int[][] HarpBars =
        {
            new[] { 50, 57, 62, 66 }, new[] { 69, 66, 62, 57 },
            new[] { 49, 57, 61, 64 }, new[] { 69, 64, 61, 57 },
            new[] { 47, 54, 62, 66 }, new[] { 69, 66, 62, 54 },
            new[] { 43, 50, 59, 66 }, new[] { 69, 66, 62, 59 }
        };

        public static readonly int[][] EndingHarpBars =
        {
            new[] { 52, 59, 62, 67 }, new[] { 45, 52, 57, 61 }, new[] { 50, 57, 62, 66 }, new[] { 69, 66, 62, 57 }
        };

        /// <summary>The marimba plays four of the eight eighth-notes of each bar (a gentle, off-beat figure).</summary>
        public static readonly int[] MarimbaEighths = { 0, 3, 4, 7 };

        public static readonly int[][] MarimbaBars =
        {
            new[] { 66, 69, 74, 69 }, new[] { 64, 69, 74, 66 },
            new[] { 64, 69, 73, 69 }, new[] { 61, 66, 69, 73 },
            new[] { 66, 69, 74, 69 }, new[] { 62, 66, 71, 69 },
            new[] { 62, 67, 71, 66 }, new[] { 59, 62, 67, 71 }
        };

        public static readonly Note[] BellNotes =
        {
            new Note(1, 4, 78), new Note(6, 4, 81), new Note(9, 4, 76), new Note(13, 4, 85),
            new Note(17, 4, 74), new Note(20, 4, 78), new Note(25, 4, 83), new Note(29, 4, 81)
        };

        public static readonly Note[] PhraseA =
        {
            new Note(0, 1, 74), new Note(1, 1, 76), new Note(2, 2, 78),
            new Note(4, 1.5f, 76), new Note(5.5f, 0.5f, 74), new Note(6, 2, 69),
            new Note(8, 1, 73), new Note(9, 1, 76), new Note(10, 2, 81),
            new Note(12, 1.5f, 78), new Note(13.5f, 0.5f, 76), new Note(14, 2, 73),
            new Note(16, 1, 74), new Note(17, 1, 78), new Note(18, 2, 81),
            new Note(20, 1.5f, 78), new Note(21.5f, 0.5f, 76), new Note(22, 2, 74),
            new Note(24, 1, 71), new Note(25, 1, 74), new Note(26, 2, 78),
            new Note(28, 2, 76), new Note(30, 2, 74)
        };

        public static readonly Note[] PhraseB =
        {
            new Note(0, 1, 69), new Note(1, 1, 74), new Note(2, 1, 78), new Note(3, 1, 76),
            new Note(4, 3, 74),
            new Note(8, 1, 76), new Note(9, 1, 73), new Note(10, 2, 69),
            new Note(12, 1, 71), new Note(13, 1, 73), new Note(14, 2, 76),
            new Note(16, 1, 78), new Note(17, 1, 76), new Note(18, 1, 74), new Note(19, 1, 71),
            new Note(20, 2, 74), new Note(22, 2, 78),
            new Note(24, 1.5f, 79), new Note(25.5f, 0.5f, 78), new Note(26, 2, 74),
            new Note(28, 2, 71), new Note(30, 2, 69)
        };

        /// <summary>The closing phrase of the ending (starts on beat 32): Em7, A, then the home chord and a long last note.</summary>
        public static readonly Note[] PhraseEnd =
        {
            new Note(0, 1.5f, 79), new Note(1.5f, 0.5f, 78), new Note(2, 2, 76),
            new Note(4, 1, 73), new Note(5, 1, 76), new Note(6, 2, 81),
            new Note(8, 2, 78), new Note(10, 6, 74)
        };

        /// <summary>The pitch classes of D major: D E F# G A B C#.</summary>
        public static bool InKey(int midi)
        {
            switch (((midi % 12) + 12) % 12)
            {
                case 2: case 4: case 6: case 7: case 9: case 11: case 1: return true;
                default: return false;
            }
        }

        /// <summary>Every pitch the composition uses, for the in-key test.</summary>
        public static IEnumerable<int> AllNotes()
        {
            foreach (int[] chord in PadChords) foreach (int n in chord) yield return n;
            foreach (int n in EndingEmChord) yield return n;
            foreach (int n in EndingAChord) yield return n;
            foreach (int n in EndingFinalChord) yield return n;
            foreach (int[] bar in HarpBars) foreach (int n in bar) yield return n;
            foreach (int[] bar in EndingHarpBars) foreach (int n in bar) yield return n;
            foreach (int[] bar in MarimbaBars) foreach (int n in bar) yield return n;
            foreach (Note n in BellNotes) yield return n.Midi;
            foreach (Note n in PhraseA) yield return n.Midi;
            foreach (Note n in PhraseB) yield return n.Midi;
            foreach (Note n in PhraseEnd) yield return n.Midi;
            foreach (int n in ChimeScale) yield return n;
        }

        static readonly int[] ChimeScale = { 86, 88, 90, 93, 95, 98 };

        // ---- What the director plays at each stage --------------------------------------------------------------------------------

        public sealed class LayerSpec
        {
            public readonly string Name;
            public readonly bool Music;
            public readonly float[] Volumes;

            public LayerSpec(string name, bool music, float s0, float s1, float s2, float s3)
            {
                Name = name;
                Music = music;
                Volumes = new[] { s0, s1, s2, s3 };
            }
        }

        /// <summary>S0 sparse bells and wind; S1 adds water and the harp; S2 birds, chimes, the marimba and a pad; S3 the melody, a fuller pad and the turning sails.</summary>
        public static readonly LayerSpec[] ValleyLayers =
        {
            new LayerSpec("bells", true, 0.46f, 0.40f, 0.34f, 0.30f),
            new LayerSpec("harp", true, 0.00f, 0.55f, 0.62f, 0.58f),
            new LayerSpec("marimba", true, 0.00f, 0.00f, 0.42f, 0.38f),
            new LayerSpec("pad", true, 0.00f, 0.00f, 0.35f, 0.62f),
            new LayerSpec("lead", true, 0.00f, 0.00f, 0.00f, 0.55f),
            new LayerSpec("chimes", false, 0.00f, 0.00f, 0.50f, 0.38f),
            new LayerSpec("wind", false, 0.60f, 0.42f, 0.32f, 0.24f),
            new LayerSpec("water", false, 0.00f, 0.55f, 0.62f, 0.62f),
            new LayerSpec("birds", false, 0.00f, 0.00f, 0.60f, 0.42f),
            new LayerSpec("sails", false, 0.00f, 0.00f, 0.00f, 0.30f)
        };

        /// <summary>The title screen plays one welcoming mix whatever the save says (index 0 of each table).</summary>
        public static readonly LayerSpec[] MenuLayers =
        {
            new LayerSpec("bells", true, 0.50f, 0, 0, 0),
            new LayerSpec("harp", true, 0.46f, 0, 0, 0),
            new LayerSpec("pad", true, 0.32f, 0, 0, 0),
            new LayerSpec("wind", false, 0.34f, 0, 0, 0),
            new LayerSpec("chimes", false, 0.16f, 0, 0, 0)
        };

        /// <summary>The loudness of each cue relative to its file (the footsteps and interface ticks are kept quiet).</summary>
        public static float CueVolume(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Footstep: return 0.34f;
                case SoundCue.UiHover: return 0.45f;
                case SoundCue.UiClick: return 0.7f;
                case SoundCue.DialogueTick: return 0.5f;
                case SoundCue.PanelOpen:
                case SoundCue.PanelClose: return 0.6f;
                case SoundCue.Denied: return 0.7f;
                case SoundCue.GatherHerb:
                case SoundCue.GatherFlower:
                case SoundCue.GatherOre: return 0.8f;
                case SoundCue.StreamRestored:
                case SoundCue.GardenRestored:
                case SoundCue.WindmillRestored: return 1f;
                default: return 0.9f;
            }
        }

        // ---- Writing ---------------------------------------------------------------------------------------------------------------------

        static int written;
        static int unchanged;

        /// <summary>Synthesises everything, writes the WAV files whose bytes changed, configures their import and loads them back.</summary>
        public static AudioSet Generate()
        {
            written = 0;
            unchanged = 0;
            AssetPaths.EnsureFolder(MusicFolder);
            AssetPaths.EnsureFolder(AmbienceFolder);
            AssetPaths.EnsureFolder(SfxFolder);

            AudioSet set = new AudioSet();
            set.Bells = Save(set, MusicFolder, "bells", BellStem(), MusicRate);
            set.Harp = Save(set, MusicFolder, "harp", HarpStem(), MusicRate);
            set.Marimba = Save(set, MusicFolder, "marimba", MarimbaStem(), MusicRate);
            set.Pad = Save(set, MusicFolder, "pad", PadStem(), MusicRate);
            set.Lead = Save(set, MusicFolder, "lead", LeadStem(), MusicRate);
            set.Ending = Save(set, MusicFolder, "ending", EndingPiece(), MusicRate);
            set.Chimes = Save(set, AmbienceFolder, "chimes", ChimeStem(), MusicRate);
            set.Wind = Save(set, AmbienceFolder, "wind", WindBed(), BedRate);
            set.Water = Save(set, AmbienceFolder, "water", WaterBed(), BedRate);
            set.Birds = Save(set, AmbienceFolder, "birds", BirdBed(), BedRate);
            set.Sails = Save(set, AmbienceFolder, "sails", SailsBed(), BedRate);
            BuildCues(set);

            Debug.Log("[Echoes] Audio: " + set.Paths.Count + " files (" + written + " written, " + unchanged + " unchanged).");
            return set;
        }

        static AudioClip Save(AudioSet set, string folder, string name, float[] samples, int rate)
        {
            string assetPath = folder + "/" + name + ".wav";
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            byte[] bytes = ToWav(samples, rate);
            bool changed = !File.Exists(fullPath) || !File.ReadAllBytes(fullPath).SequenceEqual(bytes);
            if (changed)
            {
                File.WriteAllBytes(fullPath, bytes);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                written++;
            }
            else
            {
                unchanged++;
            }

            ConfigureImport(assetPath);
            set.Paths.Add(assetPath);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        /// <summary>Plain uncompressed PCM, decompressed on load: loops are sample-exact and the files are small and short enough.</summary>
        static void ConfigureImport(string assetPath)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) return;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool right = importer.forceToMono && !importer.loadInBackground && settings.preloadAudioData
                         && settings.loadType == AudioClipLoadType.DecompressOnLoad
                         && settings.compressionFormat == AudioCompressionFormat.PCM
                         && settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate;
            if (right) return;

            importer.forceToMono = true;
            importer.loadInBackground = false;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        static float S(float x) => Mathf.SmoothStep(0f, 1f, x);

        // ---- Music stems (24 seconds, looping) ---------------------------------------------------------------------------------------

        /// <summary>Sparse, glassy bells: one every few seconds, in a big room. This is most of what the silent valley sings.</summary>
        static float[] BellStem()
        {
            int r = MusicRate;
            float[] y = Buffer(LoopSeconds, r);
            foreach (Note n in BellNotes)
                Mix(y, Bell(Midi(n.Midi), 7f, r, 0.8f, 0.9f), n.Beat * Beat, 1f, r, true);
            y = Reverb(y, r, 0.55f, 1.7f, true);
            return Normalize(RemoveOffset(y), -6f);
        }

        /// <summary>A plucked harp rolling through each chord, one note a beat.</summary>
        static float[] HarpStem()
        {
            int r = MusicRate;
            float[] y = Buffer(LoopSeconds, r);
            System.Random human = new System.Random(77);
            for (int bar = 0; bar < HarpBars.Length; bar++)
            {
                for (int q = 0; q < 4; q++)
                {
                    int midi = HarpBars[bar][q];
                    float t = (bar * 4 + q) * Beat + ((float)human.NextDouble() - 0.5f) * 0.016f;
                    float velocity = q == 0 ? 1f : 0.62f + 0.12f * (q % 2);
                    Mix(y, Pluck(Midi(midi), 4.2f, r, 1.35f, 0.2f + 0.04f * (q % 3)), t, velocity, r, true);
                }
            }
            y = Reverb(y, r, 0.35f, 1.3f, true);
            return Normalize(RemoveOffset(y), -5f);
        }

        /// <summary>A soft wooden mallet figure, four notes a bar, off the beat.</summary>
        static float[] MarimbaStem()
        {
            int r = MusicRate;
            float[] y = Buffer(LoopSeconds, r);
            float eighth = Beat * 0.5f;
            for (int bar = 0; bar < MarimbaBars.Length; bar++)
            {
                for (int h = 0; h < MarimbaEighths.Length; h++)
                {
                    float t = (bar * 8 + MarimbaEighths[h]) * eighth;
                    Mix(y, Marimba(Midi(MarimbaBars[bar][h]), 1.8f, r), t, h == 0 ? 1f : 0.7f, r, true);
                }
            }
            y = Reverb(y, r, 0.25f, 1.2f, true);
            return Normalize(RemoveOffset(y), -6f);
        }

        /// <summary>Warm sustained chords that melt into one another.</summary>
        static float[] PadStem()
        {
            int r = MusicRate;
            float[] y = Buffer(LoopSeconds, r);
            for (int c = 0; c < PadChords.Length; c++)
                Mix(y, Pad(Midis(PadChords[c]), 2f * Bar + 2.2f, r, 1.4f, 2.2f), c * 2f * Bar, 1f, r, true);
            y = Reverb(y, r, 0.4f, 1.5f, true);
            return NormalizeRms(RemoveOffset(y), -21f, -6f);
        }

        /// <summary>The tune: two eight-bar phrases (48 seconds before it comes round again), in a soft flute-like voice.</summary>
        static float[] LeadStem()
        {
            int r = MusicRate;
            float[] y = Buffer(LeadLoopSeconds, r);
            Phrase(y, PhraseA, 0f, r, 11);
            Phrase(y, PhraseB, LoopSeconds, r, 29);
            y = Reverb(y, r, 0.32f, 1.3f, true);
            return NormalizeRms(RemoveOffset(y), -22f, -6f);
        }

        static void Phrase(float[] y, Note[] notes, float startSeconds, int rate, int seed, bool wrap = true)
        {
            foreach (Note n in notes)
            {
                float length = n.Length * Beat;
                float register = Mathf.Clamp(1f - (n.Midi - 74) * 0.02f, 0.8f, 1.15f);
                float release = n.Length >= 5f ? 1.4f : 0.4f;
                float[] voice = Lead(Midi(n.Midi), length + release - 0.05f, rate, 0.08f, release, seed + n.Midi + (int)(n.Beat * 10f));
                Mix(y, voice, startSeconds + n.Beat * Beat, register, rate, wrap);
            }
        }

        static float[] Midis(int[] notes) => notes.Select(n => Midi(n)).ToArray();

        /// <summary>Small clusters of wind chimes on a breeze, in the pentatonic scale of the key.</summary>
        static float[] ChimeStem()
        {
            int r = MusicRate;
            float[] y = Buffer(LoopSeconds, r);
            System.Random random = new System.Random(4242);
            float[] clusters = { 1.3f, 4.9f, 8.6f, 12.4f, 15.6f, 19.1f, 22.3f };
            foreach (float start in clusters)
            {
                float t = start;
                int count = 2 + random.Next(3);
                for (int k = 0; k < count; k++)
                {
                    int midi = ChimeScale[random.Next(ChimeScale.Length)];
                    Mix(y, Chime(Midi(midi), 3.5f, r, 1.2f), t, 1f - 0.18f * k, r, true);
                    t += 0.07f + (float)random.NextDouble() * 0.11f;
                }
            }
            y = Reverb(y, r, 0.35f, 1.2f, true);
            return Normalize(RemoveOffset(y), -10f);
        }

        // ---- Ambience beds (24 seconds, looping) ---------------------------------------------------------------------------------------

        /// <summary>A breeze: low and mid bands of filtered noise swelling and ebbing at different rates, whole cycles per loop.</summary>
        static float[] WindBed()
        {
            int r = BedRate;
            const float overlap = 2f;
            float total = LoopSeconds + overlap;
            float[] low = Biquad(Noise(total, r, 101), Filter.LowPass, 420f, 0.7f, r);
            float[] mid = Biquad(Biquad(Noise(total, r, 102), Filter.BandPass, 700f, 0.9f, r), Filter.LowPass, 1800f, 0.7f, r);
            float[] air = Biquad(Noise(total, r, 103), Filter.HighPass, 3200f, 0.7f, r);
            float[] y = new float[low.Length];
            float w = 2f * Mathf.PI / LoopSeconds;
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)r;
                float gl = 0.55f + 0.30f * Mathf.Sin(w * 3f * t + 0.6f) + 0.12f * Mathf.Sin(w * 7f * t + 2.0f);
                float gm = 0.30f + 0.25f * Mathf.Sin(w * 5f * t + 2.1f) + 0.10f * Mathf.Sin(w * 11f * t + 0.4f);
                float ga = 0.05f + 0.04f * Mathf.Sin(w * 4f * t + 1.1f);
                y[i] = low[i] * gl * 4.5f + mid[i] * gm * 2.4f + air[i] * ga * 0.5f;
            }
            return NormalizeRms(RemoveOffset(MakeLoop(y, overlap, r)), -24f, -6f);
        }

        /// <summary>A small stream: a bright hiss, a low murmur and a hundred and fifty tiny bubbles.</summary>
        static float[] WaterBed()
        {
            int r = BedRate;
            const float overlap = 2f;
            float total = LoopSeconds + overlap;
            float[] hiss = Biquad(Biquad(Noise(total, r, 201), Filter.BandPass, 900f, 0.6f, r), Filter.LowPass, 3200f, 0.7f, r);
            float[] rumble = Biquad(Noise(total, r, 202), Filter.LowPass, 300f, 0.7f, r);
            float[] y = new float[hiss.Length];
            float w = 2f * Mathf.PI / LoopSeconds;
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)r;
                float flow = 0.55f + 0.20f * Mathf.Sin(w * 7f * t + 0.3f) + 0.12f * Mathf.Sin(w * 13f * t + 1.9f);
                y[i] = hiss[i] * flow * 3.0f + rumble[i] * 3.5f;
            }
            float[] bed = NormalizeRms(RemoveOffset(MakeLoop(y, overlap, r)), -27f, -6f);

            System.Random random = new System.Random(203);
            for (int k = 0; k < 150; k++)
            {
                float start = (float)random.NextDouble() * LoopSeconds;
                float f0 = 420f + (float)random.NextDouble() * 620f;
                float ratio = 1.5f + (float)random.NextDouble() * 0.9f;
                float length = 0.03f + (float)random.NextDouble() * 0.05f;
                float amplitude = 0.03f + 0.09f * Mathf.Pow((float)random.NextDouble(), 2f);
                Mix(bed, Sweep(f0, f0 * ratio, length, r, 3f / length), start, amplitude, r, true);
            }
            return NormalizeRms(RemoveOffset(bed), -24f, -6f);
        }

        static float[] BirdA(int rate, float pitch)       // three rising tweets
        {
            float[] y = Buffer(0.5f, rate);
            for (int k = 0; k < 3; k++)
                Mix(y, Chirp(3000f * pitch * (1f + 0.08f * k), 3700f * pitch * (1f + 0.08f * k), 0.075f, rate, 40f, 0.02f), k * 0.12f, 1f - 0.15f * k, rate);
            return y;
        }

        static float[] BirdB(int rate, float pitch)       // a two-note whistle, high then lower
        {
            float[] y = Buffer(0.8f, rate);
            Mix(y, Chirp(2600f * pitch, 2700f * pitch, 0.22f, rate, 6f, 0.012f), 0f, 0.9f, rate);
            Mix(y, Chirp(2100f * pitch, 1850f * pitch, 0.32f, rate, 6f, 0.012f), 0.26f, 0.8f, rate);
            return y;
        }

        static float[] BirdC(int rate, float pitch)       // a quick trill
        {
            float[] y = Buffer(0.7f, rate);
            for (int k = 0; k < 8; k++)
                Mix(y, Chirp(4300f * pitch, 3500f * pitch, 0.04f, rate, 0f, 0f), k * 0.058f, 0.8f - 0.04f * k, rate);
            return y;
        }

        /// <summary>Nine calls from three small birds, spread over the loop in a small outdoor space.</summary>
        static float[] BirdBed()
        {
            int r = BedRate;
            float[] y = Buffer(LoopSeconds, r);
            System.Random random = new System.Random(311);
            float[] times = { 1.8f, 4.4f, 7.5f, 10.9f, 13.3f, 16.2f, 18.6f, 21.0f, 23.0f };
            char[] kinds = { 'A', 'C', 'B', 'A', 'B', 'C', 'A', 'B', 'C' };
            for (int k = 0; k < times.Length; k++)
            {
                float pitch = 0.95f + (float)random.NextDouble() * 0.1f;
                float[] call = kinds[k] == 'A' ? BirdA(r, pitch) : kinds[k] == 'B' ? BirdB(r, pitch) : BirdC(r, pitch);
                Mix(y, call, times[k], 0.5f + 0.4f * (float)random.NextDouble(), r, true);
            }
            y = Reverb(y, r, 0.18f, 1f, true);
            return Normalize(RemoveOffset(y), -7f);
        }

        /// <summary>The windmill's sails coming round every two seconds: a soft low whump and a faint swish.</summary>
        static float[] SailsBed()
        {
            int r = BedRate;
            const float overlap = 1.5f;
            float total = LoopSeconds + overlap;
            float[] thump = Biquad(Biquad(Noise(total, r, 301), Filter.LowPass, 260f, 0.8f, r), Filter.HighPass, 60f, 0.7f, r);
            float[] swish = Biquad(Noise(total, r, 302), Filter.BandPass, 1700f, 0.8f, r);
            float[] y = new float[thump.Length];
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)r;
                float pass = Mathf.Pow(Mathf.Sin(Mathf.PI * t / 2f), 2f);   // one sail every two seconds
                y[i] = thump[i] * pass * 6f + swish[i] * pass * pass * 0.9f;
            }
            return NormalizeRms(RemoveOffset(MakeLoop(y, overlap, r)), -27f, -8f);
        }

        // ---- The ending ------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The closing piece (43 seconds): the tune is stated once over the four chords with a harp and a few bells, climbs through
        /// Em7 and A, comes home on D with a long last note and a rising bell figure, and rings out.
        /// </summary>
        static float[] EndingPiece()
        {
            int r = MusicRate;
            float[] y = Buffer(EndingSeconds, r);

            for (int c = 0; c < PadChords.Length; c++)
                Mix(y, Pad(Midis(PadChords[c]), 2f * Bar + 2.4f, r, 1.5f, 2.4f), c * 2f * Bar, 0.9f, r);
            Mix(y, Pad(Midis(EndingEmChord), Bar + 2.2f, r, 1.2f, 2.0f), 8f * Bar, 0.9f, r);
            Mix(y, Pad(Midis(EndingAChord), Bar + 2.2f, r, 1.2f, 2.0f), 9f * Bar, 0.9f, r);
            Mix(y, Pad(Midis(EndingFinalChord), 2f * Bar + 7f, r, 1.4f, 5.5f), 10f * Bar, 1.1f, r);

            float[] melody = Buffer(EndingSeconds, r);
            Phrase(melody, PhraseA, 0f, r, 51, false);
            Phrase(melody, PhraseEnd, 8f * Bar, r, 53, false);
            Mix(y, melody, 0f, 1.0f, r);

            System.Random human = new System.Random(88);
            for (int bar = 0; bar < HarpBars.Length + EndingHarpBars.Length; bar++)
            {
                int[] notes = bar < HarpBars.Length ? HarpBars[bar] : EndingHarpBars[bar - HarpBars.Length];
                for (int q = 0; q < 4; q++)
                {
                    float t = (bar * 4 + q) * Beat + ((float)human.NextDouble() - 0.5f) * 0.016f;
                    Mix(y, Pluck(Midi(notes[q]), 4.2f, r, 1.35f, 0.2f), Mathf.Max(0f, t), q == 0 ? 0.8f : 0.5f, r);
                }
            }
            Mix(y, Pluck(Midi(50), 6f, r, 0.9f, 0.2f), 12f * Bar, 0.8f, r);   // the last low note

            foreach (Note n in BellNotes)
                Mix(y, Bell(Midi(n.Midi), 6f, r, 0.8f, 0.9f), n.Beat * Beat, 0.45f, r);
            int[] closing = { 74, 78, 81, 86 };
            for (int k = 0; k < closing.Length; k++)
                Mix(y, Bell(Midi(closing[k]), 7f, r, 0.8f, 0.8f), 12f * Bar + 0.2f + k * 0.5f, 0.7f, r);

            y = Reverb(y, r, 0.38f, 1.5f);
            Fade(RemoveOffset(y), r, 0.6f, 2.2f);
            return Normalize(y, -6f);
        }

        // ---- One-shot sounds ---------------------------------------------------------------------------------------------------------------

        static void BuildCues(AudioSet set)
        {
            AddCue(set, SoundCue.GatherHerb, "gather_herb", Leaf(1, 81), Leaf(2, 83));
            AddCue(set, SoundCue.GatherFlower, "gather_flower", Ting(88, 93), Ting(90, 95));
            AddCue(set, SoundCue.GatherOre, "gather_ore", Ore(0), Ore(2));
            AddCue(set, SoundCue.Craft, "craft", Craft());
            AddCue(set, SoundCue.FeedHen, "feed_hen", Feed());
            AddCue(set, SoundCue.CollectEgg, "collect_egg", Egg());
            AddCue(set, SoundCue.QuestStart, "quest_start", QuestStart());
            AddCue(set, SoundCue.HeartAwakened, "heart_awakened", HeartAwakened());
            AddCue(set, SoundCue.StreamRestored, "stream_restored", StreamRestored());
            AddCue(set, SoundCue.GardenRestored, "garden_restored", GardenRestored());
            AddCue(set, SoundCue.WindmillRestored, "windmill_restored", WindmillRestored());
            AddCue(set, SoundCue.Denied, "denied", Denied());
            AddCue(set, SoundCue.Footstep, "footstep", Step(1), Step(2), Step(3));
            AddCue(set, SoundCue.UiClick, "ui_click", UiClick());
            AddCue(set, SoundCue.UiHover, "ui_hover", UiHover());
            AddCue(set, SoundCue.DialogueTick, "dialogue_tick", Tick(81), Tick(83), Tick(85));
            AddCue(set, SoundCue.PanelOpen, "panel_open", Panel(true));
            AddCue(set, SoundCue.PanelClose, "panel_close", Panel(false));
        }

        static void AddCue(AudioSet set, SoundCue cue, string name, params float[][] variants)
        {
            AudioClip[] clips = new AudioClip[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                // every one-shot ends in silence, even when its last ring-out was cut by the length of the buffer
                Fade(variants[i], SfxRate, 0.0005f, Mathf.Min(0.12f, 0.2f * variants[i].Length / SfxRate));
                clips[i] = Save(set, SfxFolder, variants.Length == 1 ? name : name + "_" + (i + 1), variants[i], SfxRate);
            }
            set.Cues[cue] = clips;
        }

        static float[] Leaf(int seed, int midi)       // a rustle of leaves and a tiny pluck
        {
            int r = SfxRate;
            float[] y = Buffer(0.45f, r);
            float[] rustle = Biquad(Biquad(Noise(0.4f, r, seed), Filter.HighPass, 2200f, 0.7f, r), Filter.LowPass, 6500f, 0.7f, r);
            Shape(rustle, r, t => S(t / 0.004f) * (Mathf.Exp(-t * 16f) + (t > 0.09f ? 0.6f * Mathf.Exp(-(t - 0.09f) * 20f) : 0f)));
            Mix(y, rustle, 0f, 0.8f, r);
            Mix(y, Pluck(Midi(midi), 0.4f, r, 6f, 0.2f), 0.04f, 0.35f, r);
            return Normalize(y, -7f);
        }

        static float[] Ting(int first, int second)    // two small rising bells
        {
            int r = SfxRate;
            float[] y = Buffer(0.9f, r);
            Mix(y, Bell(Midi(first), 0.9f, r, 0.6f, 4.5f), 0f, 0.8f, r);
            Mix(y, Bell(Midi(second), 0.8f, r, 0.6f, 4.5f), 0.09f, 0.7f, r);
            return Normalize(y, -7f);
        }

        static float[] Ore(int variant)               // a glassy tink over a low thunk
        {
            int r = SfxRate;
            float[] y = Buffer(0.7f, r);
            Mix(y, Chime(Midi(93 + variant), 0.7f, r, 5f), 0f, 0.7f, r);
            Mix(y, Sweep(190f, 80f, 0.14f, r, 24f), 0f, 0.9f, r);
            return Normalize(y, -6f);
        }

        static float[] Craft()                        // a bubbling brew, then a sparkle
        {
            int r = SfxRate;
            float[] y = Buffer(2.4f, r);
            float[] fizz = Biquad(Noise(1.0f, r, 5), Filter.LowPass, 900f, 0.7f, r);
            Shape(fizz, r, t => S(t / 0.05f) * S((1.0f - t) / 0.3f) * 0.25f);
            Mix(y, fizz, 0f, 0.6f, r);
            System.Random random = new System.Random(31);
            for (int k = 0; k < 10; k++)
            {
                float f0 = 380f + (float)random.NextDouble() * 400f;
                Mix(y, Sweep(f0, f0 * (1.7f + (float)random.NextDouble() * 0.6f), 0.07f, r, 30f), 0.05f + k * 0.085f + (float)random.NextDouble() * 0.03f, 0.35f, r);
            }
            int[] sparkle = { 86, 90, 93, 98 };
            for (int k = 0; k < sparkle.Length; k++)
                Mix(y, Bell(Midi(sparkle[k]), 1.1f, r, 0.7f, 3.2f), 0.75f + k * 0.11f, 0.55f - k * 0.05f, r);
            return Normalize(y, -6f);
        }

        /// <summary>A hen's cluck: a gliding voice whose harmonics are shaped by a resonance near 1 kHz.</summary>
        static float[] Cluck(float f0, float f1, float seconds)
        {
            int r = SfxRate;
            float[] y = Buffer(seconds, r);
            double phase = 0.0;
            for (int i = 0; i < y.Length; i++)
            {
                float t = i / (float)r;
                float k = t / seconds;
                float f = Mathf.Lerp(f0, f1, k);
                phase += 2.0 * System.Math.PI * f / r;
                float s = 0f;
                for (int h = 1; h <= 8; h++)
                {
                    float fh = f * h;
                    float weight = (1f / h) * (1f + 2.2f * Mathf.Exp(-Mathf.Pow((fh - 1050f) / 380f, 2f)));
                    s += Mathf.Sin(h * (float)phase) * weight;
                }
                y[i] = s * S(t / 0.008f) * Mathf.Exp(-t * 26f);
            }
            return y;
        }

        static float[] Feed()                         // three clucks and a patter of grain
        {
            int r = SfxRate;
            float[] y = Buffer(1.1f, r);
            Mix(y, Cluck(380f, 250f, 0.12f), 0f, 0.9f, r);
            Mix(y, Cluck(400f, 260f, 0.12f), 0.14f, 0.8f, r);
            Mix(y, Cluck(360f, 240f, 0.14f), 0.34f, 0.85f, r);
            System.Random random = new System.Random(19);
            for (int k = 0; k < 12; k++)
            {
                float[] pellet = Biquad(Noise(0.012f, r, 40 + k), Filter.BandPass, 2800f + (float)random.NextDouble() * 1500f, 1.5f, r);
                Shape(pellet, r, t => Mathf.Exp(-t * 300f));
                Mix(y, pellet, 0.45f + (float)random.NextDouble() * 0.45f, 0.55f, r);
            }
            return Normalize(y, -7f);
        }

        static float[] Egg()                          // a pop and two sparkles
        {
            int r = SfxRate;
            float[] y = Buffer(0.8f, r);
            Mix(y, Sweep(230f, 640f, 0.1f, r, 22f), 0f, 0.9f, r);
            Mix(y, Bell(Midi(88), 0.8f, r, 0.7f, 4f), 0.07f, 0.8f, r);
            Mix(y, Bell(Midi(93), 0.7f, r, 0.7f, 4f), 0.15f, 0.65f, r);
            return Normalize(y, -6f);
        }

        static float[] QuestStart()                   // two warm notes, up
        {
            int r = SfxRate;
            float[] y = Buffer(1.8f, r);
            Mix(y, Marimba(Midi(81), 1.2f, r), 0f, 0.9f, r);
            Mix(y, Bell(Midi(81), 1.4f, r, 0.5f, 2.5f), 0f, 0.3f, r);
            Mix(y, Marimba(Midi(86), 1.4f, r), 0.24f, 0.9f, r);
            Mix(y, Bell(Midi(86), 1.4f, r, 0.5f, 2.5f), 0.24f, 0.35f, r);
            Mix(y, Pad(new[] { Midi(62), Midi(69), Midi(74) }, 1.5f, r, 0.3f, 1.0f), 0f, 0.25f, r);
            return Normalize(Reverb(y, r, 0.3f, 1f), -6f);
        }

        static float[] HeartAwakened()                // a warm swell and rising bells
        {
            int r = SfxRate;
            float[] y = Buffer(5.4f, r);
            Mix(y, Pad(Midis(new[] { 50, 57, 62, 66, 69 }), 4.0f, r, 1.0f, 2.2f), 0f, 0.9f, r);
            int[] rise = { 74, 78, 81, 86, 90 };
            for (int k = 0; k < rise.Length; k++)
                Mix(y, Bell(Midi(rise[k]), 2.5f, r, 0.8f, 1.6f), 0.6f + k * 0.32f, 0.5f, r);
            return Normalize(Reverb(y, r, 0.4f, 1.4f), -5f);
        }

        static float[] StreamRestored()               // water rushing back, a harp run and a bell chord
        {
            int r = SfxRate;
            float[] y = Buffer(7.0f, r);
            float[] wash = Noise(5.6f, r, 71);
            SweepBand(wash, r, t => 250f + 1900f * S(t / 2.4f), 1.4f);
            NormalizeRms(wash, -20f, -6f);
            Shape(wash, r, t => S(t / 1.2f) * S((5.6f - t) / 2.2f));
            Mix(y, wash, 0f, 0.9f, r);

            System.Random random = new System.Random(72);
            for (int k = 0; k < 24; k++)
            {
                float f0 = 500f + (float)random.NextDouble() * 700f;
                Mix(y, Sweep(f0, f0 * 1.9f, 0.05f, r, 60f), 0.3f + (float)random.NextDouble() * 3.2f, 0.12f, r);
            }

            int[] run = { 62, 64, 66, 69, 71, 74, 76, 78, 81, 83, 86 };
            for (int k = 0; k < run.Length; k++)
                Mix(y, Pluck(Midi(run[k]), 2.8f, r, 1.6f, 0.2f), 0.5f + k * 0.14f, 0.5f, r);
            foreach (int midi in new[] { 74, 78, 81, 86 })
                Mix(y, Bell(Midi(midi), 3.2f, r, 0.8f, 1.1f), 2.4f, 0.5f, r);
            return Normalize(Reverb(y, r, 0.35f, 1.3f), -5f);
        }

        static float[] GardenRestored()               // birds, a marimba run, chimes and a warm chord
        {
            int r = SfxRate;
            float[] y = Buffer(6.2f, r);
            float[] chirpStarts = { 0.2f, 0.65f, 1.0f, 1.5f, 2.0f };
            for (int k = 0; k < chirpStarts.Length; k++)
                Mix(y, k % 2 == 0 ? BirdA(r, 0.95f + 0.05f * k) : BirdC(r, 1f), chirpStarts[k], 0.35f, r);
            int[] run = { 74, 78, 81, 83, 86, 90 };
            for (int k = 0; k < run.Length; k++)
                Mix(y, Marimba(Midi(run[k]), 1.6f, r), 0.5f + k * 0.11f, 0.7f, r);
            System.Random random = new System.Random(73);
            for (int k = 0; k < 6; k++)
                Mix(y, Chime(Midi(ChimeScale[random.Next(ChimeScale.Length)]), 2.8f, r, 1.3f), 1.4f + k * 0.12f, 0.5f, r);
            Mix(y, Pad(Midis(new[] { 57, 62, 66, 69, 73 }), 5.2f, r, 1.3f, 2.2f), 0.4f, 0.55f, r);
            Mix(y, Bell(Midi(86), 3f, r, 0.8f, 1.2f), 2.2f, 0.6f, r);
            return Normalize(Reverb(y, r, 0.33f, 1.2f), -5f);
        }

        static float[] WindmillRestored()             // a creak, a whoosh, a full chord and a crown of bells
        {
            int r = SfxRate;
            float[] y = Buffer(7.2f, r);
            Mix(y, Cluck(90f, 135f, 0.9f), 0f, 0.5f, r);
            Mix(y, Cluck(110f, 150f, 0.7f), 0.5f, 0.4f, r);
            float[] whoosh = Biquad(Noise(4.2f, r, 91), Filter.LowPass, 700f, 0.7f, r);
            Shape(whoosh, r, t => S((t - 0.1f) / 1.4f) * S((4.2f - t) / 1.8f));
            NormalizeRms(whoosh, -22f, -6f);
            Mix(y, whoosh, 0.6f, 0.8f, r);
            Mix(y, Pad(Midis(new[] { 38, 50, 57, 62, 66, 69, 74 }), 5.8f, r, 1.6f, 3.0f), 1.0f, 1.0f, r);
            int[] harp = { 50, 57, 62, 66, 69, 74 };
            for (int k = 0; k < harp.Length; k++)
                Mix(y, Pluck(Midi(harp[k]), 3f, r, 1.4f, 0.2f), 1.2f + k * 0.2f, 0.5f, r);
            int[] crown = { 86, 93, 98 };
            for (int k = 0; k < crown.Length; k++)
                Mix(y, Bell(Midi(crown[k]), 3.4f, r, 0.8f, 1.2f), 3.0f + k * 0.35f, 0.5f, r);
            return Normalize(Reverb(y, r, 0.36f, 1.4f), -5f);
        }

        static float[] Denied()                       // a soft, polite "mm-mm"
        {
            int r = SfxRate;
            float[] y = Buffer(0.35f, r);
            Mix(y, Sweep(250f, 175f, 0.1f, r, 20f), 0f, 0.8f, r);
            Mix(y, Sweep(230f, 160f, 0.12f, r, 18f), 0.12f, 0.7f, r);
            Biquad(y, Filter.LowPass, 900f, 0.7f, r);
            return Normalize(y, -10f);
        }

        static float[] Step(int seed)                 // a footfall on grass
        {
            int r = SfxRate;
            float[] y = Buffer(0.2f, r);
            float[] grass = Biquad(Biquad(Noise(0.2f, r, seed), Filter.LowPass, 1300f + seed * 200f, 0.7f, r), Filter.HighPass, 180f, 0.7f, r);
            Shape(grass, r, t => S(t / 0.004f) * Mathf.Exp(-t * 38f));
            Mix(y, grass, 0f, 0.8f, r);
            Mix(y, Sweep(120f, 70f, 0.1f, r, 30f), 0f, 0.45f, r);
            return Normalize(y, -6f);
        }

        static float[] UiClick()                      // a wooden tick
        {
            int r = SfxRate;
            float[] y = Buffer(0.2f, r);
            Mix(y, Marimba(Midi(86), 0.2f, r), 0f, 0.9f, r);
            float[] tick = Biquad(Noise(0.02f, r, 9), Filter.BandPass, 3200f, 1.2f, r);
            Shape(tick, r, t => Mathf.Exp(-t * 180f));
            Mix(y, tick, 0f, 0.35f, r);
            return Normalize(y, -8f);
        }

        static float[] UiHover()                      // the faintest bell
        {
            int r = SfxRate;
            return Normalize(Fade(Bell(Midi(93), 0.12f, r, 0.3f, 22f), r, 0.002f, 0.04f), -14f);
        }

        static float[] Tick(int midi)                 // a letter of speech
        {
            int r = SfxRate;
            return Normalize(Fade(Marimba(Midi(midi), 0.07f, r), r, 0.002f, 0.025f), -16f);
        }

        static float[] Panel(bool opening)            // two quick notes, up to open and down to close
        {
            int r = SfxRate;
            float[] y = Buffer(0.4f, r);
            int first = opening ? 74 : 81;
            int second = opening ? 81 : 74;
            Mix(y, Marimba(Midi(first), 0.3f, r), 0f, 0.8f, r);
            Mix(y, Marimba(Midi(second), 0.3f, r), 0.07f, 0.8f, r);
            float[] air = SweepBand(Noise(0.25f, r, opening ? 12 : 13), r, t => opening ? 1000f + 8000f * t : 3000f - 8000f * t, 1.2f);
            NormalizeRms(air, -24f, -8f);
            Shape(air, r, t => S(t / 0.05f) * S((0.25f - t) / 0.1f));
            Mix(y, air, 0f, 0.3f, r);
            return Normalize(y, -10f);
        }
    }
}

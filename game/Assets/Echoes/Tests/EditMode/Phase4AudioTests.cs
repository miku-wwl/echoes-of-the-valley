using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echoes.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Tests
{
    /// <summary>
    /// Phase 4 audio. Every generated file exists and is clean (no clipping, no clicks), the loops join, the music is in key and
    /// in tune, the stages add layers one group at a time, the offline mix of each stage never clips and grows, and both scenes
    /// have their directors wired. How the music *sounds* is a human judgement and is not tested here.
    /// </summary>
    public class Phase4AudioTests
    {
        const string ValleyPath = "Assets/Echoes/Scenes/Valley.unity";
        const string MainMenuPath = "Assets/Echoes/Scenes/MainMenu.unity";

        static readonly string[] Loops =
        {
            "Music/bells", "Music/harp", "Music/marimba", "Music/pad",
            "Ambience/chimes", "Ambience/wind", "Ambience/water", "Ambience/birds", "Ambience/sails"
        };

        static readonly string[] OneShots =
        {
            "gather_herb_1", "gather_herb_2", "gather_flower_1", "gather_flower_2", "gather_ore_1", "gather_ore_2",
            "craft", "feed_hen", "collect_egg", "quest_start", "heart_awakened", "stream_restored", "garden_restored",
            "windmill_restored", "denied", "footstep_1", "footstep_2", "footstep_3", "ui_click", "ui_hover",
            "dialogue_tick_1", "dialogue_tick_2", "dialogue_tick_3", "panel_open", "panel_close"
        };

        static readonly string[] MusicStems = { "bells", "harp", "marimba", "pad", "lead" };

        static IEnumerable<string> Everything =>
            Loops.Concat(new[] { "Music/lead", "Music/ending" }).Concat(OneShots.Select(n => "Sfx/" + n));

        static string AssetPath(string name) => "Assets/Echoes/Audio/" + name + ".wav";

        static string LayerFile(string layer) => (MusicStems.Contains(layer) ? "Music/" : "Ambience/") + layer;

        static float[] Decode(string name, out int rate) =>
            AudioSynth.FromWav(File.ReadAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", AssetPath(name)))), out rate);

        static T First<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        // ---- The files -----------------------------------------------------------------------------------------------------------------

        [Test]
        public void EverySound_ExistsAndImportsAsPlainMonoPcm()
        {
            Assert.AreEqual(36, Everything.Count(), "11 music and ambience files and 25 effects");
            foreach (string name in Everything)
            {
                string path = AssetPath(name);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.IsNotNull(clip, path + " is missing: run the Setup task");
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                Assert.AreEqual(AudioCompressionFormat.PCM, settings.compressionFormat, path + " should be uncompressed so loops are sample exact");
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, settings.loadType, path);
                Assert.AreEqual(1, clip.channels, path);
                Assert.Greater(clip.length, 0.05f, path);
            }
        }

        [Test]
        public void Loops_HaveTheExactLoopLength_SoEveryStemStaysInStep()
        {
            foreach (string name in Loops)
            {
                float[] samples = Decode(name, out int rate);
                Assert.AreEqual(Mathf.RoundToInt(AudioLibrary.LoopSeconds * rate), samples.Length, name);
            }

            float[] lead = Decode("Music/lead", out int leadRate);
            Assert.AreEqual(Mathf.RoundToInt(AudioLibrary.LeadLoopSeconds * leadRate), lead.Length, "the melody is two phrases");
            Assert.AreEqual(2f * AudioLibrary.LoopSeconds, AudioLibrary.LeadLoopSeconds, "a whole number of loops, so it stays in step with the others");

            float[] ending = Decode("Music/ending", out int endingRate);
            Assert.AreEqual(AudioLibrary.EndingSeconds, ending.Length / (float)endingRate, 0.01f);
            Assert.GreaterOrEqual(AudioLibrary.EndingSeconds, 30f, "a short but complete piece");
            Assert.LessOrEqual(AudioLibrary.EndingSeconds, 60f);
        }

        [Test]
        public void EverySound_IsAudible_NotClipped_AndFreeOfDcOffset()
        {
            foreach (string name in Everything)
            {
                float[] samples = Decode(name, out int rate);
                double sum = 0.0;
                float peak = 0f;
                foreach (float s in samples)
                {
                    sum += s;
                    peak = Mathf.Max(peak, Mathf.Abs(s));
                }
                Assert.Greater(peak, 0.03f, name + " is nearly silent");
                Assert.Less(peak, 0.9f, name + " is too hot (peak " + peak + ")");
                Assert.Less(Mathf.Abs((float)(sum / samples.Length)), 0.01f, name + " has a DC offset");
            }
        }

        [Test]
        public void OneShots_StartAndEndInSilence_SoNothingClicks()
        {
            foreach (string shot in OneShots)
            {
                float[] samples = Decode("Sfx/" + shot, out int rate);
                Assert.Less(Mathf.Abs(samples[0]), 0.02f, shot + " starts with a click");
                Assert.Less(Mathf.Abs(samples[samples.Length - 1]), 0.005f, shot + " ends with a click");
            }
        }

        [Test]
        public void Loops_JoinWithoutAClick()
        {
            foreach (string name in Loops.Concat(new[] { "Music/lead" }))
            {
                float[] x = Decode(name, out int rate);
                float[] steps = new float[x.Length - 1];
                for (int i = 0; i < steps.Length; i++) steps[i] = Mathf.Abs(x[i + 1] - x[i]);
                System.Array.Sort(steps);
                float typical = steps[(int)(steps.Length * 0.99f)];
                float seam = Mathf.Abs(x[0] - x[x.Length - 1]);
                Assert.LessOrEqual(seam, typical * 1.5f + 0.003f, name + " clicks where it loops (seam " + seam + ", typical step " + typical + ")");
            }
        }

        // ---- The music -------------------------------------------------------------------------------------------------------------------

        [Test]
        public void TheComposition_StaysInDMajor_AndItsPhrasesFillTheirBars()
        {
            foreach (int note in AudioLibrary.AllNotes())
                Assert.IsTrue(AudioLibrary.InKey(note), "MIDI note " + note + " is outside D major");

            AssertPhrase(AudioLibrary.PhraseA, 32f);
            AssertPhrase(AudioLibrary.PhraseB, 32f);
            AssertPhrase(AudioLibrary.PhraseEnd, 16f);
            Assert.AreEqual(8, AudioLibrary.HarpBars.Length, "eight bars");
            Assert.AreEqual(8, AudioLibrary.MarimbaBars.Length);
            Assert.AreEqual(4, AudioLibrary.PadChords.Length, "four chords of two bars");
            foreach (int[] bar in AudioLibrary.HarpBars) Assert.AreEqual(4, bar.Length, "a note a beat");
            foreach (int[] bar in AudioLibrary.MarimbaBars) Assert.AreEqual(AudioLibrary.MarimbaEighths.Length, bar.Length);
        }

        static void AssertPhrase(AudioLibrary.Note[] phrase, float beats)
        {
            float end = 0f;
            foreach (AudioLibrary.Note note in phrase)
            {
                Assert.GreaterOrEqual(note.Beat, end - 0.001f, "the notes of one voice do not overlap");
                end = note.Beat + note.Length;
            }
            Assert.AreEqual(beats, end, 0.001f, "the phrase fills its bars exactly, so it loops in time");
        }

        [Test]
        public void TheTuneComesHome_AndTheEndingResolvesOnTheHomeChord()
        {
            Assert.AreEqual(2, AudioLibrary.PhraseA.Last().Midi % 12, "phrase A ends on D");
            AudioLibrary.Note last = AudioLibrary.PhraseEnd.Last();
            Assert.AreEqual(2, last.Midi % 12, "the ending's last melody note is D");
            Assert.GreaterOrEqual(last.Length, 4f, "held for at least a bar");

            int[] home = { 2, 4, 6, 9 };   // D, E (the added ninth), F#, A
            foreach (int note in AudioLibrary.EndingFinalChord) CollectionAssert.Contains(home, note % 12, "the last chord is D (add 9)");
            Assert.AreEqual(2, AudioLibrary.EndingFinalChord.Min() % 12, "with D in the bass");
        }

        [Test]
        public void TheRenderedMusic_IsInKey_AndInTune()
        {
            foreach (string stem in new[] { "Music/bells", "Music/harp", "Music/marimba", "Music/pad", "Music/lead", "Music/ending", "Ambience/chimes" })
            {
                float[] x = Decode(stem, out int rate);
                double inKey = 0.0;
                double all = 0.0;
                double between = 0.0;
                for (int midi = 36; midi <= 100; midi++)
                {
                    double power = Goertzel(x, rate, 440.0 * System.Math.Pow(2.0, (midi - 69) / 12.0));
                    all += power;
                    if (AudioLibrary.InKey(midi)) inKey += power;
                    between += Goertzel(x, rate, 440.0 * System.Math.Pow(2.0, (midi - 69 + 0.5) / 12.0));   // a quarter tone away
                }
                Assert.GreaterOrEqual(inKey / all, 0.95, stem + ": the energy sits on D major notes");
                Assert.LessOrEqual(between / all, 0.25, stem + ": the notes are in tune, not between the semitones");
            }
        }

        /// <summary>The power of one frequency in a signal (the Goertzel algorithm).</summary>
        static double Goertzel(float[] x, int rate, double frequency)
        {
            double coefficient = 2.0 * System.Math.Cos(2.0 * System.Math.PI * frequency / rate);
            double s1 = 0.0;
            double s2 = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                double s = x[i] + coefficient * s1 - s2;
                s2 = s1;
                s1 = s;
            }
            return s1 * s1 + s2 * s2 - coefficient * s1 * s2;
        }

        // ---- The stages --------------------------------------------------------------------------------------------------------------

        [Test]
        public void TheStagePlan_AddsOneGroupOfLayersPerStage_AndNeverTakesOneAway()
        {
            string[][] adds =
            {
                new[] { "bells", "wind" },                           // S0: sparse bells and wind
                new[] { "harp", "water" },                           // S1: gentle water and a soft melodic harp
                new[] { "marimba", "pad", "chimes", "birds" },       // S2: livelier nature, birds, delicate chimes
                new[] { "lead", "sails" }                            // S3: the melody, a fuller pad, the turning sails
            };

            HashSet<string> before = new HashSet<string>();
            float lastTotal = 0f;
            for (int stage = 0; stage < 4; stage++)
            {
                HashSet<string> audible = new HashSet<string>(AudioLibrary.ValleyLayers.Where(l => l.Volumes[stage] > 0f).Select(l => l.Name));
                Assert.IsTrue(before.IsSubsetOf(audible), "stage " + stage + " keeps everything from the stage before");
                audible.ExceptWith(before);
                CollectionAssert.AreEquivalent(adds[stage], audible, "what stage " + stage + " adds");
                before.UnionWith(adds[stage]);

                float total = AudioLibrary.ValleyLayers.Sum(l => l.Volumes[stage]);
                Assert.Greater(total, lastTotal, "each stage is fuller than the last");
                lastTotal = total;
            }
            Assert.AreEqual(AudioLibrary.ValleyLayers.Length, before.Count, "every layer is used somewhere");
            foreach (AudioLibrary.LayerSpec layer in AudioLibrary.ValleyLayers)
                foreach (float volume in layer.Volumes) Assert.That(volume, Is.InRange(0f, 1f), layer.Name);
        }

        [Test]
        public void TheOfflineMix_NeverClips_AndGrowsFromEachStageToTheNext()
        {
            const int rate = 32000;
            const int seconds = 96;
            Dictionary<string, float[]> clips = new Dictionary<string, float[]>();
            foreach (AudioLibrary.LayerSpec layer in AudioLibrary.ValleyLayers)
            {
                float[] samples = Decode(LayerFile(layer.Name), out int fileRate);
                clips[layer.Name] = Resample(samples, fileRate, rate);
            }

            float[] rmsDb = new float[4];
            for (int stage = 0; stage < 4; stage++)
            {
                float[] mix = new float[rate * seconds];
                foreach (AudioLibrary.LayerSpec layer in AudioLibrary.ValleyLayers)
                {
                    float volume = layer.Volumes[stage] * (layer.Music ? GameSettings.DefaultMusic : GameSettings.DefaultSound);
                    if (volume <= 0f) continue;
                    float[] clip = clips[layer.Name];
                    for (int i = 0; i < mix.Length; i++) mix[i] += clip[i % clip.Length] * volume;
                }

                double energy = 0.0;
                float peak = 0f;
                foreach (float s in mix)
                {
                    energy += s * (double)s;
                    peak = Mathf.Max(peak, Mathf.Abs(s));
                }
                rmsDb[stage] = 20f * Mathf.Log10((float)System.Math.Sqrt(energy / mix.Length));
                Assert.Less(peak, 0.95f, "the stage " + stage + " mix clips (peak " + peak + ")");
                Assert.Greater(rmsDb[stage], -40f, "stage " + stage + " is inaudible");
                Assert.Less(rmsDb[stage], -12f, "stage " + stage + " is far too loud");
            }

            Assert.GreaterOrEqual(rmsDb[1] - rmsDb[0], 2f, "the stream is clearly heard");
            Assert.GreaterOrEqual(rmsDb[2] - rmsDb[1], 1f, "the garden is livelier");
            Assert.GreaterOrEqual(rmsDb[3] - rmsDb[2], 0.3f, "the golden hour is the fullest");
            Assert.GreaterOrEqual(rmsDb[3] - rmsDb[0], 4f, "the whole arc");
        }

        static float[] Resample(float[] x, int from, int to)
        {
            if (from == to) return x;
            float[] y = new float[(int)((long)x.Length * to / from)];
            for (int i = 0; i < y.Length; i++)
            {
                double position = (double)i * from / to;
                int i0 = (int)position;
                int i1 = Mathf.Min(i0 + 1, x.Length - 1);
                float f = (float)(position - i0);
                y[i] = x[i0] * (1f - f) + x[i1] * f;
            }
            return y;
        }

        // ---- The scenes ----------------------------------------------------------------------------------------------------------------

        [Test]
        public void TheValleyDirector_HasEveryLayerAndCue_WiredAndFollowingTheStage()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            Assert.AreEqual(1, Object.FindObjectsByType<AudioDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "one listener");

            SerializedObject director = new SerializedObject(First<AudioDirector>());
            Assert.IsTrue(director.FindProperty("followStage").boolValue, "the valley follows the restoration stage");

            SerializedProperty layers = director.FindProperty("layers");
            Assert.AreEqual(AudioLibrary.ValleyLayers.Length, layers.arraySize);
            HashSet<Object> sources = new HashSet<Object>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                AudioLibrary.LayerSpec spec = AudioLibrary.ValleyLayers[i];
                Assert.AreEqual(spec.Name, layer.FindPropertyRelative("name").stringValue);
                Assert.IsNotNull(layer.FindPropertyRelative("clip").objectReferenceValue, spec.Name + " has no clip");
                Object source = layer.FindPropertyRelative("source").objectReferenceValue;
                Assert.IsNotNull(source, spec.Name + " has no AudioSource");
                Assert.IsTrue(sources.Add(source), spec.Name + " shares its AudioSource");
                Assert.AreEqual(spec.Music, layer.FindPropertyRelative("music").boolValue, spec.Name);
                SerializedProperty volumes = layer.FindPropertyRelative("stageVolume");
                Assert.AreEqual(4, volumes.arraySize, spec.Name);
                for (int stage = 0; stage < 4; stage++)
                    Assert.AreEqual(spec.Volumes[stage], volumes.GetArrayElementAtIndex(stage).floatValue, 1e-4f, spec.Name + " at stage " + stage);
            }

            HashSet<SoundCue> found = new HashSet<SoundCue>();
            SerializedProperty sounds = director.FindProperty("cueSounds");
            for (int i = 0; i < sounds.arraySize; i++)
            {
                SerializedProperty element = sounds.GetArrayElementAtIndex(i);
                SoundCue cue = (SoundCue)element.FindPropertyRelative("cue").enumValueIndex;
                Assert.IsTrue(found.Add(cue), cue + " is listed twice");
                Assert.Greater(element.FindPropertyRelative("volume").floatValue, 0f, cue.ToString());
                SerializedProperty clips = element.FindPropertyRelative("clips");
                Assert.Greater(clips.arraySize, 0, cue + " has no clip");
                for (int c = 0; c < clips.arraySize; c++) Assert.IsNotNull(clips.GetArrayElementAtIndex(c).objectReferenceValue, cue + " clip " + c);
            }
            foreach (SoundCue cue in System.Enum.GetValues(typeof(SoundCue)))
            {
                if (cue == SoundCue.None || cue == SoundCue.EndingBegins) continue;   // the ending cue starts the ending piece instead
                Assert.IsTrue(found.Contains(cue), cue + " has no sound");
            }

            Assert.IsNotNull(director.FindProperty("endingClip").objectReferenceValue, "the ending piece");
            foreach (string field in new[] { "effectSource", "uiSource", "stepSource", "endingSource" })
                Assert.IsNotNull(director.FindProperty(field).objectReferenceValue, field);

            FootstepAudio footsteps = First<FootstepAudio>();
            Assert.IsNotNull(footsteps, "the hero walks with footsteps");
            Assert.IsNotNull(new SerializedObject(footsteps).FindProperty("player").objectReferenceValue);
            Assert.IsNotNull(footsteps.GetComponent<PlayerController>());
        }

        [Test]
        public void TheTitleScreenDirector_PlaysTheWelcomeMix_AndKnowsTheInterfaceSounds()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Assert.AreEqual(1, Object.FindObjectsByType<AudioDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            SerializedObject director = new SerializedObject(First<AudioDirector>());
            Assert.IsFalse(director.FindProperty("followStage").boolValue, "the title screen ignores the save");

            SerializedProperty layers = director.FindProperty("layers");
            Assert.AreEqual(AudioLibrary.MenuLayers.Length, layers.arraySize);
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                Assert.AreEqual(AudioLibrary.MenuLayers[i].Name, layer.FindPropertyRelative("name").stringValue);
                Assert.IsNotNull(layer.FindPropertyRelative("clip").objectReferenceValue);
                Assert.IsNotNull(layer.FindPropertyRelative("source").objectReferenceValue);
                Assert.Greater(layer.FindPropertyRelative("stageVolume").GetArrayElementAtIndex(0).floatValue, 0f, "audible on the title screen");
            }

            HashSet<SoundCue> cues = new HashSet<SoundCue>();
            SerializedProperty sounds = director.FindProperty("cueSounds");
            for (int i = 0; i < sounds.arraySize; i++) cues.Add((SoundCue)sounds.GetArrayElementAtIndex(i).FindPropertyRelative("cue").enumValueIndex);
            Assert.IsTrue(cues.Contains(SoundCue.UiClick) && cues.Contains(SoundCue.UiHover));
            Assert.IsNull(director.FindProperty("endingSource").objectReferenceValue, "no ending piece on the title screen");
        }

        // ---- Settings and credits --------------------------------------------------------------------------------------------------------

        [Test]
        public void VolumeSettings_RoundTrip_ClampAndShrugOffADamagedFile()
        {
            string folder = Path.Combine(Path.GetTempPath(), "echoes-settings-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                SaveSystem.UseFolder(folder);
                GameSettings.Reload();
                Assert.AreEqual(GameSettings.DefaultMusic, GameSettings.MusicVolume, "defaults without a file");
                Assert.AreEqual(GameSettings.DefaultSound, GameSettings.SoundVolume);

                GameSettings.MusicVolume = 0.25f;
                GameSettings.SoundVolume = 1.7f;
                Assert.AreEqual(1f, GameSettings.SoundVolume, "clamped to 1");
                Assert.IsTrue(GameSettings.Save());
                GameSettings.MusicVolume = 0.9f;   // changed in memory only
                GameSettings.Reload();
                Assert.AreEqual(0.25f, GameSettings.MusicVolume, 1e-5f, "read back from the file");
                Assert.AreEqual(1f, GameSettings.SoundVolume);

                File.WriteAllText(GameSettings.FilePath, "{ this is not json");
                GameSettings.Reload();
                Assert.AreEqual(GameSettings.DefaultMusic, GameSettings.MusicVolume, "a damaged file gives the defaults");

                File.WriteAllText(GameSettings.FilePath, "{\"version\":1,\"music\":7.5,\"sound\":-2}");
                GameSettings.Reload();
                Assert.AreEqual(1f, GameSettings.MusicVolume, "out of range is clamped");
                Assert.AreEqual(0f, GameSettings.SoundVolume);

                int changes = 0;
                System.Action count = () => changes++;
                GameSettings.Changed += count;
                GameSettings.MusicVolume = 0.5f;
                GameSettings.Changed -= count;
                Assert.AreEqual(1, changes, "a change is announced once");
            }
            finally
            {
                SaveSystem.ClearFolderOverride();
                GameSettings.Reload();
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TheCredits_StateWhatIsTrue_AndDoNotInventAName()
        {
            string text = GameCredits.Text;
            StringAssert.Contains("AI assistance", text);
            StringAssert.Contains("Claude", text);
            StringAssert.Contains("No external art, music, sound or font files", text);
            StringAssert.DoesNotContain("placeholder", text);
            StringAssert.DoesNotContain("Jason Grace", text, "the official theme recording is not used, so its composer is not credited as if it were");

            if (string.IsNullOrEmpty(GameCredits.CreatorDisplayName))
                Assert.AreEqual("Created by the developer of this entry", GameCredits.CreatorLine, "a name is not invented before the developer confirms one");
            else
                StringAssert.Contains(GameCredits.CreatorDisplayName, GameCredits.CreatorLine);

            Assert.AreEqual(3, GameCredits.EndingLines.Length);
            foreach (string line in GameCredits.EndingLines) Assert.IsFalse(string.IsNullOrWhiteSpace(line));
            StringAssert.Contains("AI assistance", string.Join("\n", GameCredits.EndingLines));
        }

        [Test]
        public void EverySpeakerHasATint_AndTheVillagersAreDistinct()
        {
            Color[] tints =
            {
                DialogueBox.SpeakerColor("Mira"), DialogueBox.SpeakerColor("Wren, the Garden Keeper"),
                DialogueBox.SpeakerColor("Alder, the Windmill Restorer"), DialogueBox.SpeakerColor("The Ruined Spring")
            };
            for (int i = 0; i < tints.Length; i++)
                for (int j = i + 1; j < tints.Length; j++)
                    if (i != 3 && j != 3) Assert.AreNotEqual(tints[i], tints[j], "villagers " + i + " and " + j);
            Assert.AreEqual(DialogueBox.SpeakerColor("The Ruined Spring"), DialogueBox.SpeakerColor("Alchemy Workshop"), "places and things share a colour");
            Assert.AreEqual(DialogueBox.SpeakerColor(null), DialogueBox.SpeakerColor(string.Empty));
        }
    }
}

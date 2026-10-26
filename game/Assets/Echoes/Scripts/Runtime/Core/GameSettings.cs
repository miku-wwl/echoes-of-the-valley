using System;
using System.IO;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// The player's volume settings. They live in a small JSON file next to the save, so tests and the smoke run (which move the
    /// save folder) never touch the real settings. A missing, damaged or out-of-range file simply gives the defaults.
    /// </summary>
    public static class GameSettings
    {
        public const string FileName = "echoes_settings.json";
        public const float DefaultMusic = 0.8f;
        public const float DefaultSound = 0.9f;

        [Serializable]
        sealed class Data
        {
            public int version = 1;
            public float music = DefaultMusic;
            public float sound = DefaultSound;
        }

        static Data data = new Data();
        static string loadedFrom;

        /// <summary>Raised when a volume changes.</summary>
        public static event Action Changed;

        public static string FilePath => System.IO.Path.Combine(SaveSystem.Folder, FileName);

        /// <summary>Volume of the music and the ambience beds, 0..1.</summary>
        public static float MusicVolume
        {
            get
            {
                Ensure();
                return data.music;
            }
            set
            {
                Ensure();
                data.music = Clean(value, DefaultMusic);
                Changed?.Invoke();
            }
        }

        /// <summary>Volume of the sound effects and the interface sounds, 0..1.</summary>
        public static float SoundVolume
        {
            get
            {
                Ensure();
                return data.sound;
            }
            set
            {
                Ensure();
                data.sound = Clean(value, DefaultSound);
                Changed?.Invoke();
            }
        }

        /// <summary>Writes the settings; failures are ignored (the game never stops over a volume setting).</summary>
        public static bool Save()
        {
            try
            {
                Ensure();
                Directory.CreateDirectory(SaveSystem.Folder);
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Forgets what was loaded, so the next read comes from the current folder again.</summary>
        public static void Reload() => loadedFrom = null;

        static void Ensure()
        {
            string path = FilePath;
            if (path == loadedFrom) return;   // the folder is the identity: a test or the smoke run switching folders gets its own settings

            loadedFrom = path;
            data = new Data();
            try
            {
                if (!File.Exists(path)) return;
                Data loaded = JsonUtility.FromJson<Data>(File.ReadAllText(path));
                if (loaded == null || loaded.version != 1) return;
                data.music = Clean(loaded.music, DefaultMusic);
                data.sound = Clean(loaded.sound, DefaultSound);
            }
            catch (Exception)
            {
                data = new Data();
            }
        }

        static float Clean(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
    }
}

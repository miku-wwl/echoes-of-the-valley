using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Echoes
{
    public enum LaunchMode { None, New, Continue }

    /// <summary>What the next Valley load should do. Loading the Valley directly (tests, the Editor) always means a clean game.</summary>
    public static class GameLaunch
    {
        public static LaunchMode Mode { get; set; }
    }

    /// <summary>
    /// Local JSON save in Application.persistentDataPath (or an override for tests and the smoke run).
    /// Saving is: write a temp file, read it back and validate it, then replace the main file while keeping the
    /// previous good save as a backup. Loading falls back to that backup, and says so, when the main file is unusable.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "echoes_save.json";

        static string folderOverride;

        public static string Folder => folderOverride ?? Application.persistentDataPath;
        public static string MainPath => Path.Combine(Folder, FileName);
        public static string TempPath => MainPath + ".tmp";
        public static string BackupPath => MainPath + ".bak";

        /// <summary>Where a main file the game cannot read is kept when a new save replaces it (never deleted outright).</summary>
        public static string SetAsidePath => MainPath + ".unreadable";

        /// <summary>True when a main save or a backup exists on disk, readable or not. New Game asks before it can replace either.</summary>
        public static bool HasSaveFiles => File.Exists(MainPath) || File.Exists(BackupPath);

        /// <summary>Redirects saves (tests and -echoes-save-dir) so the player's real save is never touched.</summary>
        public static void UseFolder(string path) => folderOverride = path;

        public static void ClearFolderOverride() => folderOverride = null;

        public static bool Save(GameSession session, out string message)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string json = SaveCodec.Serialize(SaveCodec.ToData(session));
                File.WriteAllText(TempPath, json, new UTF8Encoding(false));

                LoadResult check = SaveCodec.Parse(File.ReadAllText(TempPath));
                if (!check.Ok)
                {
                    File.Delete(TempPath);
                    message = "Save failed verification: " + check.Message;
                    return false;
                }

                if (File.Exists(MainPath))
                {
                    // Keep the old save as the backup only if it is itself good; never overwrite a good backup with a bad file.
                    if (ReadFile(MainPath).Ok) File.Replace(TempPath, MainPath, BackupPath, true);
                    else
                    {
                        // Do not destroy what we could not read (damage, or a save from a newer version): set it aside.
                        if (File.Exists(SetAsidePath)) File.Delete(SetAsidePath);
                        File.Move(MainPath, SetAsidePath);
                        File.Move(TempPath, MainPath);
                    }
                }
                else
                {
                    File.Move(TempPath, MainPath);
                }

                message = "Game saved.";
                return true;
            }
            catch (Exception exception)
            {
                TryDelete(TempPath);
                message = "Could not save: " + exception.Message;
                return false;
            }
        }

        /// <summary>Loads the main save, or the backup when the main file is missing or unusable. Never throws.</summary>
        public static LoadResult Load()
        {
            bool hasMain = File.Exists(MainPath);
            bool hasBackup = File.Exists(BackupPath);
            if (!hasMain && !hasBackup) return new LoadResult(SaveStatus.Missing, null, "no saved game found");

            LoadResult main = hasMain ? ReadFile(MainPath) : new LoadResult(SaveStatus.Missing, null, "the main save is missing");
            if (main.Ok) return main;

            if (hasBackup)
            {
                LoadResult backup = ReadFile(BackupPath);
                if (backup.Ok)
                    return new LoadResult(SaveStatus.Ok, backup.Session, "The main save was unusable (" + main.Message + "); loaded the backup.", true, backup.SourceVersion);
            }
            return main;
        }

        static LoadResult ReadFile(string path)
        {
            try
            {
                return SaveCodec.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                return new LoadResult(SaveStatus.Corrupt, null, "the save could not be read (" + exception.Message + ")");
            }
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // best effort cleanup of a temp file
            }
        }
    }
}

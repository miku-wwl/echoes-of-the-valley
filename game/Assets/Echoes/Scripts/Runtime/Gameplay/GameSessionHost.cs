using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Valley-side owner of the GameSession. On load it creates a clean session (New Game, or loading the scene directly)
    /// or restores the saved one (Continue), advances the session clock, and autosaves at story milestones.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameSessionHost : MonoBehaviour
    {
        [SerializeField] ExplorationObjective objective;

        public static GameSessionHost Instance { get; private set; }

        public GameSession Session { get; private set; }
        public bool LoadedFromSave { get; private set; }

        /// <summary>One-line note about how this session started ("Game loaded.", or why a load fell back to a new game).</summary>
        public string StartupNotice { get; private set; }

        void Awake()
        {
            Instance = this;

            GameSession session = null;
            if (GameLaunch.Mode == LaunchMode.Continue)
            {
                LoadResult load = SaveSystem.Load();
                if (load.Ok)
                {
                    session = load.Session;
                    LoadedFromSave = true;
                    StartupNotice = load.UsedBackup ? load.Message : "Game loaded.";
                }
                else
                {
                    StartupNotice = "Could not load the save (" + load.Message + "). Started a new game.";
                }
            }
            GameLaunch.Mode = LaunchMode.None;

            Session = session ?? new GameSession();
            GameSession.Current = Session;
            Session.Milestone += OnMilestone;
        }

        void Start()
        {
            if (objective != null && Session.ExplorationStep > 0)
                objective.RestoreStep((ObjectiveStep)Session.ExplorationStep);
        }

        void Update()
        {
            if (!GameFlow.IsPaused) Session.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (Session != null) Session.Milestone -= OnMilestone;
            if (Instance == this) Instance = null;
        }

        /// <summary>Writes the session to disk (temp file, verify, replace) and tells the player how it went.</summary>
        public bool SaveNow()
        {
            bool saved = SaveSystem.Save(Session, out string message);
            Session.Notify(message);
            return saved;
        }

        /// <summary>Every story beat (a quest starting or finishing, the ending) is worth a save.</summary>
        void OnMilestone(string key) => SaveNow();
    }
}

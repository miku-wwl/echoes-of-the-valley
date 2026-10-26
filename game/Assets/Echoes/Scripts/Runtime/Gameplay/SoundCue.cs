namespace Echoes
{
    /// <summary>
    /// Every one-shot sound the game can ask for. GameSession raises the gameplay ones right after a transaction commits, so a
    /// sound is heard exactly when the world changed (a rejected action raises only Denied) and never when a save loads.
    /// AudioDirector maps each cue to its clips.
    /// </summary>
    public enum SoundCue
    {
        None = 0,
        GatherHerb,
        GatherFlower,
        GatherOre,
        Craft,
        FeedHen,
        CollectEgg,
        QuestStart,
        HeartAwakened,
        StreamRestored,
        GardenRestored,
        WindmillRestored,
        EndingBegins,
        Denied,
        Footstep,
        UiClick,
        UiHover,
        DialogueTick,
        PanelOpen,
        PanelClose
    }
}

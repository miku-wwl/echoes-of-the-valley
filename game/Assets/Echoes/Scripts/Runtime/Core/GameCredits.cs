namespace Echoes
{
    /// <summary>
    /// The credits shown on the title screen and (in short) on the ending screen. They say what is true of this build: no external
    /// art, music, sound or font files, and everything else made with AI assistance. The developer's public display name has
    /// not been confirmed, so it is left empty on purpose rather than invented; set CreatorDisplayName once it is known.
    /// </summary>
    public static class GameCredits
    {
        /// <summary>The creator's public display name. Empty until the developer confirms it (docs/03-RULES-CREDITS.md).</summary>
        public const string CreatorDisplayName = "";

        public const string Title = "Echoes of the Valley";

        public static string CreatorLine => string.IsNullOrEmpty(CreatorDisplayName) ? "Created by the developer of this entry" : "Created by " + CreatorDisplayName;

        public static readonly string[] EndingLines =
        {
            "A game for thatgamejam #02  ·  Made with Unity",
            "Code, writing, art and sound made with AI assistance (Anthropic Claude Code)",
            "Full credits are on the title screen  ·  Thank you for listening."
        };

        /// <summary>The full credits, one paragraph per line.</summary>
        public static string[] Lines => new[]
        {
            Title,
            "A game for thatgamejam #02, 2026",
            CreatorLine,
            "",
            "Made with AI assistance",
            "All code, dialogue and story text, the 3D art (simple shapes assembled by code) and all music and sound effects (synthesised by code) were created with AI assistance: Anthropic Claude Code, directed by the developer.",
            "",
            "Assets",
            "No external art, music, sound or font files are used. Music and sounds are original and synthesised procedurally. The interface uses the font and sprites built into Unity.",
            "",
            "Built with Unity 6 and its Universal Render Pipeline.",
            "",
            "Thank you for playing."
        };

        public static string Text => string.Join("\n", Lines);
    }
}

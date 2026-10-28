# AI disclosure for Phase 5.2-B3 (thatgamejam #02 requires disclosure of AI-generated material)

**AI-generated art in the B3 pilot.** The playable caretaker's sprites, the three villagers' sprites and all four dialogue busts are made
from illustrations created with **OpenAI image generation** and supplied by the game's owner (`01-heroine-portrait.png`,
`02-npc-lineup.png`, `05-heroine-turnaround-reference.png`; see `asset-manifest.md`). The owner's pack states they are original
characters, not copies of any franchise. They were cut out, animated (2D bone warping), mirrored, cropped and downscaled by scripts;
no human painted over them and they were not hand-drawn. They should be disclosed as **AI-generated character art (OpenAI image
generation), adapted for the game with AI-assisted code (Anthropic Claude Code)**.

**AI-assisted code and documentation.** Anthropic Claude Code (this session reported the model Claude Opus 5.5) wrote, under the
developer's direction: the sprite pipeline in `tools/art/phase52b3/`, the `Echoes/Sprite25D` and `Echoes/ContactShadow` shaders, the
2.5D presenter, fixed camera, dialogue portraits, B3 pilot builder, preview and automated evidence tour, the B3 tests, `docs/09` and the
review package. Earlier phases' AI use is recorded in `docs/03-RULES-CREDITS.md` (OpenAI Codex for early phases and the first 5.2 pilot;
Claude Code for B2).

**Not AI-generated / unchanged.** Gameplay systems, save format, quests, the 36 synthesised WAV files and the Unity built-in font are
unchanged by B3. No third-party art, Atelier Ryza material, marketplace pack, stock image, motion data or font was used.

**In-game credits.** The B3 pilot menu's credits line (version `0.5.2-pilot.3`) states: the caretaker and villager sprites and portraits
are animated and cut from original illustrations made with OpenAI image generation and supplied by the owner; the sprite pipeline,
shaders, fixed camera, foliage and path art were made with Anthropic Claude Code.

**Owner action before any release:** confirm this wording for the itch.io page and the release README, confirm the creator display name,
and review the AI illustrations themselves (the pack's rights note says the owner must review and approve imported images).

# Echoes of the Valley

**Theme-led Unity game jam starter · 2026-10-25 NZDT**

An original, short, peaceful fantasy adventure about **bringing a silent valley back to life** through gathering, caring for a chicken, alchemy and visible restoration.

> **Silence → Awakening → Resonance → Rest**

**GATHER → CARE → CRAFT → RESTORE → WATCH THE WORLD RESPOND**

## Current truth

- **Competition:** [thatgamejam #02](https://itch.io/jam/thatgamejam02), hosted by thatgamecompany.
- **Start:** 2026-10-24 20:00 NZDT; **deadline:** **2026-11-08 20:59 NZDT**. Internal upload target: 2026-11-07.
- **Environment:** Phase 0.5 **PASS** for the separately tested Unity/Windows toolchain (see [baseline](docs/04-BASELINE.md)).
- **Official theme:** released; the supplied 60-second audio's dynamics have been measured and interpreted. The emotional interpretation remains a **design choice**, not a claim of direct human listening.
- **Competition project:** **NOT CREATED IN THIS ZIP**. This is a clean initialization package, not a claimed playable or tested Unity build.
- **Remote:** this ZIP is **not pushed or committed** to GitHub. Integrate into the existing repository intentionally.

## Exact MVP (10–15-minute goal; max 20-minute target)

One compact valley, one original protagonist, three short NPC quests, herbs/ore, a chicken and egg, three recipes, three restoration states, an ending, and local save/load. No combat, fishing, seasons, breeding, online features or large world.

The signature feature is **Valley Resonance**: each important restoration creates a clear, persistent before/after change in color, vegetation, activity and sound. The game does **not** need to be a rhythm game or ship with the official theme audio.

## Docs: four short working references

1. [01 — Game vision and musical theme](docs/01-GAME-DESIGN.md)
2. [02 — Implementation and acceptance gates](docs/02-BUILD-PLAN.md)
3. [03 — Jam rules, provenance and AI disclosure](docs/03-RULES-CREDITS.md)
4. [04 — Verified Phase 0.5 baseline](docs/04-BASELINE.md)

## Code-first Unity initialization (Windows)

Requires the already verified Unity Editor 6000.3.27f1 with a valid Personal license, Windows PowerShell and sufficient D: disk space. From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Init-Unity.ps1
```

This script uses Unity batchmode to create `game/` from Unity's own Universal 3D template, generates a basic owned-content folder layout and leaves logs in ignored `artifacts/bootstrap/`. It **never overwrites** an existing Unity project. It does **not** create gameplay or count as Phase 1 PASS. Open or batch-build the generated project and follow [Phase 1](docs/02-BUILD-PLAN.md) for genuine tests.

**Existing GitHub repo migration:** keep Git history and the original Phase 0.5 evidence commit. Unpack this ZIP separately and review its diff before copying it into your existing checkout. After confirming the new four docs cover your needs, remove the ten old tracked `docs/00-...09-...` files from the **working tree only**, commit the intentional replacement, and push only if you choose. Simply extracting this ZIP on top of the old repository will **not** remove those old docs.

Manual fallback: create a **Universal 3D (URP)** Unity 6000.3.27f1 project in `game/` and keep `.meta` files, `Packages/manifest.json`, `Packages/packages-lock.json` and `ProjectSettings/` in version control.

## Rights and attribution

Original source code is MIT-licensed (see [LICENSE](LICENSE)). That grant does **not** apply to art, audio, fonts, third-party material, documentation or the official theme. **Do not add the official theme WAV to public GitHub.** Maintain an actual-use asset register and make an honest AI-usage disclosure at submission; see [rules](docs/03-RULES-CREDITS.md).

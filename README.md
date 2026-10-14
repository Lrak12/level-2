# Island game — Intro, Level 2 and Level 3

This folder is the complete Unity project. The intro scene, artwork, fonts, and audio are included under `Assets/Intro`. Player movement, boss logic, and the shorter runtime scripts are in `Assets/Scripts`. Other larger runtime systems and animation helpers are grouped under `Assets/Kevin Iglesias/Scripts`; the `ProjectIntegration` and `ProjectSupport` subfolders identify project additions. Editor-only tools are in `Assets/Kevin Iglesias/Scripts/Editor`. See [CODE_GUIDE.md](CODE_GUIDE.md) to find each script. No separate Intro project or original source-art folder is needed.

## Open and play

1. Clone/download this repository.
2. In Unity Hub, add the folder containing `Assets`, `Packages`, and `ProjectSettings`.
3. Open it with **Unity 6000.3.4f1**. Allow Unity to download the packages and import the assets on the first launch.
4. Open **Assets/Intro/Scenes/Intro.unity**.
5. Press **Play**, then **New Game**.

Current game flow: **Intro → Stage2_Battlefield → Stage3_AshenCitadel**.
Level 1 will be added later. The build scene list already starts with the intro.

## Share on GitHub

Use this project folder as the repository root. Commit `Assets/` with all its `.meta` files, `Packages/` with both JSON files, `ProjectSettings/`, and the project documentation. The included `.gitignore` excludes Unity caches, editor-specific settings, generated solution files, and local backups/review captures.

Keep Unity's folder structure intact. Unity regenerates `Library`, `Temp`, and other caches on your friend's computer. First launch requires internet access for package downloads.

## Project notes

- [CODE_GUIDE.md](CODE_GUIDE.md): where to find each gameplay script, function explanations, and questions to practise for a code presentation.
- `PLAYER_CONTROLS.md`: gameplay controls.
- `INTRO_MERGE.md`: merge details and how to insert level 1 later.
- `Tools/MergeIntro.ps1`: optional historical import utility. It references the original local source project and is not needed to open, play, or build this merged project.

The intro-to-level-2 transition, Options and How to Play panels, level 2 pause/restart, and level 3 loading have been checked in Play mode.

# Intro and level 2 merge

Open this project: E:\Unity\Unity Hub\level 2

Starting scene: Assets/Intro/Scenes/Intro.unity
Current build order: Intro -> Stage2_Battlefield -> Stage3_AshenCitadel.
Press Play with Intro open, then choose New Game to enter level 2.

The intro artwork, animated background, menu audio, fonts, options, and help were imported with their asset references. Identical shared assets were reused. The intro scene received a new GUID because the source SampleScene shared a GUID with the destination SampleScene.

The source Intro project, existing gameplay scenes, package versions, and rendering settings were retained. The pre-merge build settings, scene hashes, and imported asset manifest are in IntroMerge_Backup/20261011-161117.

## Adding level 1 later

1. Import level 1 and its dependencies, checking for duplicate asset GUIDs and script class names.
2. Add its scene to Build Profiles / Scene List between Intro and Stage2_Battlefield.
3. In the Intro scene, select the object with MainMenu and set First Gameplay Scene to the full level 1 scene asset path.
4. Set level 1's completion transition to Stage2_Battlefield.

Do not create an empty level 1 scene or a disabled placeholder now. The first gameplay scene is configurable and currently points directly to level 2.

## Validation

Unity 6000.3.4f1 compiled and loaded the imported scene successfully.
Validated menu callbacks, required references, fonts, animated artwork, and all three scenes for missing scripts.
Play mode verified New Game -> level 2, Options/How to Play opening and closing, level 2 pause/restart, level 3 loading, and reloading the intro with normal time and an unlocked cursor.
Level 2's existing next-scene target remains Stage3_AshenCitadel. Its full win/dialogue sequence was not replayed.
See VisualReview/IntroMerge/Validation.txt for the results.
The editor helper `Assets/Kevin Iglesias/Scripts/Editor/IntroMergeValidation.cs` targets this interim level 2-first setup; update its expected first scene when level 1 is added.

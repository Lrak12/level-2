# Find and explain the game's code

The complete game project is **E:\Unity\Unity Hub\level 2**. Its current scene order is **Intro → Stage2_Battlefield → Stage3_AshenCitadel**. Level 1 is not included yet.

The `e3 project` folder contains source models/animations. The separate `Intro` folder is the earlier menu project. Use the scripts inside this merged `level 2` project when studying the complete game.

## Find a script quickly

1. Open the **level 2** project in Unity.
2. In the **Project** window, open **Assets → Scripts** for player movement, boss logic, and the shorter scripts. This folder contains 16 runtime scripts, including `TribalPlayerController.cs` and `Level3FinalBoss.cs`. The other 12 runtime scripts are in **Assets → Kevin Iglesias → Scripts**, grouped by purpose; its **Editor** subfolder contains the 19 editor-only tools. Search from **Assets** to find any script by name, such as `TribalPlayerController` or `IslandGameSession`.
3. Double-click the `.cs` script to open it in your configured code editor.
4. Use **Ctrl+F** inside the file to find a function such as `TryJump` or `TakeDamage`. Search for `TryJump(` to find its calls and declaration.
5. Read the summary directly above the function, then the comments beside its code lines. All 16 scripts directly inside `Assets/Scripts` have line-by-line explanations, including the player controller and final boss. The gameplay scripts also begin with an overall purpose and a pointer back to this guide.

To list scripts by type, use Unity Search with `t:script`. To narrow it down, use `t:script TribalPlayerController`. See Unity's [asset-search documentation](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-navigating-managing/editor-searching/search-overview/search-by-provider/search-project-scenes/search-assets).

You can also select an object in the **Hierarchy**, find its script component in the **Inspector**, and open the referenced script. If double-clicking a script does not open your editor, check **Edit → Preferences → External Tools → External Script Editor**. See [Unity's script inspection guide](https://docs.unity3d.com/6000.3/Documentation/Manual/inspecting-scripts.html).

This guide sits beside `README.md` in the project root, outside `Assets`. Open it from File Explorer or your code editor. The script links below are relative to this project so they still work after sharing the project folder.

## Start with these five scripts

| Topic your professor asks about | Open this script | Functions to find |
| --- | --- | --- |
| Player movement, jump, dash, attacks, health | [TribalPlayerController.cs](Assets/Scripts/TribalPlayerController.cs) | `Update`, `TryJump`, `TryLightAttack`, `TryHeavyAttack`, `ApplyAttackDamage`, `TakeDamage` |
| Score, objectives, lives, checkpoints, pause, win/lose | [IslandGameSession.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Gameplay/IslandGameSession.cs>) | `EnemyDefeated`, `TryFinishObjectives`, `HandleDeath`, `TogglePause`, `Finish` |
| Enemy patrol, chasing, and attacking | [IslandEnemyAI.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Gameplay/IslandEnemyAI.cs>) | `Update`, `Patrol`, `Attack`, `DealDamageAfterWindup`, `TakeDamage` |
| Main menu and starting the game | [MainMenu.cs](Assets/Scripts/MainMenu.cs) | `NewGame`, `OpenOptions`, `OpenHowToPlay`, `QuitGame` |
| On-screen information and gameplay buttons | [IslandGameHUD.cs](Assets/Scripts/IslandGameHUD.cs) | `Awake`, `LateUpdate` |

## Other runtime scripts

| Feature | Script | What to explain |
| --- | --- | --- |
| Camera follows the hero | [CinemachinePlayerFollow.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Camera/CinemachinePlayerFollow.cs>) | `ApplyTarget` assigns both Follow and LookAt. |
| Mouse rotates the view | [CinemachineMouseOrbit.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Camera/CinemachineMouseOrbit.cs>) | `Update` reads the mouse; `PostPipelineStageCallback` applies the orbit. |
| Swords held in both hands | [PlayerSwordEquipper.cs](<Assets/Kevin Iglesias/Scripts/ProjectIntegration/PlayerSwordEquipper.cs>) | `Equip` loads/reuses swords; `BuildGrip` aligns them with the hands. Damage is calculated in the player controller. |
| Player health display | [PlayerHealthBar.cs](Assets/Scripts/PlayerHealthBar.cs) | `LateUpdate` converts health to bar width and color. |
| Enemy health display | [EnemyHealthBar.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/UI/EnemyHealthBar.cs>) | `BuildBar` creates the bar; `Refresh` updates health; `LateUpdate` faces it toward the camera. |
| Boss special attack | [Level3FinalBoss.cs](Assets/Scripts/Level3FinalBoss.cs) | `Slam` warns, waits, checks distance, and applies area damage. |
| Boss health display | [Level3BossHUD.cs](Assets/Scripts/Level3BossHUD.cs) | `LateUpdate` shows the active boss's health. |
| Checkpoints | [IslandCheckpoint.cs](Assets/Scripts/IslandCheckpoint.cs) | `OnTriggerEnter` sends the checkpoint index to the session. |
| Relics | [Level3Relic.cs](Assets/Scripts/Level3Relic.cs) | `OnTriggerEnter` collects once and updates the objective. |
| Healing pickup | [Level3HealingPickup.cs](Assets/Scripts/Level3HealingPickup.cs) | `TryCollect` consumes the pickup only if `HealToFull` succeeds. |
| Hazard damage | [VolcanicHazard.cs](Assets/Scripts/VolcanicHazard.cs) | `TryDamage` limits damage attempts with a cooldown. |
| Recurring weather | [IslandRainCycle.cs](Assets/Scripts/IslandRainCycle.cs) | `Update` alternates clear/effect periods and follows the player. |
| Story and ending | [Level3EndingSequence.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Story/Level3EndingSequence.cs>) | `Begin` starts playback; `PlayStory` reveals pages, controls the camera, and restores state. |
| Running animation stabilization | [HumanoidSwayStabilizer.cs](<Assets/Kevin Iglesias/Scripts/ProjectIntegration/HumanoidSwayStabilizer.cs>) | `LateUpdate` reduces horizontal hip sway. |
| Music, effects, footsteps, trails | [LevelPresentation.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Presentation/LevelPresentation.cs>) | `Play`, `Burst`, `Ring`, and `Hit` coordinate feedback using reusable objects. |
| Character flashes when hit | [ImpactFlash.cs](Assets/Scripts/ImpactFlash.cs) | `Pulse` changes the tint briefly; `Restore` puts it back. |
| Floating objects and flickering lights | [AtmosphereMotion.cs](Assets/Scripts/AtmosphereMotion.cs) | `Update` applies the selected decorative motion. |
| Intro settings | [OptionsMenu.cs](Assets/Scripts/OptionsMenu.cs) | Slider/toggle callbacks change assigned audio sources and fullscreen. This script does not save settings. |
| Menu hover/click sounds | [UIAudio.cs](Assets/Scripts/UIAudio.cs) | `PlayHover` and `PlayClick` play one-shot clips. |
| Moving menu painting | [MenuBackdropMotion.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Presentation/MenuBackdropMotion.cs>) | `EvaluateAt` sizes and moves the artwork for a supplied time. |
| Menu smoke, sparks, and light | [MenuAtmosphere.cs](<Assets/Kevin Iglesias/Scripts/ProjectSupport/Presentation/MenuAtmosphere.cs>) | `OnPopulateMesh` builds decorative UI geometry. |

The 26 game runtime scripts listed above have explanations above every method. `Readme.cs` remains in `Assets/Scripts`. Kevin Iglesias's supplied `SpineProxy.cs` is in `Assets/Kevin Iglesias/Scripts`. Within that folder, **ProjectIntegration** contains the project's animation/weapon helpers, and **ProjectSupport** contains the project's larger gameplay, camera, UI, story, and presentation systems. These subfolders contain project additions; they are not scripts supplied by Kevin Iglesias. See the [folder guide](<Assets/Kevin Iglesias/Scripts/README.md>) for the complete layout.

All 19 editor-only scripts, including `ReadmeEditor.cs` and the scene setup/review tools, are in `Assets/Kevin Iglesias/Scripts/Editor`. These include project setup and validation tools, not just supplied asset code. Keep those tools in an `Editor` subfolder so Unity excludes them from the built game.

## Explain one feature from start to finish

For any function, explain four things: **what calls it, what information it uses, what it changes, and why any important check exists**. Show the relevant lines while explaining.

### Example: left-click attack

1. `TribalPlayerController.Update` detects a fresh left-click and calls `TryLightAttack`.
2. `TryLightAttack` checks whether attacking is allowed. It starts an attack coroutine or queues another combo input.
3. `PlayComboStep` triggers the animation and waits until the strike's impact time.
4. `ApplyAttackDamage` checks a sphere in front of the player. A `HashSet` prevents duplicate damage to an enemy with multiple colliders.
5. `IslandEnemyAI.TakeDamage` reduces enemy health. If it reaches zero, the enemy reports its death to `IslandGameSession.EnemyDefeated`.
6. The session updates score/objectives, and the HUD reads the updated values.

Practice explanation: "The input starts an attack sequence. Damage is delayed to match the swing, then a sphere check finds nearby enemies. Each enemy can be damaged once per strike. Enemy deaths update the shared level progress."

### Example: death and checkpoint respawn

`TribalPlayerController.TakeDamage` marks the player dead at zero health. The session's `Update` notices this and starts `HandleDeath`. That coroutine spends a life, waits briefly, and calls `RespawnAt` with the current checkpoint position if lives remain. The `handlingDeath` flag prevents repeatedly spending lives for one death. At zero lives, the session calls `Finish(false)`.

### Example: winning and changing levels

`TryFinishObjectives` checks the required enemy count, relic count, and boss defeat when a boss is configured. Meeting all requirements adds a remaining-time bonus and calls `Finish(true)`. The HUD can then show Next if another scene is configured. `LoadNextLevel` plays transition dialogue when present, then `EnterNextLevel` loads the scene. A boss-level victory can play an ending story before showing results.

## Questions to practise

| Question | Explanation to practise |
| --- | --- |
| Why use `Awake`, `Start`, and `Update`? | `Awake` caches/initializes component state. `Start` handles setup before the first update. `Update` handles per-frame work. Unity calls these lifecycle methods. |
| Why use `LateUpdate`? | It runs after normal Update work, which suits displays that read the latest state and adjustments made after animation. |
| What is a coroutine? | An `IEnumerator` function started with `StartCoroutine`. `yield return` suspends it so it can continue later without blocking the whole frame. Attack wind-up and respawn delays use this. |
| Why multiply movement by `Time.deltaTime`? | Speed is measured per second; multiplying by frame duration gives the distance for this frame. |
| How does pause work? | The session sets `Time.timeScale` to zero and marks gameplay blocked. Update can still run, so state checks stop input. Dialogue/menu motion uses unscaled time where it must keep moving. |
| Why a `CharacterController` for the player? | `Move` performs collision-constrained movement. The player script supplies direction and vertical velocity, including gravity and jumping. |
| How do enemies choose an action? | `IslandEnemyAI.Update` compares player distance with attack and detection ranges. The NavMeshAgent follows the chosen destination. |
| What is `[SerializeField]`? | It lets Unity serialize a private field and expose it in the Inspector. Scene/prefab values can override the default written in the script. |
| Why use `Instance`? | It gives other scripts access to the current scene's session or presentation component. Their `OnDestroy` methods clear their reference. |
| Does `true` from `TryLightAttack` mean a successful hit? | No. It means the input was accepted. The later hit query decides whether an enemy receives damage. |
| Where do movement and damage numbers come from? | Read the fields at the top of the relevant script, then check the component's Inspector values and any setup calls. Do not assume a source-code default is every scene's active value. |

## Controls to demonstrate

WASD moves relative to the camera; Shift sprints; Space jumps; Q dashes; left-click performs light attacks; right-click performs a heavy attack; Escape pauses. See [PLAYER_CONTROLS.md](PLAYER_CONTROLS.md) for the detailed controls and tuning notes.

The comments are study aids. Practise tracing the code and explaining it in your own words, especially the checks that prevent an action, the state it changes, and the script called next.

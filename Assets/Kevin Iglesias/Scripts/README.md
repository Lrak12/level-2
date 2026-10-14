# Animation helpers and project support scripts

This folder groups the supplied animation helper with the project's animation integration and larger support systems. Player movement is in `Assets/Scripts/TribalPlayerController.cs` and boss logic is in `Assets/Scripts/Level3FinalBoss.cs`, alongside the shorter runtime scripts. The **Editor** subfolder here contains all 19 editor-only tools, including the project's scene setup and validation tools and the supplied `ReadmeEditor.cs`. The project's editor tools were not supplied by Kevin Iglesias.

## Supplied asset script

- [SpineProxy.cs](SpineProxy.cs) is the Kevin Iglesias animation helper. Its original author comment is retained.

## Project additions

The scripts below belong to this game project and were not supplied by Kevin Iglesias. Their subfolder names and purpose labels distinguish them from the original asset code.

| Folder | Script | Purpose |
| --- | --- | --- |
| ProjectIntegration | [HumanoidSwayStabilizer.cs](ProjectIntegration/HumanoidSwayStabilizer.cs) | Reduces horizontal hip sway after animation. |
| ProjectIntegration | [PlayerSwordEquipper.cs](ProjectIntegration/PlayerSwordEquipper.cs) | Attaches and aligns swords with the character's hands. |
| ProjectSupport/Gameplay | [IslandEnemyAI.cs](ProjectSupport/Gameplay/IslandEnemyAI.cs) | Enemy patrol, chasing, attacks, and health. |
| ProjectSupport/Gameplay | [IslandGameSession.cs](ProjectSupport/Gameplay/IslandGameSession.cs) | Shared score, objectives, lives, checkpoints, and level flow. |
| ProjectSupport/Story | [Level3EndingSequence.cs](ProjectSupport/Story/Level3EndingSequence.cs) | Story pages, ending playback, and temporary camera control. |
| ProjectSupport/Camera | [CinemachinePlayerFollow.cs](ProjectSupport/Camera/CinemachinePlayerFollow.cs) | Connects the follow camera to the player. |
| ProjectSupport/Camera | [CinemachineMouseOrbit.cs](ProjectSupport/Camera/CinemachineMouseOrbit.cs) | Applies mouse input to camera orbit. |
| ProjectSupport/UI | [EnemyHealthBar.cs](ProjectSupport/UI/EnemyHealthBar.cs) | Creates and updates enemy health bars. |
| ProjectSupport/Presentation | [MenuAtmosphere.cs](ProjectSupport/Presentation/MenuAtmosphere.cs) | Draws the menu's moving sparks, smoke, and light. |
| ProjectSupport/Presentation | [MenuBackdropMotion.cs](ProjectSupport/Presentation/MenuBackdropMotion.cs) | Sizes and animates the menu artwork. |
| ProjectSupport/Presentation | [LevelPresentation.cs](ProjectSupport/Presentation/LevelPresentation.cs) | Coordinates music, footsteps, and combat effects. |

These remain runtime scripts used by the game. Their method comments explain what each function does. See the [code guide](../../../CODE_GUIDE.md) for study examples and the rest of the scripts.

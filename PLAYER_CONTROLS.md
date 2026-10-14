# Player controls

- WASD: move relative to the camera.
- Shift: sprint.
- Space: jump (1.25 m configured height, with 0.10 seconds of ledge forgiveness).
- Q: grounded dash.
- Left mouse: light attack combo.
- Right mouse: heavy overhead attack.
- Escape: pause.

Heavy attack uses the installed Kevin Iglesias humanoid clip `HumanM@Attack2H01` at 1.15x speed. It deals 70 damage (2x the default light attack), costs 25 stamina, hits after a 0.48-second wind-up, and finishes recovering at 1.4 seconds. Each enemy receives one hit even if it has multiple colliders. The overhead clip is reserved for heavy attack; the light combo uses the one-handed right/left swings.

Jump has a dedicated humanoid takeoff/tuck/extension pose in `Assets/Stage2_Imported/Player/TribalJump.anim`. Landing blends back into locomotion. Attacks and dash require grounded footing; jump cannot interrupt an attack or dash, and holding Space does not repeat jumps. Jump has no stamina cost. All actions respect pause/death, and respawning resets action state.

Movement, jump height, heavy damage multiplier and stamina cost are adjustable on TribalPlayerController. Both gameplay scenes share the updated controller and display the new HUD hints. The original setup tool preserves the animator asset GUID and keeps these states when run again.

Validation: Tools > Island Game > Review Jump and Heavy Attack runs a temporary Play-mode test in both levels. Results and pose previews are written under VisualReview/Controls. Tests do not save their temporary floor, enemy positions or balance overrides.

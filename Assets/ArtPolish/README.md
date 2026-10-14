# Level presentation

Applied to `Stage2_Battlefield` and `Stage3_AshenCitadel` in this project.

- Coast: brighter daylight, softer shadows, turquoise moving water, clustered grass, pollen, campfire flames/smoke and positional fire audio.
- Citadel: cool textured stone, bronze details and glowing inlays, layered ash sky, flowing lava, fire/smoke/embers, floating relics, pickup halos and improved ash fall.
- Both: HDR color grading, bloom, vignette, SMAA, sword trails, hit flashes/sparks, footfall dust, dash feedback, checkpoint bursts and sounds. Citadel also has boss warning/slam effects and relic/healing cues.

## Audio

24 original synthesized WAV files: 2 looping scores, 4 ambient beds and 18 gameplay cues. No downloaded audio samples. The coast uses **Coastal Watch** (38.4 seconds at 100 BPM); the citadel uses **Ashen Procession** (45.714 seconds at 84 BPM). Musical tails wrap across the loop boundary. Ambient loops are 16 seconds.

Select **Level Presentation** in either scene to adjust **Music Volume**, **Ambience Volume** and **Effects Volume**. Defaults are 0.45, 0.32 and 0.8. Positional fires have separate Audio Sources on the Firelight children. Pause ducks music and pauses world audio; scene changes dispose of the previous soundscape.

Long clips stream with Vorbis compression; short cues use ADPCM and decompress on load. Gameplay feedback uses 18 reusable audio sources, 12 particle systems (maximum 100 particles each), and 4 reusable impact rings. Ambient systems have explicit particle caps. New decoration has no colliders.

## Authoring and review

- `Tools/Island Game/Polish Both Levels` reapplies the presentation pass to both scenes. Save current work and exit Play mode first. This replaces the generated presentation objects and resets their tuning to the installer defaults.
- `Tools/Island Game/Validate Presentation and Save Previews` checks saved audio references, shaders, profiles and missing scripts; writes four images and `Validation.txt` into the project's `VisualReview` directory.
- `Tools/compose_level_audio.py` regenerates the original audio using Python and NumPy; the game has no Python dependency.
- The original scenes and changed gameplay scripts are archived in the project-level `ArtPolish_Backup` directory. Original art materials were preserved; the polish uses new material assets.

The Intro/menu project is separate. Gameplay volume controls here are Inspector settings; no menu volume persistence was added.

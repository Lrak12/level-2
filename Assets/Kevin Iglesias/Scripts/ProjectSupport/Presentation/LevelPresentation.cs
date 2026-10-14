// Centralizes level audio and visual feedback, reusing sound voices, particle bursts, and expanding rings.
// Study guide and script map: CODE_GUIDE.md in the project root.

using System.Collections;
using UnityEngine;

public enum LevelSound
{
    Step_1, Step_2, Step_3, Slash_1, Slash_2, Slash_3, Blade_Impact, Player_Hurt,
    Dash, Enemy_Down, Boss_Slam, Boss_Warning, Checkpoint, Relic, Heal, Victory, Defeat, UI_Click
}

/// <summary>Scene-owned soundscape and bounded pools for gameplay feedback.</summary>
public sealed class LevelPresentation : MonoBehaviour
{
    public static LevelPresentation Instance { get; private set; }
    [Header("Soundscape")]
    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip ambience;
    [SerializeField] private AudioClip rainLoop;
    [SerializeField] private AudioClip[] effects;
    [SerializeField, Range(0f, 1f)] private float musicVolume = .45f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = .32f;
    [SerializeField, Range(0f, 1f)] private float effectsVolume = .8f;
    [Header("Feedback")]
    [SerializeField] private Material particleMaterial;
    [SerializeField] private bool volcanic;
    [SerializeField] private TribalPlayerController player;
    [SerializeField] private IslandRainCycle weather;
    private readonly AudioSource[] voices = new AudioSource[18];
    private readonly ParticleSystem[] bursts = new ParticleSystem[12];
    private readonly LineRenderer[] rings = new LineRenderer[4];
    private readonly float[] ringStart = new float[4];
    private readonly float[] ringRadius = new float[4];
    private readonly Color[] ringColor = new Color[4];
    private AudioSource musicSource, ambienceSource, weatherSource, uiSource;
    private TrailRenderer[] swordTrails;
    private int nextVoice, nextBurst, nextRing, step;
    private Vector3 lastPosition;
    private float strideDistance, trailUntil, started;
    private bool listenerPaused;

    // Assign the environment type, player/weather references, audio clips, and particle material during level setup.
    public void Configure(bool citadel, TribalPlayerController owner, IslandRainCycle cycle,
        AudioClip score, AudioClip bed, AudioClip rain, AudioClip[] sounds, Material particles)
    {
        volcanic = citadel; player = owner; weather = cycle;
        music = score; ambience = bed; rainLoop = rain; effects = sounds; particleMaterial = particles;
    }

    // Register this scene's presentation service and create reusable audio, particle, and ring objects.
    // Pooling these effects limits how many objects must be created during combat.
    private void Awake()
    {
        Instance = this;
        AudioListener.pause = false;
        musicSource = Source("Music", music, true, false);
        musicSource.ignoreListenerPause = true;
        ambienceSource = Source("Environment", ambience, true, false);
        weatherSource = Source("Weather", rainLoop, true, false);
        uiSource = Source("Interface and objective cues", null, false, false);
        uiSource.ignoreListenerPause = true;
        for (int i = 0; i < voices.Length; i++) voices[i] = Source("SFX voice " + i, null, false, true);
        for (int i = 0; i < bursts.Length; i++) bursts[i] = CreateBurst(i);
        for (int i = 0; i < rings.Length; i++)
        {
            var go = new GameObject("Impact ring " + i);
            go.transform.SetParent(transform);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = particleMaterial;
            line.loop = true; line.useWorldSpace = true; line.positionCount = 64;
            line.widthMultiplier = .08f; line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false; line.enabled = false;
            rings[i] = line;
        }
        if (player != null) lastPosition = player.transform.position;
        started = Time.unscaledTime;
    }

    // Wait one frame for sword equipment to exist, then attach a disabled TrailRenderer to each blade tip.
    private IEnumerator Start()
    {
        // The equipment component creates both swords in Start.
        yield return null;
        if (player == null) yield break;
        var trails = new System.Collections.Generic.List<TrailRenderer>();
        foreach (Transform bone in player.GetComponentsInChildren<Transform>())
        {
            if (bone.name != "PlayerSword_Right" && bone.name != "PlayerSword_Left") continue;
            var tip = new GameObject("Blade glint trail");
            tip.transform.SetParent(bone, false);
            tip.transform.localPosition = Vector3.up * 2.1f;
            var trail = tip.AddComponent<TrailRenderer>();
            trail.sharedMaterial = particleMaterial; trail.time = .16f;
            trail.startWidth = .2f; trail.endWidth = 0f; trail.minVertexDistance = .06f;
            trail.startColor = new Color(.65f, .9f, 1f, .65f); trail.endColor = new Color(.2f, .7f, 1f, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false; trail.emitting = false;
            trails.Add(trail);
        }
        swordTrails = trails.ToArray();
    }

    // Create and return an AudioSource with the requested looping/spatial settings; start it if a looping clip is supplied.
    private AudioSource Source(string label, AudioClip clip, bool loop, bool spatial)
    {
        var go = new GameObject(label); go.transform.SetParent(transform);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false; source.clip = clip; source.loop = loop; source.volume = 0f;
        source.spatialBlend = spatial ? .8f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 5f; source.maxDistance = 32f; source.dopplerLevel = 0f;
        source.priority = loop ? 160 : 100;
        if (clip != null && loop) source.Play();
        return source;
    }

    // Create and return one pooled particle system, configured for bursts that fade and shrink over their lifetime.
    private ParticleSystem CreateBurst(int index)
    {
        var go = new GameObject("Feedback particles " + index); go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = false; main.loop = false;
        main.maxParticles = 100; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.6f, .3f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var size = ps.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    // Each frame, adjust sound levels for pause/weather, update trails/rings, and trigger footsteps based on travel distance.
    // Ignore teleports and blocked/dead/airborne movement when deciding to create new footstep feedback.
    private void Update()
    {
        var session = IslandGameSession.Instance;
        bool paused = session != null && session.IsPaused;
        if (listenerPaused != paused) { AudioListener.pause = paused; listenerPaused = paused; }
        float fade = Mathf.Clamp01((Time.unscaledTime - started) / 2.5f);
        float duck = paused ? .28f : session != null && session.IsFinished ? .35f : 1f;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicVolume * fade * duck, Time.unscaledDeltaTime * .5f);
        ambienceSource.volume = ambienceVolume * fade;
        weatherSource.volume = Mathf.MoveTowards(weatherSource.volume,
            weather != null && weather.IsRaining ? (volcanic ? .1f : .27f) : 0f, Time.deltaTime * .13f);
        if (swordTrails != null) foreach (var trail in swordTrails)
            if (trail != null) trail.emitting = player != null && !player.IsDead && Time.time < trailUntil;
        UpdateRings();
        if (player == null) return;
        Vector3 current = player.transform.position;
        float distance = Vector3.Distance(current, lastPosition);
        lastPosition = current;
        if (player.IsDead || (session != null && session.BlocksGameplay) || distance > 2f) { strideDistance = 0; return; }
        if (distance < .002f) return;
        var controller = player.GetComponent<CharacterController>();
        if (controller != null && !controller.isGrounded) return;
        strideDistance += distance;
        if (strideDistance < 1.25f) return;
        strideDistance = 0;
        Play((LevelSound)((step++) % 3), current, .48f);
        Burst(current + Vector3.up * .05f, volcanic ? new Color(.48f, .42f, .38f, .24f) : new Color(.7f, .61f, .4f, .22f), 5, .5f, .19f);
    }

    // Expand and fade active ring effects, then disable each ring when its short lifetime ends.
    private void UpdateRings()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            var ring = rings[i]; if (!ring.enabled) continue;
            float t = (Time.time - ringStart[i]) / .65f;
            if (t >= 1) { ring.enabled = false; continue; }
            float radius = Mathf.Lerp(.2f, ringRadius[i], 1 - (1-t)*(1-t));
            Color c = ringColor[i]; c.a *= 1-t;
            ring.startColor = ring.endColor = c;
            ring.widthMultiplier = Mathf.Lerp(.15f, .035f, t);
            for (int j = 0; j < 64; j++)
            {
                float angle = j * Mathf.PI * 2 / 64;
                ring.SetPosition(j, ring.transform.position + new Vector3(Mathf.Cos(angle)*radius, 0, Mathf.Sin(angle)*radius));
            }
        }
    }

    // Look up a LevelSound clip and play it through the UI source or an available positional sound voice.
    // If all positional voices are busy, skip the new sound instead of creating another AudioSource.
    public void Play(LevelSound sound, Vector3 position, float volume = 1f, bool interfaceCue = false)
    {
        int index = (int)sound;
        if (effects == null || index >= effects.Length || effects[index] == null) return;
        if (interfaceCue)
        {
            uiSource.volume = effectsVolume;
            uiSource.PlayOneShot(effects[index], volume);
            return;
        }
        AudioSource voice = null;
        for (int i = 0; i < voices.Length; i++)
        {
            var candidate = voices[(nextVoice + i) % voices.Length];
            if (!candidate.isPlaying) { voice = candidate; nextVoice = (nextVoice+i+1)%voices.Length; break; }
        }
        if (voice == null) return;
        voice.transform.position = position; voice.clip = effects[index];
        voice.pitch = sound <= LevelSound.Enemy_Down ? Random.Range(.94f, 1.06f) : 1f;
        voice.volume = volume * effectsVolume; voice.Play();
    }

    // Reuse the next particle system, clear its old particles, and emit a limited burst at the requested world position.
    public void Burst(Vector3 position, Color color, int count = 25, float speed = 2f, float size = .12f)
    {
        var ps = bursts[nextBurst++ % bursts.Length];
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.transform.position = position;
        var main = ps.main; main.gravityModifier = .22f;
        ps.Play();
        for (int i = 0; i < Mathf.Min(count, 100); i++)
        {
            Vector3 velocity = Random.onUnitSphere * Random.Range(.25f, 1f) * speed;
            velocity.y = Mathf.Abs(velocity.y) + .2f;
            var emit = new ParticleSystem.EmitParams { position = position, velocity = velocity,
                startLifetime = Random.Range(.24f, .65f), startSize = Random.Range(.6f, 1.4f) * size, startColor = color };
            ps.Emit(emit, 1);
        }
    }

    // Reuse the next ring, store its position/color/radius and start time, then display its initial shape.
    public void Ring(Vector3 position, Color color, float radius)
    {
        int i = nextRing++ % rings.Length;
        rings[i].transform.position = position + Vector3.up * .12f;
        ringStart[i] = Time.time; ringRadius[i] = radius; ringColor[i] = color; rings[i].enabled = true;
        UpdateRings();
    }

    // Play the selected light-attack sound; for player attacks, enable the normal sword trails briefly.
    public void Swing(Transform actor, int combo, bool isPlayer)
    {
        Play((LevelSound)((int)LevelSound.Slash_1 + Mathf.Clamp(combo-1,0,2)), actor.position, isPlayer ? .8f : .48f);
        if (isPlayer)
        {
            SetSwordTrailStyle(false);
            trailUntil = Time.time + .6f;
        }
    }

    // Play the heavy swing sound and briefly enable the wider, warm-colored sword trails.
    public void HeavySwing(Transform actor)
    {
        Play(LevelSound.Slash_3, actor.position, 1f);
        SetSwordTrailStyle(true);
        trailUntil = Time.time + .5f;
    }

    // Choose the width and colors for normal or heavy player sword trails.
    private void SetSwordTrailStyle(bool heavy)
    {
        if (swordTrails == null) return;
        foreach (var trail in swordTrails) if (trail != null)
        {
            trail.startWidth = heavy ? .30f : .20f;
            trail.startColor = heavy ? new Color(1f, .68f, .25f, .8f) : new Color(.65f, .9f, 1f, .65f);
            trail.endColor = heavy ? new Color(1f, .35f, .08f, 0f) : new Color(.2f, .7f, 1f, 0f);
        }
    }

    // Choose a hurt/impact/death sound, emit hit particles, and pulse the actor's ImpactFlash if one is attached.
    public void Hit(Transform actor, bool isPlayer, bool killed)
    {
        Play(isPlayer ? LevelSound.Player_Hurt : killed ? LevelSound.Enemy_Down : LevelSound.Blade_Impact, actor.position, .85f);
        Burst(actor.position + Vector3.up, isPlayer ? new Color(1, .23f, .13f, .8f) : new Color(1.7f, .85f, .25f, 1), killed ? 42 : 24, killed ? 3 : 2.2f);
        var flash = actor.GetComponent<ImpactFlash>(); if (flash != null) flash.Pulse();
    }

    // Combine a UI/objective sound, particle burst, and expanding ring for rewards or encounter announcements.
    public void Reward(LevelSound sound, Vector3 position, Color color)
    {
        Play(sound, position, .75f, true); Burst(position + Vector3.up, color, 52, 2.3f, .15f); Ring(position, color, 2.5f);
    }

    // When this active presentation service is destroyed, clear its shared reference and reset the audio listener pause.
    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null; AudioListener.pause = false;
    }
}

using UnityEngine;

/// <summary>
/// A small, asset-free ambient score. It creates one seamless loop at runtime,
/// so the project has music without needing to ship a separate audio file.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GameMusic : MonoBehaviour
{
    private const int SampleRate = 44100;
    private const float LoopSeconds = 32f;

    [Header("Music")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.22f;
    [SerializeField] private bool startMuted;

    private AudioSource musicSource;
    private static GameMusic instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartMusic()
    {
        if (instance != null)
            return;

        GameObject musicObject = new GameObject("Game Music");
        instance = musicObject.AddComponent<GameMusic>();
        DontDestroyOnLoad(musicObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = GetComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = startMuted ? 0f : volume;
        musicSource.clip = BuildAmbientLoop();
        musicSource.Play();
    }

    /// <summary>Lets menus or other gameplay code change music volume later.</summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (musicSource != null)
            musicSource.volume = volume;
    }

    private static AudioClip BuildAmbientLoop()
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * LoopSeconds);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(7319);

        // Frequencies form D minor. Every oscillator completes a whole number
        // of cycles inside the buffer, keeping the join completely seamless.
        float[] droneFrequencies = { 36.0f, 54.0f, 72.0f, 90.0f };
        float[] droneVolumes = { 0.34f, 0.19f, 0.13f, 0.08f };
        int[] melody = { 144, 171, 162, 216, 192, 171, 162, 144 };

        float noise = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float value = 0f;

            // Slowly breathing underground drone.
            float breath = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * t / 8f);
            for (int voice = 0; voice < droneFrequencies.Length; voice++)
            {
                float phase = 2f * Mathf.PI * droneFrequencies[voice] * t;
                value += Mathf.Sin(phase) * droneVolumes[voice] * breath;
                value += Mathf.Sin(phase * 2f) * droneVolumes[voice] * 0.08f;
            }

            // One soft, root-like pulse every four seconds.
            int noteIndex = Mathf.FloorToInt(t / 4f) % melody.Length;
            float noteTime = t % 4f;
            float envelope = Mathf.Exp(-noteTime * 1.35f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, noteTime * 18f));
            float notePhase = 2f * Mathf.PI * melody[noteIndex] * t;
            value += (Mathf.Sin(notePhase) + 0.22f * Mathf.Sin(notePhase * 2f)) * envelope * 0.12f;

            // Filtered noise adds a very quiet organic texture.
            float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
            noise += (whiteNoise - noise) * 0.008f;
            value += noise * 0.035f;

            // Fade the texture at the seam while the tonal voices stay periodic.
            float seamDistance = Mathf.Min(t, LoopSeconds - t);
            float seamFade = Mathf.Clamp01(seamDistance * 2f);
            samples[i] = Mathf.Clamp(value * (0.92f + seamFade * 0.08f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Roots Ambient Loop", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

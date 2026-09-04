using UnityEngine;

/// <summary>
/// Starts and preserves the game's looping soundtrack across scene reloads.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GameMusic : MonoBehaviour
{
    private const string MusicResourcePath =
        "music/Tim-Struik-Area-music-prelude-2026-06-16-10_31";

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
        musicSource.clip = Resources.Load<AudioClip>(MusicResourcePath);

        if (musicSource.clip == null)
        {
            Debug.LogError(
                $"Music clip was not found at Resources/{MusicResourcePath}."
            );
            return;
        }

        musicSource.Play();
    }

    /// <summary>Lets menus or other gameplay code change music volume later.</summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (musicSource != null)
            musicSource.volume = volume;
    }

}

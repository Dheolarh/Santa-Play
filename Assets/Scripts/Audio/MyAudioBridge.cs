using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MyAudioBridge : MonoBehaviour
{
    [Header("Background Music")]
    [Tooltip("The background music track for the game.")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private bool loopMusic = true;

    [Header("Audio Clips (SFX)")]
    [Tooltip("Sound played when catching a snowflake.")]
    [SerializeField] private AudioClip flakeCaughtSound;

    [Tooltip("Sound played when catching a bomb / explosion.")]
    [SerializeField] private AudioClip bombCaughtSound;

    [Tooltip("Sound played when a shockwave happens.")]
    [SerializeField] private AudioClip shockwaveSound;

    [Tooltip("Sound played when flipping the net.")]
    [SerializeField] private AudioClip netFlipSound;

    [Header("Melt Sound")]
    [Tooltip("Sound played while the snow in the net melts.")]
    [SerializeField] private AudioClip meltSound;
    [Range(0f, 1f)]
    [SerializeField] private float meltVolume = 0.8f;
    [Tooltip("Skip this many seconds at the start of the clip (cuts leading silence).")]
    [SerializeField, Min(0f)] private float meltStartOffset = 0f;
    [Tooltip("When the melt ends, the sound fades out over this many seconds. If the clip is longer than the melt, it gets cut here.")]
    [SerializeField, Min(0.01f)] private float meltFadeOut = 0.15f;

    private AudioSource audioSource;
    private AudioSource meltSource;
    private Coroutine meltFadeRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Separate source so the melt sound can be stopped on its own
        // without touching the music or other SFX.
        meltSource = gameObject.AddComponent<AudioSource>();
        meltSource.playOnAwake = false;
        meltSource.loop = false;
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void OnEnable()
    {
        GameEvents.FlakeCaught += OnFlakeCaught;
        GameEvents.BombCaught += OnBombCaught;
        GameEvents.ShockwaveStarted += OnShockwave;
        GameEvents.NetFlipped += OnNetFlipped;
        GameEvents.MeltStarted += OnMeltStarted;
        GameEvents.MeltFinished += OnMeltFinished;
    }

    private void OnDisable()
    {
        GameEvents.FlakeCaught -= OnFlakeCaught;
        GameEvents.BombCaught -= OnBombCaught;
        GameEvents.ShockwaveStarted -= OnShockwave;
        GameEvents.NetFlipped -= OnNetFlipped;
        GameEvents.MeltStarted -= OnMeltStarted;
        GameEvents.MeltFinished -= OnMeltFinished;
    }

    // --- Background Music Control ---

    private void PlayBackgroundMusic()
    {
        if (backgroundMusicClip != null && audioSource != null)
        {
            audioSource.clip = backgroundMusicClip;
            audioSource.loop = loopMusic;
            audioSource.volume = musicVolume;
            audioSource.Play();
        }
    }

    // --- Sound Triggers (SFX) ---

    private void OnFlakeCaught(Flake f)
    {
        PlaySound(flakeCaughtSound);
    }

    private void OnBombCaught(Bomb b, int loss)
    {
        PlaySound(bombCaughtSound);
    }

    private void OnShockwave(Vector3 pos)
    {
        PlaySound(shockwaveSound);
    }

    private void OnNetFlipped(float side)
    {
        PlaySound(netFlipSound);
    }

    // --- Melt sound: starts with the melt, fades out when the melt finishes ---

    private void OnMeltStarted(int flakeCount)
    {
        if (meltSound == null || meltSource == null) return;

        if (meltFadeRoutine != null)
        {
            StopCoroutine(meltFadeRoutine);
            meltFadeRoutine = null;
        }

        meltSource.Stop();
        meltSource.clip = meltSound;
        meltSource.volume = meltVolume;
        meltSource.time = Mathf.Clamp(meltStartOffset, 0f, Mathf.Max(0f, meltSound.length - 0.01f));
        meltSource.Play();
    }

    private void OnMeltFinished()
    {
        if (meltSource == null || !meltSource.isPlaying) return;

        if (meltFadeRoutine != null) StopCoroutine(meltFadeRoutine);
        meltFadeRoutine = StartCoroutine(FadeOutMelt());
    }

    private IEnumerator FadeOutMelt()
    {
        float startVolume = meltSource.volume;
        float t = 0f;

        while (t < meltFadeOut && meltSource.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            meltSource.volume = Mathf.Lerp(startVolume, 0f, t / meltFadeOut);
            yield return null;
        }

        meltSource.Stop();
        meltSource.volume = meltVolume;
        meltFadeRoutine = null;
    }

    // Helper method to overlap SFX cleanly without cutting off background music
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
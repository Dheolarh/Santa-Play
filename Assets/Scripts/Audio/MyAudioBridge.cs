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

    [Tooltip("Volume of the SFX above. Separate from the music volume, so fading the music never fades them.")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.5f;

    [Header("Melt Sound")]
    [Tooltip("Sound played while the snow in the net melts.")]
    [SerializeField] private AudioClip meltSound;
    [Range(0f, 1f)]
    [SerializeField] private float meltVolume = 0.8f;
    [Tooltip("Skip this many seconds at the start of the clip (cuts leading silence).")]
    [SerializeField, Min(0f)] private float meltStartOffset = 0f;
    [Tooltip("When the melt ends, the sound fades out over this many seconds. If the clip is longer than the melt, it gets cut here.")]
    [SerializeField, Min(0.01f)] private float meltFadeOut = 0.15f;

    [Header("Time Running Out")]
    [Tooltip("Loops from the moment the warning starts (30s left) until time is up.")]
    [SerializeField] private AudioClip timeWarningLoopSound;
    [Range(0f, 1f)]
    [SerializeField] private float timeWarningVolume = 0.8f;

    [Header("Game Over")]
    [Tooltip("Plays once when the timer reaches 0.")]
    [SerializeField] private AudioClip gameOverSound;
    [Range(0f, 1f)]
    [SerializeField] private float gameOverVolume = 1f;
    [Tooltip("Fade the background music out when the game ends.")]
    [SerializeField] private bool fadeMusicOnGameOver = true;
    [SerializeField, Min(0.01f)] private float musicFadeOut = 0.5f;

    private AudioSource musicSource;   // background music only
    private AudioSource sfxSource;     // one-shot sound effects
    private AudioSource meltSource;    // melt sound (can be cut short)
    private AudioSource warningSource; // time-running-out loop
    private Coroutine meltFadeRoutine;
    private Coroutine musicFadeRoutine;

    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();

        // Separate sources so each sound can be stopped or faded on its own.
        sfxSource = CreateSource(false);
        meltSource = CreateSource(false);
        warningSource = CreateSource(true);
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        return source;
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
        GameEvents.TimeWarningStarted += OnTimeWarningStarted;
        GameEvents.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.FlakeCaught -= OnFlakeCaught;
        GameEvents.BombCaught -= OnBombCaught;
        GameEvents.ShockwaveStarted -= OnShockwave;
        GameEvents.NetFlipped -= OnNetFlipped;
        GameEvents.MeltStarted -= OnMeltStarted;
        GameEvents.MeltFinished -= OnMeltFinished;
        GameEvents.TimeWarningStarted -= OnTimeWarningStarted;
        GameEvents.GameOver -= OnGameOver;
    }

    // --- Background Music Control ---

    private void PlayBackgroundMusic()
    {
        if (backgroundMusicClip != null && musicSource != null)
        {
            musicSource.clip = backgroundMusicClip;
            musicSource.loop = loopMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
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

    // --- Time running out: loop from the warning until game over ---

    private void OnTimeWarningStarted()
    {
        if (timeWarningLoopSound == null || warningSource == null) return;

        warningSource.clip = timeWarningLoopSound;
        warningSource.volume = timeWarningVolume;
        warningSource.loop = true;
        warningSource.Play();
    }

    // --- Game over: stop the loop, play the game over sound, fade the music ---

    private void OnGameOver()
    {
        if (warningSource != null) warningSource.Stop();

        if (gameOverSound != null && sfxSource != null)
            sfxSource.PlayOneShot(gameOverSound, gameOverVolume);

        if (fadeMusicOnGameOver && musicSource != null && musicSource.isPlaying)
        {
            if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = StartCoroutine(FadeOutMusic());
        }
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < musicFadeOut)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeOut);
            yield return null;
        }

        musicSource.Stop();
        musicFadeRoutine = null;
    }

    // Helper method to overlap SFX cleanly without cutting off background music
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
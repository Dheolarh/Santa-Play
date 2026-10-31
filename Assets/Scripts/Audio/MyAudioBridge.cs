using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MyAudioBridge : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Sound played when catching a snowflake.")]
    [SerializeField] private AudioClip flakeCaughtSound;

    [Tooltip("Sound played when catching a bomb / explosion.")]
    [SerializeField] private AudioClip bombCaughtSound;

    [Tooltip("Sound played when a shockwave happens.")]
    [SerializeField] private AudioClip shockwaveSound;

    [Tooltip("Sound played when flipping the net.")]
    [SerializeField] private AudioClip netFlipSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GameEvents.FlakeCaught += OnFlakeCaught;
        GameEvents.BombCaught += OnBombCaught;
        GameEvents.ShockwaveStarted += OnShockwave;
        GameEvents.NetFlipped += OnNetFlipped;
    }

    private void OnDisable()
    {
        GameEvents.FlakeCaught -= OnFlakeCaught;
        GameEvents.BombCaught -= OnBombCaught;
        GameEvents.ShockwaveStarted -= OnShockwave;
        GameEvents.NetFlipped -= OnNetFlipped;
    }

    // --- Sound Triggers ---

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

    // Helper method to overlap sounds cleanly without cutting off
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
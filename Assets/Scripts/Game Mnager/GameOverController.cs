using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// When the timer hits 0: stops the gameplay and shows the game over screen.
// Put this on an object that stays ACTIVE (e.g. an empty "GameManager"), not on the game over panel itself.
public class GameOverController : MonoBehaviour
{
    [Header("Game Over Screen")]
    [Tooltip("Your game over panel. It is hidden at the start and shown at game over.")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Optional. An Image inside your game over panel (banner, title, background...).")]
    [SerializeField] private Image gameOverImage;
    [Tooltip("Optional. Drop your game over sprite here and it is placed on the Image above. Swap it any time.")]
    [SerializeField] private Sprite gameOverSprite;
    [Tooltip("Optional. Text inside the panel that shows the final score.")]
    [SerializeField] private TMP_Text finalScoreText;
    [Tooltip("{0} becomes the final score. Example: Score: {0}")]
    [SerializeField] private string finalScoreFormat = "{0}";
    [Tooltip("Seconds to wait after time is up before the panel appears (lets the game over sound start first).")]
    [SerializeField, Min(0f)] private float showDelay = 0.6f;

    [Header("Stopped At Game Over (empty = found automatically)")]
    [SerializeField] private SantaController santa;
    [SerializeField] private NetCatcher netCatcher;
    [SerializeField] private FlakeSpawner flakeSpawner;
    [SerializeField] private BombSpawner bombSpawner;
    [SerializeField] private GameSpeed gameSpeed;

    private bool isOver;
    private Coroutine showRoutine;

    private void Awake()
    {
        if (santa == null) santa = FindFirstObjectByType<SantaController>();
        if (netCatcher == null) netCatcher = FindFirstObjectByType<NetCatcher>();
        if (flakeSpawner == null) flakeSpawner = FindFirstObjectByType<FlakeSpawner>();
        if (bombSpawner == null) bombSpawner = FindFirstObjectByType<BombSpawner>();
        if (gameSpeed == null) gameSpeed = FindFirstObjectByType<GameSpeed>();

        if (gameOverImage != null && gameOverSprite != null)
            gameOverImage.sprite = gameOverSprite;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.GameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        if (isOver) return;
        isOver = true;

        StopGameplay();

        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowScreen());
    }

    private void StopGameplay()
    {
        if (santa != null) santa.SetControlEnabled(false);
        if (netCatcher != null) netCatcher.SetCatchingEnabled(false);
        if (flakeSpawner != null) flakeSpawner.StopSpawning();
        if (bombSpawner != null) bombSpawner.SetSpawningEnabled(false);
        if (gameSpeed != null) gameSpeed.SetAutoRamp(false);
    }

    private IEnumerator ShowScreen()
    {
        if (showDelay > 0f) yield return new WaitForSecondsRealtime(showDelay);

        if (finalScoreText != null)
        {
            int score = netCatcher != null ? netCatcher.TotalScore : 0;
            finalScoreText.text = string.Format(finalScoreFormat, score);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        showRoutine = null;
    }

    // Hook this to a Restart button's OnClick.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

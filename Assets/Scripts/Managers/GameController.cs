using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Referanslar")]
    public MidiGameManager gameManager;
    public SongPlayer songPlayer;

    [Header("Klavye Kısayolları")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode restartKey = KeyCode.R;
    public KeyCode statsKey = KeyCode.Tab;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(statsKey))
        {
            ShowStats();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            Debug.Log("═══ OYUN DURAKLATILDI ═══");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("═══ OYUN DEVAM EDİYOR ═══");
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameManager != null)
            gameManager.ResetGame();

        if (songPlayer != null)
            songPlayer.RestartSong();

        Debug.Log("═══ OYUN YENİDEN BAŞLATILDI ═══");
    }

    public void ShowStats()
    {
        if (gameManager != null)
        {
            gameManager.PrintStats();
        }
    }

    void OnApplicationQuit()
    {
        if (gameManager != null)
        {
            Debug.Log("═══ SON İSTATİSTİKLER ═══");
            gameManager.PrintStats();
        }
    }
}
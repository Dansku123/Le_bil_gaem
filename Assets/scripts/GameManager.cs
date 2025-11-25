using UnityEngine;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static event Action<bool> OnGameRunningChanged; // subscribers (e.g. movement scripts) should disable input when false

    public TextMeshProUGUI lapText;
    [SerializeField] private CheckpointTarkistus playerCT;

    public int lapsToWin = 3;

    // UI for win screen
    public GameObject winPanel;                 // assign a panel GameObject (inactive by default)
    public Image winBackground;                 // the panel's Image component or a full-screen Image
    public TextMeshProUGUI winTitleText;        // title text on the win panel
    public Color playerWinColor = Color.blue;
    public Color clankerWinColor = Color.red;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Use this to check whether gameplay is allowed
    public bool gameRunning = true;
    public bool IsGameRunning => gameRunning;

    private bool winShown = false;

    void Start()
    {
        // stop the game at start (gameRunning false but not pausing time)
        SetGameRunning(false, pauseTime: false);

        // ensure win panel is hidden at start
        if (winPanel != null) winPanel.SetActive(false);
    }

    void Update()
    {
        int laps = 0;

        if (playerCT != null)
        {
            var t = playerCT.GetType();

            // try to read a public field named "laps" or "Laps"
            var f = t.GetField("laps");
            if (f != null && f.FieldType == typeof(int))
            {
                laps = (int)f.GetValue(playerCT);
            }
            else
            {
                // try properties "laps" then "Laps"
                var p = t.GetProperty("laps");
                if (p != null && p.PropertyType == typeof(int))
                {
                    laps = (int)p.GetValue(playerCT, null);
                }
                else
                {
                    var p2 = t.GetProperty("Laps");
                    if (p2 != null && p2.PropertyType == typeof(int))
                    {
                        laps = (int)p2.GetValue(playerCT, null);
                    }
                }
            }
        }

        if (lapText is not null)
            lapText.text = "Lap: " + laps.ToString() + "/" + lapsToWin.ToString();

        // check player win
        if (!winShown && laps >= lapsToWin)
        {
            PlayerWon();
        }
    }

    // Call these from other scripts when appropriate (e.g. enemy/Clanker detects its own win)
    public void PlayerWon()
    {
        ShowWinScreen(true);
    }

    public void ClankerWon()
    {
        ShowWinScreen(false);
    }

    private void ShowWinScreen(bool playerWon)
    {
        if (winShown) return;
        winShown = true;

        // stop gameplay and pause time
        SetGameRunning(false, pauseTime: true);

        if (winPanel != null)
            winPanel.SetActive(true);

        if (winBackground != null)
            winBackground.color = playerWon ? playerWinColor : clankerWinColor;

        if (winTitleText != null)
            winTitleText.text = playerWon ? "You Win!" : "Clanker Wins!";
    }

    // Central setter so other systems receive the change (movement scripts should subscribe to OnGameRunningChanged)
    private void SetGameRunning(bool running, bool pauseTime)
    {
        gameRunning = running;
        OnGameRunningChanged?.Invoke(gameRunning);
        Time.timeScale = pauseTime ? 0f : 1f;
    }

    // Optional: call to reset time scale and UI if you restart the scene
    public void ResetWinState()
    {
        winShown = false;
        if (winPanel != null) winPanel.SetActive(false);
        SetGameRunning(true, pauseTime: false);
    }
}

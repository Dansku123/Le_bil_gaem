using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TextMeshProUGUI lapText;
    [SerializeField] private CheckpointTarkistus playerCT;

    public int lapsToWin = 3;

    public bool gameRunning = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // void Start()
    // {
    //     lapText = GameObject.Find("LapText").GetComponent<TextMeshProUGUI>();
    // }

    // Update is called once per frame
    void Update()
    {
        lapText.text = "Lap: " + playerCT.laps.ToString() + "/" + lapsToWin.ToString();
    }
}
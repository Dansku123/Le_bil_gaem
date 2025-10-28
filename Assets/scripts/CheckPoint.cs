using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//
// Attach CheckPoint to each checkpoint collider (isTrigger = true).
// Attach CheckpointManager to one GameObject in the scene (or the car).
//
// Usage:
// - CheckPoint.index: set 0..N-1 for ordered checkpoints.
// - CheckpointManager.requireOrdered: if true, checkpoints must be hit in sequence.
// - Tag the player/car GameObject with "Player" (or change playerTag).
// - Subscribe to CheckpointManager.OnPlayerWin to handle win.
//

public class CheckPoint : MonoBehaviour
{
    [Tooltip("Sequential index for this checkpoint (0..N-1).")]
    public int index = 0;

    [Tooltip("Tag used to detect player/car.")]
    public string playerTag = "Player";

    void Reset()
    {
        // Make sure collider is a trigger by default
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (CheckpointManager.Instance == null) return;
        CheckpointManager.Instance.RecordCheckpoint(index, other.gameObject);
    }
}

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("If true, checkpoints must be passed in order (0..N-1). Otherwise any order is accepted.")]
    public bool requireOrdered = true;

    [Tooltip("Number of laps required to win. A lap is completed when all checkpoints are visited.")]
    public int lapsToWin = 1;

    [Tooltip("Tag used to detect player/car (used for initial lookup).")]
    public string playerTag = "Player";

    // Fired when a player wins. Parameter is the winning player GameObject.
    public static event Action<GameObject> OnPlayerWin;

    // Internal progress per player
    class Progress
    {
        public HashSet<int> visited = new HashSet<int>();
        public int nextIndex = 0;
        public int lapsCompleted = 0;
    }

    Dictionary<GameObject, Progress> progressByPlayer = new Dictionary<GameObject, Progress>();

    int totalCheckpoints = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Discover all checkpoints and compute total count based on unique indices or count
        var cps = FindObjectsOfType<CheckPoint>();
        if (cps.Length == 0)
        {
            Debug.LogWarning("CheckpointManager: No checkpoints found in scene.");
            totalCheckpoints = 0;
            return;
        }

        // Prefer using max index + 1 if indices are set, otherwise use number of objects
        int maxIndex = cps.Max(c => c.index);
        totalCheckpoints = Mathf.Max(cps.Length, maxIndex + 1);
    }

    public void RecordCheckpoint(int index, GameObject player)
    {
        if (totalCheckpoints <= 0) return;

        if (!progressByPlayer.TryGetValue(player, out var prog))
        {
            prog = new Progress();
            progressByPlayer[player] = prog;
        }

        if (requireOrdered)
        {
            // Accept only expected next index
            if (index != prog.nextIndex)
            {
                // Ignore out-of-order hits (could optionally reset progress)
                return;
            }

            prog.nextIndex++;

            if (prog.nextIndex >= totalCheckpoints)
            {
                prog.nextIndex = 0;
                prog.lapsCompleted++;
                Debug.Log($"CheckpointManager: {player.name} completed lap {prog.lapsCompleted}/{lapsToWin}");
                if (prog.lapsCompleted >= lapsToWin)
                {
                    Win(player);
                }
            }
        }
        else
        {
            // Any order: mark visited
            if (prog.visited.Add(index))
            {
                if (prog.visited.Count >= totalCheckpoints)
                {
                    prog.lapsCompleted++;
                    prog.visited.Clear();
                    Debug.Log($"CheckpointManager: {player.name} completed lap {prog.lapsCompleted}/{lapsToWin}");
                    if (prog.lapsCompleted >= lapsToWin)
                    {
                        Win(player);
                    }
                }
            }
        }
    }

    void Win(GameObject player)
    {
        Debug.Log($"CheckpointManager: {player.name} WIN!");
        OnPlayerWin?.Invoke(player);
    }

    // Optional helper to reset a player's progress
    public void ResetProgress(GameObject player)
    {
        progressByPlayer.Remove(player);
    }

    // Optional: get progress info (lap, percent complete)
    public (int lapsCompleted, float checkpointProgress) GetProgress(GameObject player)
    {
        if (!progressByPlayer.TryGetValue(player, out var prog)) return (0, 0f);
        float progress = requireOrdered ? (float)prog.nextIndex / Mathf.Max(1, totalCheckpoints) : (float)prog.visited.Count / Mathf.Max(1, totalCheckpoints);
        return (prog.lapsCompleted, progress);
    }
}










/*using System.Collections.Generic;
using UnityEngine;

public class CarIdentity : MonoBehaviour
{
    public int ChenckPointIndex = 0;

    public bool
}*/
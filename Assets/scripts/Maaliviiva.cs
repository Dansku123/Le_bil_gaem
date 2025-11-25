using TMPro;
using UnityEngine;

namespace Peli
{
    public class Maaliviiva : MonoBehaviour
    {
        private bool winnerDeclared = false;

        [SerializeField] private TextMeshProUGUI winnerText;

        private void OnTriggerEnter(Collider auto)
        {
            var id = auto.GetComponent<CarIdentity>();
            var tarkistaja = auto.GetComponent<CheckpointTarkistus>();
            if (tarkistaja == null) return;

            if (!tarkistaja.CanWin()) return;

            int laps = GetLapsFromCheckpoint(tarkistaja);
            if (laps != GameManager.Instance.lapsToWin || winnerDeclared) return;

            // Determine whether this collider belongs to the player.
            // This uses GameObject tags — ensure your player GameObject has the tag "Player".
            bool isPlayer = auto.CompareTag("Player") || (id != null && id.CompareTag("Player"));

            if (isPlayer)
            {
                winnerDeclared = true;
                if (winnerText != null)
                {
                    winnerText.text = "Race Finished!\nWinner: " + (id != null ? id.car_name : "Unknown");
                    winnerText.color = Color.green;
                    winnerText.gameObject.SetActive(true);
                }
            }
            else
            {
                tarkistaja.ResetLap();
            }
        }

        // Try to read common lap-count fields/properties from the checkpoint checker via reflection.
        // This makes the method resilient to different naming conventions in CheckpointTarkistus.
        private int GetLapsFromCheckpoint(CheckpointTarkistus tarkistaja)
        {
            if (tarkistaja == null) return 0;

            var t = tarkistaja.GetType();
            string[] candidates = { "laps", "Laps", "currentLap", "CurrentLap", "lapsCompleted", "LapsCompleted", "lapCount", "LapCount", "Lap", "lap" };

            foreach (var name in candidates)
            {
                var field = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(tarkistaja);
                    if (val is int) return (int)val;
                    if (val is short) return (int)(short)val;
                }

                var prop = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var val = prop.GetValue(tarkistaja);
                    if (val is int) return (int)val;
                    if (val is short) return (int)(short)val;
                }
            }

            Debug.LogWarning($"Maaliviiva: couldn't read lap count from {tarkistaja.GetType().Name}, defaulting to 0");
            return 0;
        }
    }
}
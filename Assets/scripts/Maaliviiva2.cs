using TMPro;
using UnityEngine;

public class Maaliviiva : MonoBehaviour
{
    private bool winnerDeclared = false;

    [SerializeField] private TextMeshProUGUI winnerText;

    private void OnTriggerStay(Collider auto)
    {
        var id = auto.GetComponent<CarIdentity>();
        var tarkistaja = auto.GetComponent<CheckpointTarkistus>();
        if (id == null || tarkistaja == null || GameManager.Instance == null) return;
        if (tarkistaja.CanWin())
        {
            if (tarkistaja.laps == GameManager.Instance.lapsToWin && !winnerDeclared)
            {
                winnerDeclared = true;
                winnerText.text = "Race Finished!\nWinner: " + id.car_name;
                
                // Replace 'Player' with the correct enum value, e.g., 'Player'
                if (id.type == CarType.Racer)
                {
                    winnerText.color = Color.blue; 
                }
                else
                {
                    winnerText.color = Color.red;
                }
                
                winnerText.gameObject.SetActive(true);
                GameManager.Instance.gameRunning = false;
            }
            else
            {
                tarkistaja.ResetLap();
            }
        }
    }
}
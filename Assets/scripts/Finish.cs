using UnityEngine;

public class Maaliviiva : MonoBehaviour
{
    private void OnTriggerEnter(Collider auto)
    {
        var id = auto.GetComponent<CarIdentity>();
        var tarkastaja = auto.GetComponent<CheckpointTarkistus>();
        if (!tarkastaja.CanWin())
        {
            Debug.Log("Winner is " + id.car_name);
        }
    }
}
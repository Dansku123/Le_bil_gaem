using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int orderIndex = 0;
    private void OnTriggerEnter(Collider auto)
    {
        var id = auto.GetComponent<CarIdentity>();
        Debug.Log(id.car_name + " hit " + orderIndex + " the checkpoint.");
        var tarkastaja = auto.GetComponent<CheckpointTarkistus>();
        tarkastaja.MarkVisited(orderIndex);
    }
}
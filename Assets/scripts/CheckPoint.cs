using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int orderIndex = 0;
    private void OnTriggerEnter(Collider auto)
    {
        var id = auto.GetComponent<CarIdentity>();
        if (id != null)
        {
            Debug.Log(id.car_name + " hit " + orderIndex + " the checkpoint.");
        }
        else
        {
            Debug.LogWarning("CarIdentity component not found on collider.");
        }

        var tarkastaja = auto.GetComponent<CheckpointTarkistus>();
        if (tarkastaja != null)
        {
            tarkastaja.MarkVisited(orderIndex);
        }
        else
        {
            Debug.LogWarning("CheckpointTarkistus component not found on collider.");
        }
    }
}
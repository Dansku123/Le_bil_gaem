using UnityEngine;
public class Finish : MonoBehaviour
{
    private void OnTriggerEnter(Collider Car)
    {
        var id = Car.GetComponent<CarIdentity>();
        Debug.Log(id.car_name + " Won the race!");
    }

}
using UnityEngine;

public class Finish : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // match CheckPoint behavior: ignore non-car colliders (assumes tag "Car")
        if (!other.CompareTag("Car")) return;

        var id = other.GetComponentInParent<CarIdentity>();
        if (id == null) return;

        Debug.Log($"{id.car_name} Won the race!");
        // if CheckPoint calls a method on CarIdentity, call it here (e.g. id.OnFinish();)
    }
}
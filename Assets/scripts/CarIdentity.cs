using UnityEngine;

public enum CarType
{
    Racer,
    Clanker
}

public class CarIdentity : MonoBehaviour
{
    public CarType type = CarType.Racer;

    // Leave empty in the inspector to use the GameObject's name automatically
    public string car_name = "";

    // Editor-time convenience so the inspector shows the GameObject name by default
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(car_name))
            car_name = gameObject.name;
    }

    // Runtime fallback: ensure there's a sensible name
    void Awake()
    {
        if (string.IsNullOrWhiteSpace(car_name))
            car_name = gameObject.name;
    }
}

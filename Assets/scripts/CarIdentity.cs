using System.Collections.Generic;
using UnityEngine;
public enum CarType
    {
        Racer,
        Clanker
    }

public class CarIdentity : MonoBehaviour
{
    public CarType type = CarType.Racer;

    public string car_name = "Cool Porsche";
    /*    private static Dictionary<CarType, System.Type> carTypeToScriptType = new Dictionary<CarType, System.Type>
        {
            { CarType.Racer, typeof(Racer) },
            { CarType.Clanker, typeof(ClankerCar) }
        };

        void Awake()
        {
            // Ensure only the correct script is attached based on carType
            foreach (var entry in carTypeToScriptType)
            {
                var script = GetComponent(entry.Value);
                if (entry.Key == carType)
                {
                    if (script == null)
                    {
                        gameObject.AddComponent(entry.Value);
                        Debug.Log($"{entry.Key} script added.");
                    }
                }
                else
                {
                    if (script != null)
                    {
                        Destroy(script);
                        Debug.Log($"{entry.Key} script removed.");
                    }
                }
            }
        }*/    
}

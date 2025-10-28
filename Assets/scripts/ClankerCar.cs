using UnityEngine;

public class ClankerCar : MonoBehaviour
{
    // Optional link to a CarIdentity. If assigned, values from it are used.
    // CarIdentity may expose: public Transform[] waypoints; public float speed; public float rotationSpeed;
    // We access these by reflection so there's no compile-time dependency on a specific CarIdentity shape.
    public CarIdentity carIdentity;

    // kept for backwards compatibility if no CarIdentity is assigned
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    public float speed = 10f;
    public float rotationSpeed = 5f;

    void Start()
    {
    }

    void Update()
    {
        // choose waypoint source (CarIdentity takes precedence) using reflection so missing compile-time members won't fail
        Transform[] waypointSource = null;
        if (carIdentity != null)
        {
            var type = carIdentity.GetType();
            var wpField = type.GetField("waypoints");
            if (wpField != null && typeof(Transform[]).IsAssignableFrom(wpField.FieldType))
            {
                waypointSource = (Transform[])wpField.GetValue(carIdentity);
            }
            else
            {
                var wpProp = type.GetProperty("waypoints");
                if (wpProp != null && typeof(Transform[]).IsAssignableFrom(wpProp.PropertyType))
                    waypointSource = (Transform[])wpProp.GetValue(carIdentity, null);
                else
                    waypointSource = waypoints;
            }
        }
        else
        {
            waypointSource = waypoints;
        }

        if (waypointSource == null || waypointSource.Length == 0)
            return;

        // ensure current index is valid if the waypoint array changed
        if (currentWaypointIndex >= waypointSource.Length)
            currentWaypointIndex = 0;

        float activeSpeed;
        float activeRotationSpeed;

        if (carIdentity != null)
        {
            // try to get a field or property named "speed" via reflection (avoids compile-time dependency)
            var type = carIdentity.GetType();
            var speedField = type.GetField("speed");
            if (speedField != null && speedField.FieldType == typeof(float))
                activeSpeed = (float)speedField.GetValue(carIdentity);
            else
            {
                var speedProp = type.GetProperty("speed");
                if (speedProp != null && speedProp.PropertyType == typeof(float))
                    activeSpeed = (float)speedProp.GetValue(carIdentity, null);
                else
                    activeSpeed = speed;
            }

            // try to get a field or property named "rotationSpeed" via reflection (avoids compile-time dependency)
            var rotField = type.GetField("rotationSpeed");
            if (rotField != null && rotField.FieldType == typeof(float))
                activeRotationSpeed = (float)rotField.GetValue(carIdentity);
            else
            {
                var rotProp = type.GetProperty("rotationSpeed");
                if (rotProp != null && rotProp.PropertyType == typeof(float))
                    activeRotationSpeed = (float)rotProp.GetValue(carIdentity, null);
                else
                    activeRotationSpeed = rotationSpeed;
            }
        }
        else
        {
            activeSpeed = speed;
            activeRotationSpeed = rotationSpeed;
        }

        Transform target = waypointSource[currentWaypointIndex];
        Vector3 targetXZ = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = (targetXZ - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, activeRotationSpeed * Time.deltaTime);
        transform.Translate(Vector3.forward * activeSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetXZ) < 1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypointSource.Length;
        }
    }
}

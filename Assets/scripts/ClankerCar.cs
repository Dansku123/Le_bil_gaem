using Unity.Collections;
using UnityEngine;
// using System.Numerics;
//using Vector3 = UnityEngine.Vector3;

public class AICar : MonoBehaviour
{

    public Transform[] waypoints; // Taulukko reittipisteistä

    private int currentWaypointIndex = 0; // Indeksi reittipisteeseen, jota kohti liikutaan tällä hetkellä

    public float speed = 10f; // AI-Auton nopeus

    public float rotationSpeed = 5f; // AI-Auton kääntymisnopeus

    // Update is called once per frame
    void Update()
{
    // Haetaan waypoints-taulukosta tämänhetkinen kohdepiste
    Transform target = waypoints[currentWaypointIndex];

    // Kohteen xz, pidä nykyinen y (ei nouse tai laske)
    Vector3 targetXZ = new Vector3(target.position.x, transform.position.y, target.position.z);

    // Suuntavektori kohteeseen (ei normalisoida heti, tarvitaan magnituden tarkistus)
    Vector3 direction = targetXZ - transform.position;
    float sqrMag = direction.sqrMagnitude;

    // Jos suunta on liian pieni, oletettavasti ollaan perillä tai hyvin lähellä — vaihda waypointiin
    if (sqrMag < 1e-6f)
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        return;
    }

    // Normalisoidaan turvallisesti
    Vector3 dirNorm = direction.normalized;

    // Rotaatio kohti suuntaa — käytä RotateTowards tai Slerp turvallisesti
    Quaternion lookRotation = Quaternion.LookRotation(dirNorm);
    transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime * 100f);

    // Liikuta kohti kohdetta maailmantilassa (vakio liikerata, ei local-forward-arcea)
    transform.position = Vector3.MoveTowards(transform.position, targetXZ, speed * Time.deltaTime);

    // Jos lähellä, vaihda seuraava piste
    if (Vector3.Distance(transform.position, targetXZ) < 0.5f)
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }
}
}
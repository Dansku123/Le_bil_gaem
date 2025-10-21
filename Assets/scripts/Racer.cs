using UnityEngine;

public class Racer : MonoBehaviour
{
    public float speed = 10f;
    public float turnSpeed = 100f;

    public float boostMultiplier = 2f;
    public float boostDuration = 3f;
    public float boostCooldown = 5f;

    private float boostTimer = 0f;
    private float cooldownTimer = 0f;

    void Start()
    {
        Debug.Log("Racer script has started.");
    }

    void Update()
    {
        // Handle boost and cooldown timers
        if (boostTimer > 0)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                cooldownTimer = boostCooldown; // Start cooldown after boost ends
                Debug.Log("Boost ended. Cooldown started.");
            }
        }
        else if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Activate boost if Shift is pressed and not in cooldown
        if (Input.GetKeyDown(KeyCode.LeftShift) && boostTimer <= 0 && cooldownTimer <= 0)
        {
            boostTimer = boostDuration;
            Debug.Log("Boost activated!");
        }

        // Determine current speed
        float currentSpeed = speed;
        if (boostTimer > 0)
        {
            currentSpeed *= boostMultiplier;
        }

        // Handle movement
        float move = Input.GetAxis("Vertical") * currentSpeed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;

        transform.Translate(Vector3.forward * move);
        transform.Rotate(Vector3.up * turn);
    }
}

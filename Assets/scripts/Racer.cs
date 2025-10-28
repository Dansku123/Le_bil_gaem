using System;
using UnityEngine;

using System;
using UnityEngine;

public class Racer : MonoBehaviour
{
    // Public fields kept for backward compatibility and inspector tuning
    public float speed = 10f;
    public float turnSpeed = 100f;

    // Acceleration / deceleration
    public float acceleration = 10f;    // units per second² while speeding up
    public float deceleration = 15f;    // units per second² while slowing down

    // Minimum turning responsiveness at zero speed (0..1)
    // 0.25 means turning at 25% of turnSpeed when stopped
    public float minTurnSpeedFactor = 0.25f;

    // Keep original names for compatibility:
    // boostDuration now represents the total boost "charge" (3s by default)
    // boostCooldown is the cooldown length (20s by default)
    public float boostDuration = 3f;
    public float boostMultiplier = 2f;
    public float boostCooldown = 20f;

    // Internal state
    private float boostRemaining;
    private float cooldownTimer = 0f;
    private bool isBoosting = false;

    // Current forward speed (actual instantaneous forward velocity, can be negative for reverse)
    private float currentForwardSpeed = 0f;

    // For console cooldown updates (to avoid spamming same value each frame)
    // track hundredths of percent (pct * 100) to detect changes at two-decimal precision
    private int lastLoggedCooldownHundredths = -1;
    // For console boost updates (to avoid spamming same value each frame)
    private int lastLoggedBoostHundredths = -1;

    // External control support
    public bool useExternalInput = false; // if true, external scripts must call SetInput()
    private float externalVertical = 0f;
    private float externalHorizontal = 0f;

    // Events other scripts can subscribe to
    public event Action OnBoostStarted;
    public event Action OnBoostEnded;

    void Start()
    {
        boostRemaining = boostDuration; // start with full charge
        Debug.Log("Racer script has started.");
    }

    void Update()
    {
        // Handle cooldown timer (counts down when active)
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            float pct = boostCooldown > 0f ? (cooldownTimer / boostCooldown * 100f) : 0f;
            int hundredths = Mathf.RoundToInt(pct * 100f);
            if (hundredths != lastLoggedCooldownHundredths)
            {
                lastLoggedCooldownHundredths = hundredths;
                Debug.Log($"Cooldown remaining: {Mathf.Max(0f, pct):F2}%");
            }

            if (cooldownTimer <= 0f)
            {
                // Cooldown finished -> restore full boost charge
                cooldownTimer = 0f;
                boostRemaining = boostDuration;
                lastLoggedCooldownHundredths = -1;
                lastLoggedBoostHundredths = -1;
                Debug.Log("Cooldown ended. Boost recharged.");
            }
        }

        // Keyboard control to start/stop boosting (only if not using external input)
        if (!useExternalInput)
        {
            // start boosting when shift pressed, if there is charge and not in cooldown
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                TryActivateBoost();
            }

            // Stop boosting when shift released; remaining charge is preserved
            if (isBoosting && Input.GetKeyUp(KeyCode.LeftShift))
            {
                isBoosting = false;
                float pct = boostDuration > 0f ? (boostRemaining / boostDuration * 100f) : 0f;
                Debug.Log($"Boost paused. Remaining: {Mathf.Max(0f, pct):F2}%");
                // Note: OnBoostEnded is only invoked when boost fully depletes and cooldown starts.
                lastLoggedBoostHundredths = -1;
            }
        }

        // If boosting, consume charge
        if (isBoosting)
        {
            if (boostRemaining > 0f)
            {
                boostRemaining -= Time.deltaTime;

                float pct = boostDuration > 0f ? (boostRemaining / boostDuration * 100f) : 0f;
                int hundredths = Mathf.RoundToInt(pct * 100f);
                if (hundredths != lastLoggedBoostHundredths)
                {
                    lastLoggedBoostHundredths = hundredths;
                    Debug.Log($"Boost remaining: {Mathf.Max(0f, pct):F2}%");
                }

                if (boostRemaining <= 0f)
                {
                    // Boost fully used -> start cooldown
                    boostRemaining = 0f;
                    isBoosting = false;
                    cooldownTimer = boostCooldown;
                    lastLoggedCooldownHundredths = -1; // allow immediate cooldown percent log
                    lastLoggedBoostHundredths = -1;
                    Debug.Log($"Boost ended. Cooldown started: {boostCooldown:F0}s");
                    OnBoostEnded?.Invoke();
                }
            }
        }

        // Get movement input (external or from Input)
        float moveInput = useExternalInput ? externalVertical : Input.GetAxis("Vertical");
        float turnInput = useExternalInput ? externalHorizontal : Input.GetAxis("Horizontal");

        // Determine desired max forward speed (base speed, possibly boosted)
        float maxForwardSpeed = speed * ((isBoosting && boostRemaining > 0f) ? boostMultiplier : 1f);

        // Desired target forward speed based on input
        float targetForward = moveInput * maxForwardSpeed;

        // Choose acceleration or deceleration depending on whether we're increasing magnitude
        float accel = (Mathf.Abs(targetForward) > Mathf.Abs(currentForwardSpeed)) ? acceleration : deceleration;

        // Move currentForwardSpeed toward target using chosen rate
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetForward, accel * Time.deltaTime);

        // Turning scale based on speed: slower turning at slower speeds, lerp between minTurnSpeedFactor and 1
        float absCurrent = Mathf.Abs(currentForwardSpeed);
        float denom = Mathf.Max(0.0001f, maxForwardSpeed); // avoid div by zero
        float speedLerp = Mathf.InverseLerp(0f, denom, absCurrent); // 0 at stop, 1 at maxForwardSpeed
        float turnFactor = Mathf.Lerp(minTurnSpeedFactor, 1f, speedLerp);

        // Apply movement and turning
        float move = currentForwardSpeed * Time.deltaTime;
        float turn = turnInput * turnSpeed * turnFactor * Time.deltaTime;

        transform.Translate(Vector3.forward * move);
        transform.Rotate(Vector3.up * turn);
    }

    // Public API for other scripts:

    // Try to activate boost; returns true if boost started
    public bool TryActivateBoost()
    {
        // Can't start if in cooldown or no charge
        if (cooldownTimer > 0f || boostRemaining <= 0f)
            return false;

        if (!isBoosting)
        {
            isBoosting = true;
            lastLoggedBoostHundredths = -1; // allow immediate percent log
            Debug.Log("Boost started.");
            OnBoostStarted?.Invoke();
        }
        return true;
    }

    // Force activate boost (ignores cooldown) — optional helper
    public void ForceActivateBoost()
    {
        cooldownTimer = 0f;
        // If there was no charge, restore full charge then start
        if (boostRemaining <= 0f)
            boostRemaining = boostDuration;

        isBoosting = true;
        lastLoggedBoostHundredths = -1;
        Debug.Log("Boost force-activated!");
        OnBoostStarted?.Invoke();
    }

    // Set movement input from another script when useExternalInput = true
    public void SetInput(float vertical, float horizontal)
    {
        externalVertical = vertical;
        externalHorizontal = horizontal;
    }

    // Query helpers
    public bool IsBoostActive => isBoosting;
    public bool IsCooldownActive => cooldownTimer > 0f;
    public float BoostRemaining => Mathf.Max(0f, boostRemaining);
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    // Get current effective forward speed (useful to other scripts)
    // Returns actual instantaneous forward speed (positive forward, negative reverse)
    public float GetCurrentSpeed()
    {
        return currentForwardSpeed;
    }
}

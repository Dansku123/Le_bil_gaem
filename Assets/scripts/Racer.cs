using System;
using UnityEngine;

public class Racer : MonoBehaviour
{
    // Public fields kept for backward compatibility and inspector tuning
    public float speed = 20f; // changed default max speed to 20
    public float turnSpeed = 100f;

    // Gears
    public int gearCount = 6; // added sixth gear (overdrive)
    [Tooltip("Normalized top-speed ratio per gear (0..1). If empty or length mismatch, initialized evenly.\nNote: gear 6 is a special overdrive gear that gives 500% top speed regardless of this array.")]
    public float[] gearRatios;
    [Range(1, 10)]
    public int currentGear = 1;
    public KeyCode shiftUpKey = KeyCode.E;
    public KeyCode shiftDownKey = KeyCode.Q;
    public event Action<int> OnGearChanged;

    // Acceleration / deceleration
    public float acceleration = 10f;    // units per second² while speeding up
    public float deceleration = 15f;    // units per second² while slowing down (coasting)
    public float brakeDeceleration = 30f; // units per second² when actively braking (opposite input)

    // Minimum turning responsiveness at zero speed (0..1)
    // 0.25 means turning at 25% of turnSpeed when stopped
    public float minTurnSpeedFactor = 0.25f;

    // Keep original names for compatibility:
    // boostDuration now represents the total boost "charge" (3s by default)
    // boostCooldown is the cooldown length (20s by default)
    public float boostDuration = 3f;
    public float boostMultiplier = 2f;
    public float boostCooldown = 20f;

    // Boost affecting acceleration as well as top speed
    public float boostAccelerationMultiplier = 1.5f;

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

    // Special overdrive gear settings
    private const int OverdriveGearNumber = 6;
    private const float OverdriveMultiplier = 5f; // 500%

    void Start()
    {
        boostRemaining = boostDuration; // start with full charge
        EnsureGearRatios();
        ClampCurrentGear();
        Debug.Log("Racer script has started. Max speed set to " + speed + ". Current gear: " + currentGear);
    }

    void Update()
    {
        // Gear input (only if not using external input for controls)
        if (!useExternalInput)
        {
            if (Input.GetKeyDown(shiftUpKey))
                ShiftUp();

            if (Input.GetKeyDown(shiftDownKey))
                ShiftDown();
        }

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

        // Determine desired max forward speed (base speed, modified by gear and possibly boosted)
        float gearRatio = GetCurrentGearRatio();
        float maxForwardSpeed = speed * gearRatio * ((isBoosting && boostRemaining > 0f) ? boostMultiplier : 1f);

        // Desired target forward speed based on input
        float targetForward = moveInput * maxForwardSpeed;

        // Choose acceleration or deceleration depending on whether we're increasing magnitude
        float accelRate;

        // If we're trying to increase magnitude (accelerating)
        if (Mathf.Abs(targetForward) > Mathf.Abs(currentForwardSpeed))
        {
            accelRate = acceleration * ((isBoosting && boostRemaining > 0f) ? boostAccelerationMultiplier : 1f);
        }
        else
        {
            // If input is actively opposite to current movement, use brake deceleration
            if (!Mathf.Approximately(targetForward, 0f) && !Mathf.Approximately(currentForwardSpeed, 0f)
                && Mathf.Sign(targetForward) != Mathf.Sign(currentForwardSpeed))
            {
                accelRate = brakeDeceleration;
            }
            else
            {
                // Coasting / letting off the throttle: use normal deceleration (takes time)
                accelRate = deceleration;
            }
        }

        // Move currentForwardSpeed toward target using chosen rate
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetForward, accelRate * Time.deltaTime);

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

    // Gear control API
    public void ShiftUp()
    {
        SetGear(currentGear + 1);
    }

    public void ShiftDown()
    {
        SetGear(currentGear - 1);
    }

    public void SetGear(int gear)
    {
        int prevGear = currentGear;
        currentGear = Mathf.Clamp(gear, 1, Mathf.Max(1, gearCount));
        if (currentGear != prevGear)
        {
            // clamp the current forward speed to the new gear's max (preserve sign)
            float newMax = speed * GetCurrentGearRatio();
            currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, -newMax, newMax);

            Debug.Log($"Gear changed: {currentGear}/{gearCount} (ratio {GetCurrentGearRatio():F2})");
            OnGearChanged?.Invoke(currentGear);
        }
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

    private void EnsureGearRatios()
    {
        if (gearRatios == null || gearRatios.Length != gearCount)
        {
            gearRatios = new float[gearCount];
            // init evenly distributed ratios from small to 1.0 for the normal gears
            int normalCount = Mathf.Max(1, gearCount);
            for (int i = 0; i < gearCount; i++)
            {
                // keep the generated ratios within 0..1; the special overdrive gear is handled separately
                gearRatios[i] = Mathf.Lerp(0.2f, 1f, (float)i / Mathf.Max(1, normalCount - 1));
            }
        }
        // ensure ratios are clamped 0..1 (overdrive gear does not rely on this array value)
        for (int i = 0; i < gearRatios.Length; i++)
            gearRatios[i] = Mathf.Clamp01(gearRatios[i]);
    }

    private void ClampCurrentGear()
    {
        currentGear = Mathf.Clamp(currentGear, 1, Mathf.Max(1, gearCount));
    }

    private float GetCurrentGearRatio()
    {
        // Special-case sixth gear: overdrive that multiplies top speed by 5 (500%)
        if (currentGear == OverdriveGearNumber)
            return OverdriveMultiplier;

        if (gearRatios == null || gearRatios.Length == 0)
            return 1f;
        int idx = Mathf.Clamp(currentGear - 1, 0, gearRatios.Length - 1);
        return gearRatios[idx];
    }
}

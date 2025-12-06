using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SprintingSC : MonoBehaviour
{
    Rig data;
    MovementSC movement;
    BasePlayerStats playerStats;
    CharacterInput input;

    [Header("Sprint")]
    public bool isSprinting = false;
    public float sprintSpeed = 50f;
    private float originalForce;

    [Header("Stamina (points/sec) - tune these")]
    // How many stamina points are removed per second while sprinting
    public float staminaPerSecondDrain = 50f;
    // How many stamina points are restored per second while regenerating
    public float staminaPerSecondRegen = 50f;

    [Header("Delays (seconds)")]
    // Delay before regen starts when stamina hits zero
    public float regenDelayAfterDeplete = 2f;
    // Delay before regen starts when you stop sprinting but didn't deplete
    public float regenDelayOnStop = 0.5f;

    public InputActionReference sprintAction;

    private Coroutine staminaDrainCoroutine;
    private Coroutine staminaRegenCoroutine;

    // accumulators for fractional changes so we only apply integer changes to UseStamina / stamina
    private float drainAccumulator = 0f;
    private float regenAccumulator = 0f;

    // scheduler for delayed regen
    private float regenStartTime = -1f;

    private void Start()
    {
        movement = GetComponent<MovementSC>();
        if (movement == null) Debug.LogError("MovementSC component not found on the GameObject.");

        data = GetComponent<Rig>();
        if (data == null) Debug.LogError("Rig component not found on the GameObject.");

        input = GetComponent<CharacterInput>();
        if (input == null) Debug.LogError("CharacterInput component not found on the GameObject.");

        playerStats = GetComponentInParent<BasePlayerStats>();
        if (playerStats == null) Debug.LogError("BasePlayerStats component not found in parent hierarchy.");

        if (movement != null)
            originalForce = movement.Force;
    }

    private void Update()
    {
        // If a regen was scheduled (either from deplete or stop) and the time has come, start regen
        if (regenStartTime > 0f && Time.time >= regenStartTime && staminaRegenCoroutine == null && !isSprinting)
        {
            regenStartTime = -1f;
            staminaRegenCoroutine = StartCoroutine(RegenerateStamina());
        }
    }

    private IEnumerator DrainStamina()
    {
        drainAccumulator = 0f;

        while (isSprinting)
        {
            // if player is moving, drain stamina; otherwise don't drain but remain sprinting state available
            if (input != null && input.isMoving)
            {
                data.Currentstate = StateType.Run;

                // accumulate fractional stamina drain
                drainAccumulator += staminaPerSecondDrain * Time.deltaTime;

                int drainInt = Mathf.FloorToInt(drainAccumulator);
                if (drainInt > 0)
                {
                    // Use existing API for draining (keeps any logic inside BasePlayerStats)
                    if (playerStats != null)
                    {
                        // Prefer UseStamina if you have it
                        playerStats.UseStamina(drainInt);
                    }

                    drainAccumulator -= drainInt;

                    // if stamina reached zero (or below) force stop sprint and schedule regen with deplete delay
                    if (playerStats != null && playerStats.stamina <= 0)
                    {
                        // clamp to zero to be safe
                        playerStats.stamina = 0;

                        // turn off sprint and reset movement
                        isSprinting = false;
                        if (movement != null) movement.Force = originalForce;
                        data.Currentstate = StateType.Walk;

                        staminaDrainCoroutine = null;

                        // stop any existing regen coroutine so we can schedule a fresh one
                        if (staminaRegenCoroutine != null)
                        {
                            StopCoroutine(staminaRegenCoroutine);
                            staminaRegenCoroutine = null;
                        }

                        // schedule regen after deplete delay (handled in Update)
                        regenStartTime = Time.time + regenDelayAfterDeplete;

                        yield break;
                    }
                }
            }
            else
            {
                // not moving — treat as walk state while keeping sprint toggle
                data.Currentstate = StateType.Walk;
            }

            yield return null;
        }

        // if loop ended because isSprinting became false (player canceled), schedule regen after stop delay
        staminaDrainCoroutine = null;

        // stop any running regen coroutine - we'll schedule a fresh one
        if (staminaRegenCoroutine != null)
        {
            StopCoroutine(staminaRegenCoroutine);
            staminaRegenCoroutine = null;
        }

        regenStartTime = Time.time + regenDelayOnStop;
    }

    private IEnumerator RegenerateStamina()
    {
        regenAccumulator = 0f;

        // Regen loop
        while (playerStats != null && playerStats.stamina < 100 && !isSprinting)
        {
            regenAccumulator += staminaPerSecondRegen * Time.deltaTime;
            int healInt = Mathf.FloorToInt(regenAccumulator);
            if (healInt > 0)
            {
                playerStats.stamina = Mathf.Min(100, playerStats.stamina + healInt);
                regenAccumulator -= healInt;
            }

            // stop regen if player starts sprinting mid-regen
            if (isSprinting) yield break;

            yield return null;
        }

        staminaRegenCoroutine = null;
    }

    private void OnEnable()
    {
        if (sprintAction != null)
        {
            sprintAction.action.started += OnSprintStarted;
            sprintAction.action.canceled += OnSprintCanceled;
        }
    }
    private void OnDisable()
    {
        if (sprintAction != null)
        {
            sprintAction.action.started -= OnSprintStarted;
            sprintAction.action.canceled -= OnSprintCanceled;
        }
    }

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        // If stamina is 0, prevent sprinting
        if (playerStats != null && playerStats.stamina <= 0)
        {
            // optionally play a sound or UI feedback here
            return;
        }

        isSprinting = true;
        if (movement != null) movement.Force = sprintSpeed;

        // cancel any pending regen schedule & running regen
        regenStartTime = -1f;
        if (staminaRegenCoroutine != null)
        {
            StopCoroutine(staminaRegenCoroutine);
            staminaRegenCoroutine = null;
        }

        if (staminaDrainCoroutine == null)
            staminaDrainCoroutine = StartCoroutine(DrainStamina());
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
        if (movement != null) movement.Force = originalForce;

        data.Currentstate = StateType.Walk;

        if (staminaDrainCoroutine != null)
        {
            StopCoroutine(staminaDrainCoroutine);
            staminaDrainCoroutine = null;
        }

        // reset accumulators so next sprint/regens start clean
        drainAccumulator = 0f;

        // stop any running regen coroutine - we'll schedule a fresh one
        if (staminaRegenCoroutine != null)
        {
            StopCoroutine(staminaRegenCoroutine);
            staminaRegenCoroutine = null;
        }

        regenStartTime = Time.time + regenDelayOnStop;
    }
}

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerAbilityManager : MonoBehaviour
{
    public GameObject AbilityUITemplate;
    public Transform abilitiesParent; // assign in inspector: Canvas/SurvivorStats/Abilites
    [Tooltip("Optional: assign in inspector. If left empty the script will try to find the RoundManager at runtime.")]
    public RoundManager roundManager;

    private BasePlayer player;

    private SurvivorType survivor;
    private KillerType killer;

    private Ability[] currentMoveset;
    private List<InputAction> abilityInputs = new List<InputAction>();
    private List<float> cooldownTimers = new List<float>();
    private List<TMP_Text> cooldownTexts = new List<TMP_Text>();
    private List<AbiltySlider> abilitySliders = new List<AbiltySlider>();

    private bool subscribed = false;

    // NEW: intermission gating (no disabling InputActions!)
    private bool abilitiesEnabled = true;

    private void Awake()
    {
        var camRefs = Camera.main?.GetComponent<PlayerCameraReferences>();
        PhotonView probablyPlayer = camRefs?.GetPlayer().GetPhotonView();
        if (probablyPlayer != null && probablyPlayer.IsMine)
            player = camRefs?.GetPlayer()?.GetComponent<BasePlayer>();
    }

    private void OnEnable()
    {
        var go = GameObject.Find("Multiplayer");
        roundManager = go != null ? go.GetComponent<RoundManager>() : FindFirstObjectByType<RoundManager>();

        Debug.Log($"PlayerAbilityManager.OnEnable - roundManager = {(roundManager != null ? roundManager.gameObject.name : "NULL")}");

        if (roundManager != null && !subscribed)
        {
            roundManager.OnRoundStart += OnRoundStart;
            roundManager.OnIntermissionStart += OnIntermissionStart;
            subscribed = true;
            Debug.Log("PlayerAbilityManager subscribed to RoundManager events");
        }
        else if (roundManager == null)
        {
            Debug.LogError("PlayerAbilityManager: RoundManager not found in OnEnable!");
        }

        // If the round is already active, set up abilities immediately
        if (roundManager != null && roundManager.roundActive)
        {
            abilitiesEnabled = true;
            Debug.Log("PlayerAbilityManager detected roundActive on OnEnable -> calling OnRoundStart()");
            OnRoundStart();
        }
        else
        {
            // Not active yet -> assume intermission/lobby state
            abilitiesEnabled = false;
            SetAllCooldownTexts("Disabled");
        }
    }

    private void OnIntermissionStart()
    {
        Debug.Log("Intermission Started - disabling ability usage");
        abilitiesEnabled = false;
        SetAllCooldownTexts("Disabled");
    }

    private void OnDisable()
    {
        if (roundManager != null && subscribed)
        {
            roundManager.OnRoundStart -= OnRoundStart;
            roundManager.OnIntermissionStart -= OnIntermissionStart; // IMPORTANT: was missing
            subscribed = false;
            Debug.Log("PlayerAbilityManager unsubscribed from RoundManager events");
        }
    }

    private void Start()
    {
        if (player == null)
        {
            var camRefs = Camera.main?.GetComponent<PlayerCameraReferences>();
            PhotonView probablyPlayer = camRefs?.GetPlayer().GetPhotonView();
            if (probablyPlayer != null && probablyPlayer.IsMine)
                player = camRefs?.GetPlayer()?.GetComponent<BasePlayer>();
        }

        Debug.Log($"PlayerAbilityManager.Start called. enabled={enabled}, activeInHierarchy={gameObject.activeInHierarchy}, player={(player == null ? "NULL" : "FOUND")}");
    }

    private void OnRoundStart()
    {
        Debug.Log("Round Started - Setting up abilities");
        abilitiesEnabled = true;

        if (player == null)
        {
            Debug.LogError("BasePlayer component not found on the player!");
            return;
        }

        // If abilities were already built, make sure actions are enabled now.
        EnsureAbilityActionsEnabled();

        StartCoroutine(BuildAbilitiesWithDelay());
    }


    private IEnumerator BuildAbilitiesWithDelay()
    {
        BuildAbilitiesUI();
        yield return new WaitForSeconds(1.5f);
        BuildAbilitiesUI();
    }

    private void BuildAbilitiesUI()
    {
        survivor = null;
        killer = null;
        currentMoveset = null;

        var type = player.GetPlayerType();
        Debug.Log($"[BuildAbilitiesUI] Player type: {type}");

        if (type == BasePlayer.PlayerType.Survivor)
        {
            survivor = player.GetEquippedSurvivor();
            Debug.Log($"Equipped Survivor: {(survivor != null ? survivor.name : "NULL")}");
            currentMoveset = survivor?.abilities;
        }
        else if (type == BasePlayer.PlayerType.Killer)
        {
            killer = player.GetEquippedKiller();
            Debug.Log($"Equipped Killer: {(killer != null ? killer.name : "NULL")}");
            currentMoveset = killer?.abilities;
        }
        else
        {
            Debug.LogWarning($"Unknown player type {type}");
            return;
        }

        if (currentMoveset == null || currentMoveset.Length == 0)
        {
            Debug.LogWarning($"No abilities found for {type}");
            return;
        }

        abilityInputs.Clear();
        cooldownTimers.Clear();
        cooldownTexts.Clear();
        abilitySliders.Clear();

        if (abilitiesParent == null)
        {
            Debug.LogError("Abilities parent not found under Canvas/SurvivorStats/Abilites");
            return;
        }

        foreach (Transform child in abilitiesParent)
            Destroy(child.gameObject);

        foreach (Ability ability in currentMoveset)
        {
            if (ability == null) continue;

            GameObject abilityGO = Instantiate(AbilityUITemplate, abilitiesParent);

            var normalIcon = abilityGO.transform.Find("Icon")?.GetComponent<Image>();
            var grayIcon = abilityGO.transform.Find("IconGrayscale")?.GetComponent<Image>();

            AbiltySlider slider = normalIcon.GetComponent<AbiltySlider>();
            if (slider != null)
            {
                slider.ability = ability;
                slider.SetSliderValue(100f);
                abilitySliders.Add(slider);
            }
            else
            {
                abilitySliders.Add(null);
            }

            if (normalIcon != null) normalIcon.sprite = ability.AbilityIcon;
            if (grayIcon != null)
            {
                grayIcon.sprite = ability.AbilityIconGrayscale;
                grayIcon.fillAmount = 1f;
            }


            var nameText = abilityGO.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = ability.AbilityName;

            TMP_Text cooldownText = abilityGO.transform.Find("Cooldown")?.GetComponent<TMP_Text>();
            if (cooldownText != null) cooldownText.text = abilitiesEnabled ? "Ready" : "Disabled";

            // NOTE: This MUST be && (your old code had || which can NRE)
            if (ability.AbilityActivation != null && ability.AbilityActivation.action != null)
                abilityInputs.Add(ability.AbilityActivation.action);
            else
                abilityInputs.Add(null);

            cooldownTimers.Add(ability.AbilityCooldown);
            cooldownTexts.Add(cooldownText);
        }
        Debug.Log($"Built ability UI for {currentMoveset.Length} abilities for {type}");

        EnsureAbilityActionsEnabled();
    }

    private void EnsureAbilityActionsEnabled()
    {
        // Only do this for the local player
        // (player may be null early; just bail)
        if (player == null) return;

        for (int i = 0; i < abilityInputs.Count; i++)
        {
            var act = abilityInputs[i];
            if (act == null) continue;

            // Make sure it's enabled so WasPressedThisFrame can work
            if (!act.enabled)
                act.Enable();
        }
    }


    private void SetAllCooldownTexts(string text)
    {
        for (int i = 0; i < cooldownTexts.Count; i++)
        {
            if (cooldownTexts[i] != null)
                cooldownTexts[i].text = text;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            var camRefs = Camera.main?.GetComponent<PlayerCameraReferences>();
            player = camRefs?.GetPlayer()?.GetComponent<BasePlayer>();
            if (player == null)
            {
                Debug.LogError("BasePlayer component not found on the player! looking again");
                return;
            }
        }

        if (currentMoveset == null)
            return;

        int safeCount = Mathf.Min(
            currentMoveset.Length,
            abilityInputs.Count,
            cooldownTimers.Count,
            cooldownTexts.Count,
            abilitySliders.Count);

        for (int i = 0; i < safeCount; i++)
        {
            Ability ability = currentMoveset[i];
            if (ability == null) continue;

            // Cooldown timer update (still updates even in intermission)
            if (cooldownTimers[i] < ability.AbilityCooldown)
            {
                cooldownTimers[i] += Time.deltaTime;
                if (cooldownTimers[i] > ability.AbilityCooldown)
                    cooldownTimers[i] = ability.AbilityCooldown;
            }

            float remaining = ability.AbilityCooldown - cooldownTimers[i];

            // If intermission: show Disabled and skip activation
            if (!abilitiesEnabled)
            {
                if (cooldownTexts[i] != null) cooldownTexts[i].text = "Disabled";
                if (abilitySliders[i] != null) abilitySliders[i].SetSliderValue(0f);
                continue;
            }

            // Normal UI
            if (cooldownTexts[i] != null)
                cooldownTexts[i].text = remaining > 0 ? remaining.ToString("F1") : "Ready";

            if (abilitySliders[i] != null)
            {
                float fillPercent = (cooldownTimers[i] / ability.AbilityCooldown) * 100f;
                abilitySliders[i].SetSliderValue(fillPercent);
            }

            var act = abilityInputs[i];

            // Works for Button actions (keyboard, mouse, gamepad buttons)
            bool pressed = act != null && act.triggered;

            // Fallback for Value actions that return float (common for buttons too)
            if (!pressed && act != null && act.ReadValue<float>() > 0.5f)
                pressed = true;

            if (pressed && cooldownTimers[i] >= ability.AbilityCooldown)
            {
                ability.ActivateAbility(player);
                cooldownTimers[i] = 0f;
            }
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null && subscribed)
        {
            roundManager.OnRoundStart -= OnRoundStart;
            roundManager.OnIntermissionStart -= OnIntermissionStart;
            subscribed = false;
        }
    }
}

using Photon.Pun;
using System;
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

    private void Awake()
    {
        // Try to cache player early (may be null until Camera.main exists)
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
            subscribed = true;
            Debug.Log("PlayerAbilityManager subscribed to RoundManager.OnRoundStart");
        }
        else if (roundManager == null)
        {
            Debug.LogError("PlayerAbilityManager: RoundManager not found in OnEnable!");
        }

        // If the round is already active, set up abilities immediately
        if (roundManager != null && roundManager.roundActive)
        {
            Debug.Log("PlayerAbilityManager detected roundActive on OnEnable -> calling OnRoundStart()");
            OnRoundStart();
        }
    }

    private void OnDisable()
    {
        if (roundManager != null && subscribed)
        {
            roundManager.OnRoundStart -= OnRoundStart;
            subscribed = false;
            Debug.Log("PlayerAbilityManager unsubscribed from RoundManager.OnRoundStart");
        }
    }

    private void Start()
    {
        // Ensure player is cached (again) once Camera.main is ready
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

        if (player == null)
        {
            Debug.LogError("BasePlayer component not found on the player!");
            return;
        }

        // Start a coroutine to build UI immediately and again after a short delay
        StartCoroutine(BuildAbilitiesWithDelay());
    }

    private IEnumerator BuildAbilitiesWithDelay()
    {
        // First immediate attempt
        BuildAbilitiesUI();
        // Wait a bit and try again (lets PlayerType and equipped data settle)
        yield return new WaitForSeconds(1.5f);
        BuildAbilitiesUI();
    }

    private void BuildAbilitiesUI()
    {
        // Reset
        survivor = null;
        killer = null;
        currentMoveset = null;

        var type = player.GetPlayerType();
        Debug.Log($"[BuildAbilitiesUI] Player type: {type}");

        // Only call one accessor based on type
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

        // Bail if no abilities found
        if (currentMoveset == null || currentMoveset.Length == 0)
        {
            Debug.LogWarning($"No abilities found for {type}");
            return;
        }

        // Clear and rebuild the UI
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
            Destroy(child.gameObject); // Clear old abilities

        foreach (Ability ability in currentMoveset)
        {
            if (ability == null) continue;

            GameObject abilityGO = Instantiate(AbilityUITemplate, abilitiesParent);

            var icon = abilityGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = ability.AbilityIcon;

            AbiltySlider slider = abilityGO.transform.Find("Icon")?.GetComponent<AbiltySlider>();
            if (slider != null)
            {
                slider.ability = ability;
                slider.SetSliderValue(100f); // start full
                abilitySliders.Add(slider);
            }

            var nameText = abilityGO.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = ability.AbilityName;

            TMP_Text cooldownText = abilityGO.transform.Find("Cooldown")?.GetComponent<TMP_Text>();
            if (cooldownText != null) cooldownText.text = "Ready";

            if (ability.AbilityActivation != null)
                abilityInputs.Add(ability.AbilityActivation.action);
            else
                abilityInputs.Add(default);

            cooldownTimers.Add(ability.AbilityCooldown); // start at max cooldown (ready)
            cooldownTexts.Add(cooldownText);
        }

        Debug.Log($"Built ability UI for {currentMoveset.Length} abilities for {type}");
    }


    private void Update()
    {
        // Cache player if lost (avoid repeated heavy finds)
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

        // safety: skip if UI lists not built
        int safeCount = Mathf.Min(
            currentMoveset.Length,
            abilityInputs.Count,
            cooldownTimers.Count,
            cooldownTexts.Count,
            abilitySliders.Count);

        for (int i = 0; i < safeCount; i++)
        {
            Ability ability = currentMoveset[i];

            // Cooldown timer update
            if (cooldownTimers[i] < ability.AbilityCooldown)
            {
                cooldownTimers[i] += Time.deltaTime;
                if (cooldownTimers[i] > ability.AbilityCooldown)
                    cooldownTimers[i] = ability.AbilityCooldown;
            }

            // Update cooldown text
            float remaining = ability.AbilityCooldown - cooldownTimers[i];
            if (remaining > 0)
                cooldownTexts[i].text = remaining.ToString("F1");
            else
                cooldownTexts[i].text = "Ready";

            // Update slider (percentage of cooldown completed)
            float fillPercent = (cooldownTimers[i] / ability.AbilityCooldown) * 100f;
            abilitySliders[i].SetSliderValue(fillPercent);

            // Input check
            if (abilityInputs[i] != null && abilityInputs[i].WasPressedThisFrame() && cooldownTimers[i] >= ability.AbilityCooldown)
            {
                ability.ActivateAbility(player);
                cooldownTimers[i] = 0f; // reset cooldown
            }
        }
    }

    private void OnDestroy()
    {
        // last safety unsubscribe
        if (roundManager != null && subscribed)
        {
            roundManager.OnRoundStart -= OnRoundStart;
            subscribed = false;
        }
    }
}

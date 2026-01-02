using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum ClientEffectId : byte
{
    UnstablePresence = 1,
    // Add more: Fear, Hallucination, BleedVision, etc.
}

public class StatusEffectReceiver : MonoBehaviourPun
{
    // Stack-safe: effect can have multiple sources (multiple killers / re-applies)
    private readonly Dictionary<ClientEffectId, int> stacks = new();
    private readonly Dictionary<ClientEffectId, float> intensity = new();

    public void ApplyEffect(ClientEffectId id, bool active, float amount = 1f)
    {
        if (!photonView.IsMine) return; // only local client changes its own visuals

        if (!stacks.ContainsKey(id)) stacks[id] = 0;
        stacks[id] += active ? 1 : -1;
        stacks[id] = Mathf.Max(0, stacks[id]);

        if (active) intensity[id] = Mathf.Max(intensity.ContainsKey(id) ? intensity[id] : 0f, amount);
        if (stacks[id] == 0) intensity[id] = 0f;

        bool enabledNow = stacks[id] > 0;
        float amt = intensity[id];

        // Route to actual implementations
        switch (id)
        {
            case ClientEffectId.UnstablePresence:
                SetUnstablePresence(enabledNow, amt);
                break;
        }
    }

    private void SetUnstablePresence(bool on, float amt)
    {
        Volume vol = GameObject.Find("Global Volume").GetComponent<Volume>();
        if (vol != null)
        {
            if (vol.profile.TryGet(out ChromaticAberration chromatic))
            {
                chromatic.intensity.value = on ? Mathf.Clamp01(amt * 0.5f) : 0f;
            }
            if (vol.profile.TryGet(out FilmGrain film))
            {
                film.intensity.value = on ? Mathf.Clamp(amt * 10f, 0f, 30f) : 0f;
                // TODO: smooth transitions for both effects
            }
        }
    }
}

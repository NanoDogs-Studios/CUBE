using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class PassiveRunner : MonoBehaviourPun
{
    [Header("Tick")]
    [Min(0.05f)] public float tickInterval = 0.25f;

    public UnityEvent onTick;

    private Coroutine loop;

    protected virtual void OnEnable()
    {
        // authority: only the owner runs passives
        if (!photonView || !photonView.IsMine) return;

        loop = StartCoroutine(TickLoop());
    }

    protected virtual void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator TickLoop()
    {
        var wait = new WaitForSeconds(tickInterval);

        while (true)
        {
            Tick();
            yield return wait;
        }
    }

    public virtual void Tick()
    {
        if (!photonView.IsMine) return;
        onTick?.Invoke();
    }
}

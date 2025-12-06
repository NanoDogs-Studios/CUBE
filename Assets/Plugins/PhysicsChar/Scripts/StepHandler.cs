using UnityEngine;

public class StepHandler : MonoBehaviour
{

    public PartAnimation[] Legs;
    private float timer = 0f;
    public float interval = 0.5f;
    bool Left;

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if (timer >= interval)
        {
            timer -= interval;

            OnStepInterval();
        }
    }

    private void OnStepInterval()
    {
        if (Left)
        {
            // left
            Legs[0].Step();
            Left = false;
        }
        else
        {
            // right
            Legs[1].Step();
            Left = true;
        }

    }
}
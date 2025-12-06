using UnityEngine;
using UnityEngine.Audio;

public class FootstepSC : MonoBehaviour
{
    [Header("Foot Colliders")]
    public CollisionHandler leftFoot;
    public CollisionHandler rightFoot;

    [Header("Settings")]
    public AudioClip[] steps;
    public AudioMixerGroup sfxMixer;

    private bool leftFootOnGround = false;
    private bool rightFootOnGround = false;

    void Update()
    {
        HandleFootstep(leftFoot, ref leftFootOnGround);
        HandleFootstep(rightFoot, ref rightFootOnGround);
    }

    void HandleFootstep(CollisionHandler foot, ref bool footOnGround)
    {
        // landed this frame
        if (foot.TouchGround && !footOnGround)
        {
            footOnGround = true;
            PlayRandomFootstep(foot);
        }
        // lifted this frame
        else if (!foot.TouchGround && footOnGround)
        {
            footOnGround = false;
        }
    }

    void PlayRandomFootstep(CollisionHandler foot)
    {
        if (steps.Length == 0) return;

        int randomIndex = Random.Range(0, steps.Length);
        AudioClip step = steps[randomIndex];
        //PlayClipAtPoint(step, pos, 1, sfxMixer);
        foot.GetComponent<AudioSource>().clip = step;
        foot.GetComponent<AudioSource>().Play();
    }
}

using UnityEngine;

public class FRILerp : MonoBehaviour
{
    private void Start()
    {
    }

    public static Vector3 Lerp(Vector3 from, Vector3 target, float speed, bool useTimeScale = true)
    {
        return Vector3.Lerp(from, target, 1f - Mathf.Exp((0f - speed) * (useTimeScale ? Time.deltaTime : Time.unscaledDeltaTime)));
    }

    public static Vector3 LerpUnclamped(Vector3 from, Vector3 target, float speed)
    {
        return Vector3.LerpUnclamped(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }

    public static float Lerp(float from, float target, float speed)
    {
        return Mathf.Lerp(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }

    public static float LerpUnclamped(float from, float target, float speed)
    {
        return Mathf.LerpUnclamped(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }

    public static Vector3 Slerp(Vector3 from, Vector3 target, float speed)
    {
        return Vector3.Slerp(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }

    public static Vector3 SlerpUnclamped(Vector3 from, Vector3 target, float speed)
    {
        return Vector3.SlerpUnclamped(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }

    public static Quaternion Lerp(Quaternion from, Quaternion target, float speed)
    {
        return Quaternion.Lerp(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }

    public static Quaternion LerpUnclamped(Quaternion from, Quaternion target, float speed)
    {
        return Quaternion.LerpUnclamped(from, target, 1f - Mathf.Exp((0f - speed) * Time.deltaTime));
    }
}

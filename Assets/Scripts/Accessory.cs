using UnityEngine;

/// <summary>
/// Represents an accessory that can be attached to a parent object in a scene.
/// </summary>
/// <remarks>The <see cref="Accessory"/> class defines the necessary components for attaching a prefab to a parent
/// object, including its position and rotation relative to the parent. This class is typically used in scenarios where
/// dynamic object composition or customization is required.</remarks>
[System.Serializable]
public class Accessory
{
    public GameObject prefab;
    public Transform parent;
    public Vector3 localPosition;
    public Quaternion localRotation;
}
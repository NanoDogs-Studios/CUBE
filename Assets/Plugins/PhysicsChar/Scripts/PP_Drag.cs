using System;
using UnityEngine;

public class PP_Drag : MonoBehaviour
{
	private Rig data;

	public PP_Drag_State[] dragStates;

	private void Start()
	{
		data = GetComponent<Rig>();
		for (int i = 0; i < data.allRigs.Count; i++)
		{
			data.allRigs[i].maxAngularVelocity = 500f;
		}
	}

	private void FixedUpdate()
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < dragStates.Length; i++)
		{
			if (data.Currentstate == dragStates[i].animState)
			{
				num = dragStates[i].drag;
				num2 = dragStates[i].angularDrag;
			}
		}
		for (int j = 0; j < data.allRigs.Count; j++)
		{
			Vector3 velocity = data.allRigs[j].linearVelocity;
			Vector3 angularVelocity = data.allRigs[j].angularVelocity;
			data.allRigs[j].linearVelocity -= velocity.normalized * data.control * num * Mathf.Pow(velocity.magnitude, 0.5f);
			data.allRigs[j].angularVelocity -= angularVelocity.normalized * data.control * num2 * Mathf.Pow(angularVelocity.magnitude, 0.5f);
		}
	}
}


[Serializable]
public class PP_Drag_State
{
    public StateType animState;

    public float drag;

    public float angularDrag;
}

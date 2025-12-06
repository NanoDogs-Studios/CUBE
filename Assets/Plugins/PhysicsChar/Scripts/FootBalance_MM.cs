using UnityEngine;

public class FootBalance_MM : MonoBehaviour
{
    public Rig data;

    public Rigidbody rig;

    public Rigidbody fR;

    public Rigidbody fL;

    public float force;

    public void Start()
    {
        rig = GetComponent<Rigidbody>();
        data = GetComponentInParent<Rig>();
        fR = base.transform.parent.GetComponentInChildren<FootRight_MM>().GetComponent<Rigidbody>();
        fL = base.transform.parent.GetComponentInChildren<FootLeft_MM>().GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {

        Vector3 zero = Vector3.zero;
        for (int i = 0; i < data.allRigs.Count; i++)
        {
            zero += data.allRigs[i].position;
        }
        zero /= data.allRigs.Count;
        Vector3 vector = (fR.position + fL.position) * 0.5f;
        zero.y = 0f;
        vector.y = 0f;
        float y = rig.transform.position.y;
        float y2 = data.torso.transform.position.y;
        float num = Mathf.Clamp(0f - (y - y2), 0f, 1f);
        rig.AddForce((zero - vector) * num * data.control * force, ForceMode.Acceleration);
    }
}

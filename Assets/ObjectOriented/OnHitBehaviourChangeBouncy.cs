using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnHitBehaviourChangeBouncy : OnHitBehaviourChange
{
    bool m_Bouncy;
    public PhysicMaterial physmat;
    public Vector3 startforse;

    public override void CollisionAction() // If not yet activated, on hit change restitution
    {
        if (!m_Bouncy)
        {
            GetComponent<Collider>().material = physmat;
            Rigidbody thisrg = GetComponent<Rigidbody>();
            thisrg.AddForce(startforse, ForceMode.Impulse);
            m_Bouncy = true;
        }
    }
}

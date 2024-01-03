using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnHitBehaviourChangeMass : OnHitBehaviourChange
{
    public override void CollisionAction()
    {
        Rigidbody thisrg = GetComponent<Rigidbody>();
        thisrg.mass *= 2;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnHitBehaviourChangeSpeed: OnHitBehaviourChange
{
    public override void CollisionAction()
    {
        Vector3 grav = Physics.gravity;
        grav.x *= 1.1f;
        grav.y *= 1.1f;
        grav.z *= 1.1f;
        Physics.gravity = grav; // Override global physics gravity on each hit.
    }
}

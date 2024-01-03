using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class OnHitBehaviourChange : MonoBehaviour
{
    public abstract void CollisionAction();

    public void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }
        if (collision.relativeVelocity.magnitude > 0)
        {
            CollisionAction();
        }
    }
}

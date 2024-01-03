using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base abstract class for all components that "change" behaviour of sphere on each hit.
public abstract class OnHitBehaviourChange : MonoBehaviour
{
    // Overridable function to execute on each collision.
    public abstract void CollisionAction();

    // OnCollisionEnter is a function called by Unity on each collision.
    public void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }
        // If collision is powerful enough,
        if (collision.relativeVelocity.magnitude > 0)
        {
            CollisionAction(); // execute user-defined CollisionAction
        }
    }
}

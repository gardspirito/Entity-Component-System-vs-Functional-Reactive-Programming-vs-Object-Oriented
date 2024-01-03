using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnHitBehaviourChangeColour : OnHitBehaviourChange
{
    public Material oldmat;
    public Material newmat;
    public int hits = 0;

    public override void CollisionAction() // On each even hit set color to white, on odd - to red.
    {
        hits++;

        if (hits % 2 == 0)
        {
            Renderer thisMat = GetComponent<Renderer>();
            thisMat.material.color = oldmat.color;
        }
        else
        {
            Renderer thisMat = GetComponent<Renderer>();
            thisMat.material.color = newmat.color;
        }
    }
}

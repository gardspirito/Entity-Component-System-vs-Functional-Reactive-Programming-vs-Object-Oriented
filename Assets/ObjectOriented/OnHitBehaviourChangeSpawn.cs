using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnHitBehaviourChangeSpawn : OnHitBehaviourChange
{
    public GameObject sphere;

    public override void CollisionAction() // On hit spawn new object from prefab
    {
        Transform transform = GetComponent<Transform>();
        Vector3 newPosition = transform.position;
        newPosition.y += 20.0f;
        Instantiate(sphere, newPosition, transform.rotation);
    }
}

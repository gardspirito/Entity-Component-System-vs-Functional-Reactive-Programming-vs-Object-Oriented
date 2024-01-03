using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class CollisionsAuthoring : MonoBehaviour
{
	public class CollisionsBaker : Baker<CollisionsAuthoring> {
		public override void Bake(CollisionsAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddBuffer<CollisionsComponent>(entity);
		}
	}
}

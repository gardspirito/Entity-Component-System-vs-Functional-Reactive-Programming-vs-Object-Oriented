using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SpawnerBallAuthoring : MonoBehaviour
{
	public GameObject prefab;
	
	public class SpawnerBallBaker : Baker<SpawnerBallAuthoring> {
		public override void Bake(SpawnerBallAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new SpawnerBallComponent() {
				prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
				spawned = 0 
			});
		}
	}
}

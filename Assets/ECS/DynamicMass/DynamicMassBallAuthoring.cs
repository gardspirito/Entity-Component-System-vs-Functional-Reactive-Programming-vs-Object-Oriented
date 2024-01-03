using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class DynamicMassBallAuthoring : MonoBehaviour
{	
	public class DynamicMassBallBaker : Baker<DynamicMassBallAuthoring> {
		public override void Bake(DynamicMassBallAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new DynamicMassBallComponent() {});
		}
	}
}

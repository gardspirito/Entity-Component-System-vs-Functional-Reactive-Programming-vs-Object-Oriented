using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class DynamicColorBallAuthoring : MonoBehaviour
{	
	public class DynamicColorBallBaker : Baker<DynamicColorBallAuthoring> {
		public override void Bake(DynamicColorBallAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new DynamicColorBallComponent() {
				toggled = false
			});
		}
	}
}

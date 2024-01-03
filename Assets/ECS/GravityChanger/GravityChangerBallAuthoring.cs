using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class GravityChangerBallAuthoring : MonoBehaviour
{	
	public class GravityChangerBallBaker : Baker<GravityChangerBallAuthoring> {
		public override void Bake(GravityChangerBallAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new GravityChangerBallComponent() {});
		}
	}
}

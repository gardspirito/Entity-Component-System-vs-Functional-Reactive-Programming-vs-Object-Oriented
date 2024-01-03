using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class BouncyBallAuthoring : MonoBehaviour
{	
	public class BouncyBallBaker : Baker<BouncyBallAuthoring> {
		public override void Bake(BouncyBallAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new BouncyBallComponent() {
				toggled = false
			});
		}
	}
}

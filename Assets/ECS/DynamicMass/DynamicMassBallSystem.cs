using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.Physics.Aspects;

[BurstCompile]
public partial struct DynamicMassBallSystem : ISystem {
	
	public void OnUpdate(ref SystemState state) {
		new DynamicMassJob() {}.ScheduleParallel();
	}
	
	private partial struct DynamicMassJob : IJobEntity {
		public void Execute(in DynamicMassBallComponent _, ref DynamicBuffer<CollisionsComponent> collisionsBuffer, RigidBodyAspect aspect) {
			if (CollisionsSystem.HasNewCollisions(ref collisionsBuffer)) { // On collision
				aspect.Mass *= 2f; // change mass
			}
		}
	}
}

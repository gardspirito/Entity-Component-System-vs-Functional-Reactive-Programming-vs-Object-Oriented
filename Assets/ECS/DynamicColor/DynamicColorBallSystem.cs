using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.Rendering;

[BurstCompile]
public partial struct DynamicColorBallSystem : ISystem {
	
	public void OnUpdate(ref SystemState state) {
		new DynamicColorJob() {}.ScheduleParallel();
	}
	
	private partial struct DynamicColorJob : IJobEntity {
		public void Execute(ref DynamicBuffer<CollisionsComponent> collisionsBuffer, ref DynamicColorBallComponent dynamicColor, ref URPMaterialPropertyBaseColor color) {
			if (CollisionsSystem.HasNewCollisions(ref collisionsBuffer)) {
				dynamicColor.toggled = !dynamicColor.toggled;
				var targetColor = dynamicColor.toggled ? Color.red : Color.white;
				color.Value.x = targetColor.r;
    			color.Value.y = targetColor.g;
    			color.Value.z = targetColor.b;
    			color.Value.w = targetColor.a;
			}
		}
	}
}

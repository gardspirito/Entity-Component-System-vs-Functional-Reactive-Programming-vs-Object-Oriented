using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Burst;
using Unity.Physics.Aspects;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BouncyBallSystem : ISystem {
	
	public void OnUpdate(ref SystemState state) {
		new BouncyBallJob() {}.ScheduleParallel();
	}
	
	private partial struct BouncyBallJob : IJobEntity {
		public void Execute(ref DynamicBuffer<CollisionsComponent> collisionsBuffer, ref BouncyBallComponent bouncyBall, ColliderAspect collider) {
			if (!bouncyBall.toggled && CollisionsSystem.HasNewCollisions(ref collisionsBuffer)) {
				bouncyBall.toggled = true;
				collider.SetRestitution(0.9f);
				collider.Position += new float3(0f, 5f, 0f);
			}
		}
	}
}

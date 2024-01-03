using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct SpawnerBallSystem : ISystem {
	
	public void OnUpdate(ref SystemState state) {
		// Schedule parallel job to handle duplicating spheres.
		// It is passed CommandBuffer, which allows it to queue addition of new entities/components to the worl.
		new SpawnerJob() {
			Ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.WorldUnmanaged)
		    	.AsParallelWriter()
        }.ScheduleParallel();
	}
	
	private partial struct SpawnerJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter Ecb;
		
		public void Execute([ChunkIndexInQuery] int chunkIndex, ref DynamicBuffer<CollisionsComponent> collisionsBuffer, ref SpawnerBallComponent spawner, in LocalTransform transform) {
			if (spawner.spawned < 8 && CollisionsSystem.HasNewCollisions(ref collisionsBuffer)) { // If collision occured
				Entity ent = Ecb.Instantiate(chunkIndex, spawner.prefab); // instantiate prefab in the world
				Ecb.SetComponent(chunkIndex, ent, transform.WithPosition(transform.Position + new float3(0f, 10f, 0f))); // change position of instantiate object
				spawner.spawned += 1;
			}
		}
	}
}

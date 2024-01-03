using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

// Basic collision registering system.
// Registers a job that intercepts collisions events from Unity.Physics and
// Write them to CollisionsComponent of attached entity.
// Based on these events, it is possible to detect new collisions with HasNewCollisions method.
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[BurstCompile]
public partial struct CollisionsSystem : ISystem {

    public void OnCreate(ref SystemState state) {
		// System depends on SimulationSingleton.
    	state.RequireForUpdate<SimulationSingleton>();
    }
    
	public void OnUpdate(ref SystemState state) {
		// Schedule the job with PhysicsWorldSingleton and CollisionsComponent dependencies.
		state.Dependency = new CollisionsJob() {
			physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
			collisionBufferLookup = SystemAPI.GetBufferLookup<CollisionsComponent>(false)
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
    
	// Given CollisionsComponent, detect new collisions and mark all existing collisions as "seen".
	// This method is used by other systems.
    public static bool HasNewCollisions(ref DynamicBuffer<CollisionsComponent> collisionsBuffer) {
    	bool result = false;
    	for (int i = 0; i < collisionsBuffer.Length; i++) {
    		result |= collisionsBuffer[i].status == CollisionStatus.Enter;
    		if (collisionsBuffer[i].status == CollisionStatus.Exit) {
    			collisionsBuffer.RemoveAtSwapBack(i);
    			i--;
    		} else {
    			collisionsBuffer[i] = new CollisionsComponent() { entity = collisionsBuffer[i].entity, status = CollisionStatus.Exit };
    		}
    	}
    	return result;
    }
    
    private struct CollisionsJob : ICollisionEventsJob {
   		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
    	public BufferLookup<CollisionsComponent> collisionBufferLookup;
    	
		// Utility function. Save to Entity ent collision with Entity with.
    	private void SaveCollision(Entity ent, Entity with, CollisionStatus newCollisionStatus) {
    		DynamicBuffer<CollisionsComponent> collisionsBuffer;
    		if (!collisionBufferLookup.TryGetBuffer(ent, out collisionsBuffer))
    			return;
    		for (int i = 0; i < collisionsBuffer.Length; i++) {
    			if (collisionsBuffer[i].entity == with) {
    				if (collisionsBuffer[i].status == CollisionStatus.Exit)
    					collisionsBuffer[i] = new CollisionsComponent() { entity = with, status = CollisionStatus.Stay };
    				return;
    			}
    		
    		}

    		collisionsBuffer.Add(new CollisionsComponent() { entity = with, status = newCollisionStatus });
    	}
    	
		// Method executed on each collision.
    	public void Execute(CollisionEvent ev) {
    		float impulse = ev.CalculateDetails(ref physicsWorld.PhysicsWorld).EstimatedImpulse;
    		CollisionStatus status = impulse > 0.6f ? CollisionStatus.Enter : CollisionStatus.Stay;
    		SaveCollision(ev.EntityA, ev.EntityB, status);
    		SaveCollision(ev.EntityB, ev.EntityA, status);
    	}
    }
}

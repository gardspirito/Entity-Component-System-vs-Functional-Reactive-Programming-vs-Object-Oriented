using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[BurstCompile]
public partial struct CollisionsSystem : ISystem {

    public void OnCreate(ref SystemState state) {
    	state.RequireForUpdate<SimulationSingleton>();
    }
    
	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CollisionsJob() {
			physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
			collisionBufferLookup = SystemAPI.GetBufferLookup<CollisionsComponent>(false)
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
    
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
    	
    	public void Execute(CollisionEvent ev) {
    		float impulse = ev.CalculateDetails(ref physicsWorld.PhysicsWorld).EstimatedImpulse;
    		CollisionStatus status = impulse > 0.6f ? CollisionStatus.Enter : CollisionStatus.Stay;
    		SaveCollision(ev.EntityA, ev.EntityB, status);
    		SaveCollision(ev.EntityB, ev.EntityA, status);
    	}
    }
}

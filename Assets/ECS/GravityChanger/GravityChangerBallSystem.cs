using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

public partial struct GravityChangerBallSystem : ISystem {
	public void OnCreate(ref SystemState state) {
    	state.RequireForUpdate<PhysicsStep>();
    }
    
    public void OnUpdate(ref SystemState state) {
		// Schedule synchronous job with access to PhysicsStep singleton which controls global physics environmental variables.
    	new GravityChangerJob() { physicsStep = SystemAPI.GetSingletonRW<PhysicsStep>() }.Schedule();
    }
    
    private partial struct GravityChangerJob : IJobEntity {
    	[NativeDisableUnsafePtrRestriction] public RefRW<PhysicsStep> physicsStep;
    	
    	public void Execute(in GravityChangerBallComponent _, ref DynamicBuffer<CollisionsComponent> collisions) {
			if (CollisionsSystem.HasNewCollisions(ref collisions)) { // On collision
    			physicsStep.ValueRW.Gravity *= 1.1f; // Increase global gravit
    		}
    	}
    }
}

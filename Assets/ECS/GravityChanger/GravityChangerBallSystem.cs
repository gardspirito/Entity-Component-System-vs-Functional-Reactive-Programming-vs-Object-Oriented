using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

/*
public partial class GravityChangerBallSystem : SystemBase
{	
    protected override void OnUpdate() {
		ref var physicsStep = ref SystemAPI.GetSingletonRW<PhysicsStep>().ValueRW;
    	Entities.ForEach((GravityChangerBallComponent _, ref DynamicBuffer<CollisionsComponent> collisions) => {
			if (CollisionsSystem.HasNewCollisions(ref collisions)) {
				physicsStep.Gravity = 0.9f * physicsStep.Gravity;
				Debug.Log(physicsStep.Gravity);
			}
		}).Schedule();
    }
}*/

public partial struct GravityChangerBallSystem : ISystem {
	public void OnCreate(ref SystemState state) {
    	state.RequireForUpdate<PhysicsStep>();
    }
    
    public void OnUpdate(ref SystemState state) {
		Debug.Log("OnUpdate");
    	new GravityChangerJob() { physicsStep = SystemAPI.GetSingletonRW<PhysicsStep>() }.Schedule();
    }
    
    private partial struct GravityChangerJob : IJobEntity {
    	[NativeDisableUnsafePtrRestriction] public RefRW<PhysicsStep> physicsStep;
    	
    	public void Execute(in GravityChangerBallComponent _, ref DynamicBuffer<CollisionsComponent> collisions) {
			if (CollisionsSystem.HasNewCollisions(ref collisions)) {
    			physicsStep.ValueRW.Gravity *= 1.1f;
    		}
    	}
    }
}

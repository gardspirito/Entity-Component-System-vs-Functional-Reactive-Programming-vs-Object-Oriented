using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public enum CollisionStatus {
	Enter, Stay, Exit
}

public struct CollisionsComponent : IBufferElementData
{
	public Entity entity;
	public CollisionStatus status;
}

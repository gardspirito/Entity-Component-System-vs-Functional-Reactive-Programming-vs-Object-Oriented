using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public struct SpawnerBallComponent : IComponentData {
	public Entity prefab;
	public int spawned;
}

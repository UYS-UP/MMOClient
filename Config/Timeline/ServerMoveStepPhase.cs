
using UnityEngine;

[JsonTypeAlias(nameof(ServerMoveStepPhase))]
public class ServerMoveStepPhase : SkillPhase
{
    public float Distance { get; set; }
    public Vector3 MoveDirection { get; set; }
    public float[] DistanceSamples { get; set; }
}

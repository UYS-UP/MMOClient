
using UnityEngine;

[JsonTypeAlias(nameof(MoveStepPhase))]
public class MoveStepPhase : SkillPhase
{
    public float Distance { get; set; }
    public AnimationCurve Curve { get; set; }
    public Vector3 MoveDirection { get; set; }
    
    private float elapsed;
    private float duration;
    private float lastRatio;
    private Vector3 cachedWorldDir;

    public override void OnStart(EntityBase caster)
    {
        if(!caster.IsLocal) return;
        elapsed = 0f;
        lastRatio = 0f;
        duration = Mathf.Max(0.001f, EndTime - StartTime);
        Vector3 localDir = MoveDirection.sqrMagnitude < 0.001f ? Vector3.forward : MoveDirection.normalized;
        cachedWorldDir = caster.transform.TransformDirection(localDir);
        cachedWorldDir.Normalize();
    }

    public override void OnUpdate(EntityBase caster, float dt)
    {
        if(!caster.IsLocal) return;
        elapsed += dt;
        float t = Mathf.Clamp01(elapsed / duration);
        
        float ratio = Curve.Evaluate(t);
        float deltaRatio = ratio - lastRatio;
        lastRatio = ratio;

        if (Mathf.Abs(deltaRatio) < 0.0001f)
            return;

        Vector3 delta = cachedWorldDir * (Distance * deltaRatio);

        caster.GetEntityComponent<LocalMoveComponent>()
            ?.AddExternalMotion(delta);
    }
    
}


using UnityEngine;

[JsonTypeAlias(nameof(MoveStepPhase))]
public class MoveStepPhase : SkillPhase
{
    public float Distance { get; set; }
    public int SkillId { get; set; }
    public AnimationCurve Curve { get; set; }
    
    private float elapsed;
    private float duration;
    private float lastRatio;

    public override void OnStart(EntityBase caster)
    {
        elapsed = 0f;
        lastRatio = 0f;
        duration = Mathf.Max(0.001f, EndTime - StartTime);
    }

    public override void OnUpdate(EntityBase caster, float dt)
    {
        elapsed += dt;
        float t = Mathf.Clamp01(elapsed / duration);
        
        float ratio = Curve.Evaluate(t);
        float deltaRatio = ratio - lastRatio;
        lastRatio = ratio;

        if (Mathf.Abs(deltaRatio) < 0.0001f)
            return;

        Vector3 dir = caster.transform.forward;
        Vector3 delta = dir * (Distance * deltaRatio);

        caster.GetEntityComponent<LocalMoveComponent>()
            ?.AddExternalMotion(delta);
    }
    
}

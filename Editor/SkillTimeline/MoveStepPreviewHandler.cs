using UnityEngine;
using UnityEngine.Playables;

[SkillHandler(typeof(MoveStepPhase))]
public class MoveStepPreviewHandler : SkillPreviewHandler
{
    public override void OnSeek(GameObject target, object data, float localTime, PlayableGraph graph)
    {
        Debug.Log("OnSeek called Move");
        var phase = data as MoveStepPhase;
        if (phase == null) return;

        float duration = phase.EndTime - phase.StartTime;
        if (duration <= 0.0001f) return;


        float t = Mathf.Clamp(localTime, 0, duration);
        float progress = t / duration;

        float curveVal = (phase.Curve != null && phase.Curve.length > 0) 
            ? phase.Curve.Evaluate(progress) 
            : progress;

        float dist = phase.Distance * curveVal;

        // 累加位移
        target.transform.position += target.transform.forward * dist;
    }
}
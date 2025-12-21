
using UnityEditor;
using UnityEngine;

[JsonTypeAlias(nameof(AnimationEvent))]
public class AnimationEvent : SkillEvent
{
    public string Animation { get; set; }

    public override void Execute(EntityBase caster)
    {
        if (string.IsNullOrEmpty(this.Animation)) return;
        Debug.Log("播放动画:" + this.Animation);
        caster.GetEntityComponent<AnimatorComponent>().CrossFade(Animation, 0.25f);
    }


}

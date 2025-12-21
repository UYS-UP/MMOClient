
using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillInstance
{
    public int SkillId;
    public EntityBase Caster;
    
    private readonly SkillTimelineRunner runner;
    private readonly Action onFinished;
    
    public float CurrentTime => runner.CurrentTime;
    public bool IsFinished => runner.IsFinished;
    
    public SkillInstance(int skillId, EntityBase caster, Action onFinished = null)
    {
        if (!GameContext.Instance.SkillTimelineConfig.TryGetValue(skillId, out var config))
        {
            Debug.LogError($"该{skillId}技能不存在TimelineConfig配置");
            return;
        }
        SkillId = skillId;
        Caster = caster;
        this.onFinished = onFinished;
        
        runner = new SkillTimelineRunner(config.Duration, config.ClientEvents, config.ClientPhases);
    }
    
    public void Start()
    {
        runner.Start(Caster);

        if (Caster.IsLocal)
        {
            var payload = new ClientPlayerReleaseSkill
            {
                SkillId = SkillId,
                ClientTick = TickService.Instance.ClientTick,
                InputType = SkillCastInputType.None
            };
            GameClient.Instance.Send(Protocol.PlayerReleaseSkill, payload);
        }
    }
    
    public void Update(float dt)
    {
        if (runner.IsFinished) return;
        runner.Tick(Caster, dt);
        if (runner.IsFinished)
        {
            Debug.Log("Finish");
            onFinished?.Invoke();
        }
    }

    public void Interrupt()
    {
        if (runner.IsFinished) return;
        Debug.Log("Interrupt");
        runner.Interrupt(Caster);
        onFinished?.Invoke();
    }

}

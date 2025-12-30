using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class LocalSkillComponent : BaseComponent
{
    private EntityBase entity;
    private InputComponent input;
    private SkillModel skillModel;
    private SkillIndicator skillIndicator;
    private int currentComboSkillId = -1;
    
    public override void Attach(EntityBase e)
    {
        entity = e;
        skillModel = GameContext.Instance.Get<SkillModel>();
        skillIndicator = new SkillIndicator(GameContext.Instance.MainCamera, entity.transform);

        input = e.GetEntityComponent<InputComponent>();
        if (input != null) input.ActionStarted += OnSkillInput;

        if (skillIndicator != null) skillIndicator.OnUnitTargetSelected += OnUnitTargetSelected;

    }
    
    
    

    public override void UpdateEntity(float dt)
    {
        skillIndicator?.Update();
        skillModel?.UpdateCooldown(dt);
        
    }
    
    public override void ClearComponent()
    {
        if (input != null) input.ActionStarted -= OnSkillInput;
        if (skillIndicator != null) skillIndicator.OnUnitTargetSelected -= OnUnitTargetSelected;
    }
    
    
    private void OnUnitTargetSelected(int skillId, int targetEntityId)
    {
        if (!entity.IsLocal) return;
        if (skillModel != null && !skillModel.CheckSkill(skillId))
            return;
        
        skillModel?.CastSkill(skillId, true);


        // var payload = new ClientPlayerReleaseSkill
        // {
        //     SkillId = skillId,
        //     ClientTick = pendingClientTick,
        //     InputType = SkillCastInputType.UnitTarget,
        //     TargetEntityId = targetEntityId
        // };
        //
        // GameClient.Instance.Send(Protocol.PlayerReleaseSkill, payload);
    }

    private void OnSkillInput(PlayerAction action)
    {
        
        
        var ctx = entity.FSM.Ctx;
        
        if (action == PlayerAction.Roll)
        {
            if (ctx.CastSkill != null && !ctx.CastSkill.IsFinished)
            {
                return; 
            }
            ctx.RollRequested = true;
            return;
        }
        
        if (action == PlayerAction.Attack)
        {
            if (ctx.ComboWindowOpen)
            {
                ctx.ComboRequested = true;
                return;
            }
            if (ctx.CastSkill != null && !ctx.CastSkill.IsFinished)
            {
                return; 
            }
            ctx.AttackRequested = true;
            return;
        }
        
        int skillId = MapActionToSkillId(action);
        if (skillId < 0) return;
        
        if (skillModel != null && !skillModel.CheckSkill(skillId))
            return;
        
        ctx.RequestCast(skillId);

    }
    
    private int MapActionToSkillId(PlayerAction action)
    {
        return action switch
        {
            PlayerAction.Attack => 0,
            PlayerAction.Skill1 => 3,
            _ => -1
        };
    }


}

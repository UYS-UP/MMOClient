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
    
    
    private void OnUnitTargetSelected(int skillId, string targetEntityId)
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

        // 普攻：如果正在施法，只允许在窗口期触发连段；否则一律忽略（不缓冲）
        if (action == PlayerAction.Attack && ctx.CastSkill != null && !ctx.CastSkill.IsFinished)
        {
            if (ctx.ComboWindowOpen)
                ctx.ComboRequested = true;
            return;
        }

        // 非施法状态：正常起手
        int skillId = MapActionToSkillId(action);
        if (skillId < 0) return;

        if (skillModel != null && !skillModel.CheckSkill(skillId))
            return;

        skillModel?.CastSkill(skillId, true);
        ctx.RequestCast(skillId);
        // pendingLocalSkillId = skillId;
        // pendingClientTick = TickService.Instance.ClientTick;
        //
        // var payload = new ClientPlayerReleaseSkill
        // {
        //     SkillId = skillId,
        //     ClientTick = pendingClientTick,
        //     InputType = SkillCastInputType.None
        // };
        // GameClient.Instance.Send(Protocol.PlayerReleaseSkill, payload);
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

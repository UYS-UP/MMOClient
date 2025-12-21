using UnityEngine;

public class RemoteSkillComponent : BaseComponent
{
    private EntityBase entity;


    public override void Attach(EntityBase e)
    {
        entity = e;
    }

    public override void UpdateEntity(float dt)
    {

    }

    public void CastSkill(int skillId)
    {
        Debug.Log("Remote CastSkill" + skillId);
        entity.FSM.Ctx.RemoteRequestCast(skillId);
    }
    
}
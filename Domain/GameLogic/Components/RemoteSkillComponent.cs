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
    
    public void PlayFromServer(int skillId)
    {
        if (entity == null) return;

        // 找 timeline 配置
        if (!GameContext.Instance.SkillTimelineConfig.TryGetValue(skillId, out var timeline))
        {
            Debug.LogWarning($"RemoteSkillComponent: timeline not found skillId={skillId}");
            return;
        }
        
        Interrupt();
        
    }

    public void Interrupt()
    {

       
    }
}
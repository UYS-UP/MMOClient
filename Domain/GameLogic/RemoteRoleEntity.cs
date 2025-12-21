
using UnityEngine;

public class RemoteRoleEntity : EntityBase
{
    protected override void SetupComponents()
    {
        AddEntityComponent(new RemoteMoveComponent());
        AddEntityComponent(new AnimatorComponent(GetComponentInChildren<Animator>()));
        AddEntityComponent(new RemoteSkillComponent());
    }
    
}

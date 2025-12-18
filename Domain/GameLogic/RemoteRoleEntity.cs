
using UnityEngine;

public class RemoteRoleEntity : EntityBase
{
    protected override void SetupComponents()
    {
        AddEntityComponent(new RemoteMoveComponent());
        AddEntityComponent(new AnimatorComponent(GetComponent<Animator>()));
        AddEntityComponent(new LocalSkillComponent());
    }
    
}

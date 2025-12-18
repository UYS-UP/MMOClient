
using UnityEngine;

public class RemoteMonsterEntity : EntityBase
{
    protected override void SetupComponents()
    {
        AddEntityComponent(new RemoteMoveComponent());
        AddEntityComponent(new AnimatorComponent(GetComponent<Animator>()));
        AddEntityComponent(new LocalSkillComponent());
    }
    
}

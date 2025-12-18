using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>本地玩家实体。</summary>
public class LocalRoleEntity : EntityBase
{
    protected override void SetupComponents()
    {
        AddEntityComponent(new InputComponent());
        AddEntityComponent(new CameraComponent(GameContext.Instance.MainCamera));

        AddEntityComponent(new LocalMoveComponent());
        AddEntityComponent(new AnimatorComponent(GetComponent<Animator>()));
        
        AddEntityComponent(new LocalSkillComponent());

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseComponent
{
    public virtual void Attach(EntityBase entity)
    {
        
    }

    public virtual void UpdateEntity(float dt)
    {
        
    }

    public virtual void LateUpdateEntity(float dt)
    {
        
    }

    public virtual void ClearComponent()
    {
        
    }

    public virtual void OnAnimatorMove()
    {
        
    }
}

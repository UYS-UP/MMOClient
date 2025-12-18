using UnityEngine;

public class DialogueComponent : BaseComponent
{
    private BoxCollider collider;
    private EntityBase entity;

    public override void Attach(EntityBase entity)
    {
        this.entity = entity;
        collider = entity.GetComponent<BoxCollider>();
    }

    public void OnTriggerEnter(Collider target)
    {
        EventService.Instance.Publish(this, new TriggerEnterNpcEventArgs
        {
            NpcId = entity.EntityId,
        });
    }

    public void OnTriggerExit(Collider target)
    {
        EventService.Instance.Publish(this, new TriggerExitNpcEventArgs
        {
            NpcId = entity.EntityId,
        });
    }
}

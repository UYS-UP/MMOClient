
using System;
using UnityEngine;

public class RemoteNpcEntity : EntityBase
{
    protected override void SetupComponents()
    {
        AddEntityComponent(new AnimatorComponent(GetComponent<Animator>()));
        AddEntityComponent(new DialogueComponent());

    }

    private void Start()
    {
        // AddEntityComponent(new AnimatorComponent(GetComponent<Animator>()));
        AddEntityComponent(new DialogueComponent());
    }
    

    private void OnTriggerEnter(Collider other)
    {
        GetEntityComponent<DialogueComponent>().OnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        GetEntityComponent<DialogueComponent>().OnTriggerExit(other);
    }
}

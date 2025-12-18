
using UnityEngine;

[JsonTypeAlias(nameof(EffectEvent))]
public class EffectEvent : SkillEvent
{
    public string Effect { get; set; }
    public Vector3 Offset { get; set; }
    public Quaternion Rotation { get; set; }
    
    public override void Execute(EntityBase caster)
    {
        Debug.Log("触发特效");
        var obj = ResourceService.Instance.LoadResource<GameObject>("Prefabs/Effect/" + Effect);
        var go = Object.Instantiate(obj);
        if (go == null) return;
        go.transform.position = caster.transform.position + Offset;
        go.transform.rotation = Rotation;
        var particles = go.GetComponentsInChildren<ParticleSystem>();
        float maxDuration = 0f;
        foreach (var particle in particles)
        {
            var main = particle.main;
            main.loop = false;
            particle.Play();
            if (main.duration > maxDuration)
            {
                maxDuration = main.duration;
            }
        }
        
        Object.Destroy(go, maxDuration + 1f);
    }
}

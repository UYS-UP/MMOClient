
using UnityEngine;

[JsonTypeAlias(nameof(EffectEvent))]
public class EffectEvent : SkillEvent
{
    public string Effect { get; set; }
    public Vector3 PositionOffset { get; set; }
    public Quaternion RotationOffset { get; set; }
    public float Duration { get; set; }
    public bool FollowTarget { get; set; }
    
    public override void Execute(EntityBase caster)
    {
        Debug.Log("触发特效");
        var obj = ResourceService.Instance.LoadResource<GameObject>("Prefabs/Effect/" + Effect);
        var instance = Object.Instantiate(obj);
        if (instance == null) return;
        if (FollowTarget)
        {
            // 使用 TransformPoint 和 Transform.rotation 确保相对caster正确
            instance.transform.position = caster.transform.TransformPoint(PositionOffset);
            instance.transform.rotation = caster.transform.rotation * RotationOffset;
        }
        else
        {
            // 如果不跟随，那么 PositionOffset 和 RotationOffset 就是世界坐标
            instance.transform.position = PositionOffset;
            instance.transform.rotation = RotationOffset;
        }
        float maxParticleDuration = 0f;
        var particles = instance.GetComponentsInChildren<ParticleSystem>();
        
        foreach (var particle in particles)
        {
            // 确保粒子不是循环播放，并设置正确的播放时长
            var main = particle.main;
            // 如果 Duration 字段大于0，我们用它作为“特效期望的持续时间”
            // 如果 Duration <= 0，则用粒子系统自带的 Main Module duration
            float particleDuration = (Duration > 0) ? Duration : main.duration;

            // 确保粒子的循环播放被禁用，并且立即播放
            main.loop = false;
            particle.Play();
            
            if (particleDuration > maxParticleDuration)
            {
                maxParticleDuration = particleDuration;
            }
        }

        // 5. 销毁特效
        // 销毁时间 = maxParticleDuration + 一个小延迟，确保播完
        float destroyTime = (maxParticleDuration > 0) ? maxParticleDuration : Duration; // 如果没有粒子，则使用 EffectEvent 自己的 Duration
        if (destroyTime <= 0) destroyTime = 1f; // 默认至少保留 1 秒

        Object.Destroy(instance, destroyTime + 0.5f); // 稍作延迟销毁
        
        Debug.Log($"[EffectEvent] Instantiated effect '{Effect}' at {instance.transform.position} for caster {caster.name}. Destroying in {destroyTime + 0.5f}s.");
    }
    
    

}

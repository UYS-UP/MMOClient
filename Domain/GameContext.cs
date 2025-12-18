using System;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

/// <summary>
/// 游戏上下文管理
/// 初始化游戏Model数据以及EntityWorld
/// </summary>
public class GameContext : SingletonMono<GameContext>
{
    private readonly Dictionary<Type, IDisposable> models = new Dictionary<Type, IDisposable>();
    public IReadOnlyDictionary<int, SkillTimelineConfig>  SkillTimelineConfig => SkillTimelineJsonSerializer.SkillConfigs;
    
    public Camera MainCamera;
    protected override void Awake()
    {
        base.Awake();
        SkillTimelineJsonSerializer.Deserializer("D:\\Project\\UnityDemo\\MMORPG\\Assets\\Generated\\SkillTimelineConfig.json");
        DontDestroyOnLoad(MainCamera);
        Register(new QuestModel());
        Register(new StorageModel());
        Register(new EntityModel());
        Register(new SkillModel());
        Register(new FriendModel());
        Register(new PlayerModel());
        Register(new DialogueModel());
        Register(new ChatModel());
        Register(new TeamModel());

        
    }

    public void Register<T>(T service) where T : IDisposable
    {
        models[typeof(T)] = service;
    }
    
    public T Get<T>() => (T)models[typeof(T)];

    protected override void OnDestroy()
    {
        foreach (var disposable in models.Values)
        {
            disposable.Dispose();
        }
        models.Clear();
        base.OnDestroy();
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[AttributeUsage(AttributeTargets.Class)]
public class SkillHandlerAttribute : Attribute
{
    public Type DataType;
    public SkillHandlerAttribute(Type dataType) { DataType = dataType; }
}

public abstract class SkillPreviewHandler
{
    public abstract void OnSeek(GameObject target, object data, float localTime, PlayableGraph graph);
    public virtual void OnSceneGUI(GameObject target, object data) { }
    public virtual void OnDestroy() { } 

}

public class SkillPreviewSystem
{
    private Dictionary<Type, SkillPreviewHandler> _handlers = new Dictionary<Type, SkillPreviewHandler>();
    private GameObject _target;
    
    // 状态备份
    private Vector3 _originPos;
    private Quaternion _originRot;
    private bool _hasCaptured = false;

    // 动画图
    private PlayableGraph _graph;
    private AnimationMixerPlayable _mixer; // Output 0
    
    public PlayableGraph Graph => _graph;
    public AnimationMixerPlayable Mixer => _mixer;

    public SkillPreviewSystem()
    {
        // 反射注册所有 Handler
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var t in types)
        {
            var attr = t.GetCustomAttribute<SkillHandlerAttribute>();
            if (attr != null && typeof(SkillPreviewHandler).IsAssignableFrom(t))
            {
                var handler = Activator.CreateInstance(t) as SkillPreviewHandler;
                _handlers[attr.DataType] = handler;
            }
        }
    }
    
    public void DrawSceneGUI(object activeItem)
    {
        if (_target == null || activeItem == null) return;

        // 根据当前选中的 Item 类型，找到对应的 Handler
        if (_handlers.TryGetValue(activeItem.GetType(), out var handler))
        {
            handler.OnSceneGUI(_target, activeItem);
        }
    }

    public void BindTarget(GameObject target)
    {
        if (_target == target) return;
        
        Cleanup(); // 切换对象时清理旧图
        _target = target;
        _hasCaptured = false;
        
        if (_target != null)
        {
            // 备份初始 Transform
            _originPos = _target.transform.position;
            _originRot = _target.transform.rotation;
            _hasCaptured = true;
            
            // 初始化 Graph
            _graph = PlayableGraph.Create("SkillEditorSystem");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual); // 手动驱动
            
            var animator = _target.GetComponent<Animator>();
            if (animator != null)
            {
                var output = AnimationPlayableOutput.Create(_graph, "AnimOut", animator);
                _mixer = AnimationMixerPlayable.Create(_graph, 1, true);
                output.SetSourcePlayable(_mixer);
            }
            _graph.Play(); // 激活图
        }
    }

    public void Cleanup()
    {
        if (_graph.IsValid()) _graph.Destroy();
        
        foreach (var handler in _handlers.Values)
        {
            handler.OnDestroy();
        }
        
        if (_target != null && _hasCaptured)
        {
            _target.transform.position = _originPos;
            _target.transform.rotation = _originRot;
            
            // D. 重置动画姿态 (防止卡在半空中)
            var animator = _target.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Rebind(); // 重置为 T-Pose/Bind Pose
                animator.Update(0f);
            }
        }
    }

    public void Sample(SkillTimelineConfig skill, float time)
    {
        if (_target == null || skill == null) return;
        if (!_graph.IsValid()) BindTarget(_target);

        // 1. 重置 Transform
        if (_hasCaptured)
        {
            _target.transform.position = _originPos;
            _target.transform.rotation = _originRot;
        }

        // 2. 处理 Phases (主要是位移)
        foreach (var phase in skill.ClientPhases)
        {
            if (_handlers.TryGetValue(phase.GetType(), out var handler))
            {
                if (time < phase.StartTime) continue;

                float localTime = time - phase.StartTime;
                
                handler.OnSeek(_target, phase, localTime, _graph);
            }
        }
        
        SkillEvent lastAnimEvent = null;

        foreach (var evt in skill.ClientEvents)
        {
            // --- A. 动画特殊逻辑 ---
            // 动画是“互斥”的，我们只需要播放当前时刻之前【最近的一个】，
            // 所以这里只记录引用，不立即执行 Handler
            if (evt is AnimationEvent)
            {
                if (evt.Time <= time)
                {
                    if (lastAnimEvent == null || evt.Time > lastAnimEvent.Time)
                        lastAnimEvent = evt;
                }
                continue; 
            }
            
            if (_handlers.TryGetValue(evt.GetType(), out var handler))
            {
                if (time >= evt.Time)
                {
                    float localTime = time - evt.Time;
                    handler.OnSeek(_target, evt, localTime, _graph);
                }
            }
        }

        if (lastAnimEvent != null && _handlers.TryGetValue(lastAnimEvent.GetType(), out var animHandler))
        {
            float localTime = time - lastAnimEvent.Time;
            animHandler.OnSeek(_target, lastAnimEvent, localTime, _graph);
        }
        else
        {
            // 没有动画时，断开 Input 防止残留
            if (_mixer.IsValid()) _mixer.DisconnectInput(0);
        }

        // 4. 驱动 Graph 更新 Pose
        if (_graph.IsValid()) _graph.Evaluate();
    }
}
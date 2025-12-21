using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

[SkillHandler(typeof(EffectEvent))]
public class EffectPreviewHandler : SkillPreviewHandler
{
    // 缓存：Key是事件对象的引用，Value是场景里生成的特效物体
    private Dictionary<EffectEvent, GameObject> _spawnedEffects = new Dictionary<EffectEvent, GameObject>();

    public override void OnSeek(GameObject target, object data, float localTime, PlayableGraph graph)
    {
        Debug.Log("OnSeek called");
        var evt = data as EffectEvent;
        if (evt == null || string.IsNullOrEmpty(evt.Effect)) return;

        // 1. 判断时间是否在特效的生命周期内
        // localTime 是相对于 Event.Time 的时间
        bool isInsideDuration = localTime >= 0 && localTime <= evt.Duration;

        if (isInsideDuration)
        {
            // --- A. 如果在时间内，确保特效存在并更新 ---
            
            GameObject effectInstance;
            if (!_spawnedEffects.TryGetValue(evt, out effectInstance) || effectInstance == null)
            {
                // 实例化特效
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Effect/" + evt.Effect + ".prefab");
                if (prefab == null) return;

                effectInstance = Object.Instantiate(prefab);
                // 标记为不用保存到场景，防止把预览特效存进 Scene 文件
                effectInstance.hideFlags = HideFlags.DontSave; 
                _spawnedEffects[evt] = effectInstance;
            }

            // 更新位置
            if (evt.FollowTarget)
            {
                //跟随：设为子物体或者每帧同步坐标
                //为了Simulate稳定，通常建议每帧同步坐标而不是设为子物体
                effectInstance.transform.position = target.transform.TransformPoint(evt.PositionOffset);
                effectInstance.transform.rotation = target.transform.rotation * evt.RotationOffset;
            }
            else
            {
                effectInstance.transform.position = target.transform.position + evt.PositionOffset;
            }
            
            var particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Simulate(localTime, true, true); 
            }
        }
        else
        {
            if (_spawnedEffects.TryGetValue(evt, out var instance))
            {
                if (instance != null) Object.DestroyImmediate(instance);
                _spawnedEffects.Remove(evt);
            }
        }
    }

    public override void OnSceneGUI(GameObject target, object data)
    {
        var evt = data as EffectEvent;
        if(evt == null || target == null) return;
        Vector3 worldPos = target.transform.TransformPoint(evt.PositionOffset);
        Quaternion worldRot = target.transform.rotation * evt.RotationOffset;
        
        if (IsQuaternionInvalid(worldRot))
        {
            worldRot = Quaternion.identity;
        }
        
        Quaternion handleRot = (Tools.pivotRotation == PivotRotation.Local) ? worldRot : Quaternion.identity;
        
        EditorGUI.BeginChangeCheck();
        Handles.Label(worldPos, $"VFX: {evt.Effect}");
        
        Handles.color = Color.yellow;
        Handles.DrawDottedLine(target.transform.position, worldPos, 5f);
        
        Vector3 newWorldPos = Handles.PositionHandle(worldPos, handleRot);
        Quaternion newWorldRot = Handles.RotationHandle(handleRot, worldPos);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Edit Skill VFX"); // 标记撤销

            // 写回数据：位置
            evt.PositionOffset = target.transform.InverseTransformPoint(newWorldPos);

            // 写回数据：旋转
            // 计算局部旋转：父物体旋转的逆 * 新的世界旋转
            Quaternion localRot = Quaternion.Inverse(target.transform.rotation) * newWorldRot;
            
            // 防止万一算出来是无效的
            if (IsQuaternionInvalid(localRot)) localRot = Quaternion.identity;
            
            evt.RotationOffset = localRot;
        }
    }

    // 3. 实现清理接口：切换技能或关闭窗口时，清空所有特效
    public override void OnDestroy()
    {
        foreach (var kvp in _spawnedEffects)
        {
            if (kvp.Value != null)
            {
                Object.DestroyImmediate(kvp.Value);
            }
        }
        _spawnedEffects.Clear();
    }
    
    private bool IsQuaternionInvalid(Quaternion q)
    {
        return Mathf.Abs(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w) < 0.0001f;
    }
}
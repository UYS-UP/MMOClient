using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[SkillHandler(typeof(AnimationEvent))]
public class AnimationPreviewHandler : SkillPreviewHandler
{
    private AnimationClipPlayable _clipPlayable;
    private string _lastClipName;

    public override void OnSeek(GameObject target, object data, float localTime, PlayableGraph graph)
    {
        var evt = data as AnimationEvent;
        if (evt == null || string.IsNullOrEmpty(evt.Animation)) return;
        
        var output = (AnimationPlayableOutput)graph.GetOutput(0);
        var mixer = (AnimationMixerPlayable)output.GetSourcePlayable();

        // 1. 检查是否需要切换 Clip
        if (_lastClipName != evt.Animation || !_clipPlayable.IsValid())
        {
            string path = $"Assets/ArtRes/Animations/{evt.Animation}.anim"; // 你的路径
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip != null)
            {
                if (_clipPlayable.IsValid()) _clipPlayable.Destroy();
                
                _clipPlayable = AnimationClipPlayable.Create(graph, clip);
                
                // 连接到 Mixer 的 Input 0
                graph.Connect(_clipPlayable, 0, mixer, 0);
                mixer.SetInputWeight(0, 1f);
                
                _lastClipName = evt.Animation;
            }
        }

        // 2. 设置时间
        if (_clipPlayable.IsValid())
        {
            _clipPlayable.SetTime(localTime);
        }
    }
}
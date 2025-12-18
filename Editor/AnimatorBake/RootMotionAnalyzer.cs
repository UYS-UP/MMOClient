using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class RootMotionAnalyzer
{
    public struct BakeResult
    {
        public float MoveStartTime;
        public float MoveEndTime;
        public float TotalDistance;
        public AnimationCurve MotionCurve;
    }

    /// <summary>
    /// 静态方法：输入动画Clip，输出位移数据
    /// </summary>
    public static BakeResult Bake(GameObject target, AnimationClip clip, int sampleRate = 60, float velocityThreshold = 0.02f)
    {
        if (target == null || clip == null) return new BakeResult();

        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Preview Object 必须有 Animator 组件！");
            return new BakeResult();
        }

        // --- 1. 备份状态 (以免把场景里的角色弄乱) ---
        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
        bool originalApplyRoot = animator.applyRootMotion;

        // --- 2. 创建临时 Controller ---
        // 必须给 Animator 一个临时的 Controller 才能让它只播放我们指定的那个动作
        string tempPath = "Assets/Temp_Baker_Controller.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(tempPath);
        var state = controller.layers[0].stateMachine.AddState("Bake");
        state.motion = clip;

        try
        {
            // --- 3. 设置环境 ---
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true; 
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            target.transform.position = Vector3.zero;
            target.transform.rotation = Quaternion.identity;
            target.transform.localScale = Vector3.one; // 排除缩放影响

            // --- 4. 模拟循环 ---
            int totalFrames = Mathf.CeilToInt(clip.length * sampleRate);
            float dt = 1.0f / sampleRate;
            
            float[] dists = new float[totalFrames + 1];
            float[] vels = new float[totalFrames + 1];

            animator.Play("Bake", 0, 0f);
            animator.Update(0f);

            for (int i = 0; i < totalFrames; i++)
            {
                animator.Update(dt); // 手动驱动一帧

                // 获取 Root Motion 增量 (Z轴)
                float delta = animator.deltaPosition.z; 
                if (Mathf.Abs(delta) < 0.00001f) delta = 0;
                dists[i + 1] = dists[i] + delta;
                vels[i] = delta / dt;
            }

            // --- 5. 掐头去尾 & 生成曲线 ---
            float threshold = 0.1f; // 速度阈值，根据需要调整
            int startIndex = 0;
            int endIndex = totalFrames;

            for (int i = 0; i < totalFrames; i++) { if (vels[i] > threshold) { startIndex = i; break; } }
            for (int i = totalFrames - 1; i >= 0; i--) { if (vels[i] > threshold) { endIndex = i + 1; break; } }
            if (endIndex <= startIndex) endIndex = startIndex + 1;

            float totalDist = dists[endIndex] - dists[startIndex];
            AnimationCurve curve = new AnimationCurve();
            
            for (int i = startIndex; i <= endIndex; i++)
            {
                float t = (float)(i - startIndex) / (endIndex - startIndex);
                float v = (totalDist > 0.001f) ? (dists[i] - dists[startIndex]) / totalDist : 0;
                curve.AddKey(new Keyframe(t, v, 0, 0));
            }

            return new BakeResult
            {
                TotalDistance = totalDist,
                MotionCurve = curve,
                MoveStartTime = (float)startIndex / sampleRate,
                MoveEndTime = (float)endIndex / sampleRate
            };
        }
        finally
        {
            // --- 6. 还原状态 (非常重要) ---
            target.transform.position = originalPos;
            target.transform.rotation = originalRot;
            animator.runtimeAnimatorController = originalController;
            animator.applyRootMotion = originalApplyRoot;

            // 删除临时文件
            AssetDatabase.DeleteAsset(tempPath);
        }
    }
}
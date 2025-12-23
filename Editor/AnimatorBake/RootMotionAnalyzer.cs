using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public static class RootMotionAnalyzer
{
    public struct BakeResult
    {
        public bool IsValid;            // 是否烘焙成功
        public float MoveStartTime;     // 移动开始时间（秒）
        public float MoveEndTime;       // 移动结束时间（秒）
        public float Duration;          // 移动持续时间
        public float TotalDistance;     // 总位移长度 (标量)
        public Vector3 TotalOffset;     // 总位移向量 (终点 - 起点)
        public Vector3 MoveDirection;   // 归一化的移动方向
        public float TotalYawChange;    // 总旋转角度 (度)
        public AnimationCurve MotionCurve; // 归一化位移曲线 (0~1)
    }

    /// <summary>
    /// 静态方法：输入动画Clip，输出详细的位移矢量数据
    /// </summary>
    /// <param name="velocityThreshold">速度阈值，低于此速度视为静止 (米/秒)</param>
    public static BakeResult Bake(GameObject target, AnimationClip clip, int sampleRate = 60, float velocityThreshold = 0.1f)
    {
        if (target == null || clip == null) return new BakeResult { IsValid = false };

        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Preview Object 必须有 Animator 组件！");
            return new BakeResult { IsValid = false };
        }

        // --- 1. 备份场景对象状态 ---
        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
        bool originalApplyRoot = animator.applyRootMotion;
        AnimatorCullingMode originalCulling = animator.cullingMode;

        // --- 2. 创建临时 Controller ---
        string tempPath = "Assets/Temp_Baker_Controller.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(tempPath);
        var state = controller.layers[0].stateMachine.AddState("Bake");
        state.motion = clip;

        try
        {
            // --- 3. 设置模拟环境 ---
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true; 
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // 归零：确保从 (0,0,0) 和无旋转开始模拟
            target.transform.position = Vector3.zero;
            target.transform.rotation = Quaternion.identity;
            
            // 重要：强制更新一帧以初始化 Animator 内部状态
            animator.Play("Bake", 0, 0f);
            animator.Update(0f);

            // --- 4. 模拟循环 ---
            int totalFrames = Mathf.CeilToInt(clip.length * sampleRate);
            float dt = 1.0f / sampleRate;

            // 记录每一帧的累积位移和旋转
            List<Vector3> trajectory = new List<Vector3>(totalFrames + 1);
            List<float> speeds = new List<float>(totalFrames + 1);
            
            // 虚拟坐标和旋转，用于手动累加 Delta
            Vector3 virtualPos = Vector3.zero;
            Quaternion virtualRot = Quaternion.identity;
            float totalYaw = 0f;

            trajectory.Add(virtualPos);
            speeds.Add(0f);

            for (int i = 0; i < totalFrames; i++)
            {
                animator.Update(dt);

                Vector3 dp = animator.deltaPosition;
                Quaternion dr = animator.deltaRotation;

                // 核心：模拟物理引擎应用 RootMotion
                // 注意：animator.deltaPosition 已经是基于当前朝向计算出的世界坐标增量了（在Animator内部逻辑中）
                // 但为了严谨，我们直接累加 Unity 计算出的 deltaPosition
                virtualPos += dp;
                virtualRot *= dr;
                
                // 累加 Y 轴旋转变化
                totalYaw += dr.eulerAngles.y;

                trajectory.Add(virtualPos);

                // 计算瞬时速度 (用于掐头去尾)
                float speed = dp.magnitude / dt;
                speeds.Add(speed);
            }

            // --- 5. 掐头去尾 (基于速度向量模长) ---
            int startIndex = 0;
            int endIndex = totalFrames;
            bool foundStart = false;

            // 寻找开始点
            for (int i = 0; i < speeds.Count; i++) 
            { 
                if (speeds[i] > velocityThreshold) 
                { 
                    startIndex = i; 
                    foundStart = true;
                    break; 
                } 
            }

            // 寻找结束点
            for (int i = speeds.Count - 1; i >= 0; i--) 
            { 
                if (speeds[i] > velocityThreshold) 
                { 
                    endIndex = i; 
                    break; 
                } 
            }
            
            // 修正索引边界
            if (!foundStart) { startIndex = 0; endIndex = 0; } // 全程没动
            if (endIndex < startIndex) endIndex = startIndex; 
            if (endIndex >= trajectory.Count) endIndex = trajectory.Count - 1;

            // --- 6. 计算最终结果 ---
            
            // 获取有效区间的起点和终点坐标
            Vector3 startPos = trajectory[startIndex];
            Vector3 endPos = trajectory[endIndex];
            
            // 计算总位移向量
            Vector3 totalOffset = endPos - startPos;
            float totalDistance = totalOffset.magnitude; // 这是一个直线距离
            
            // 如果需要沿路径的累积距离（曲线长度），需要重新积分，这里通常用直线位移或投影位移即可。
            // 考虑到技能配置通常需要的是“位移了多少距离”，我们这里计算沿路径的距离会更精准用于 Sample Curve
            float pathDistance = 0f;
            float[] distSteps = new float[endIndex - startIndex + 1];
            distSteps[0] = 0f;
            
            for (int i = startIndex; i < endIndex; i++)
            {
                float step = Vector3.Distance(trajectory[i], trajectory[i+1]);
                pathDistance += step;
                distSteps[i - startIndex + 1] = pathDistance;
            }

            // 生成曲线 (X轴: 时间比率 0~1, Y轴: 位移比率 0~1)
            AnimationCurve curve = new AnimationCurve();
            int steps = endIndex - startIndex;
            if (steps > 0 && pathDistance > 0.001f)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float timeRatio = (float)i / steps;
                    float distRatio = distSteps[i] / pathDistance;
                    curve.AddKey(new Keyframe(timeRatio, distRatio, 0, 0)); // 线性切线，或根据需要调整
                }
            }
            else
            {
                // 没有位移
                curve.AddKey(0, 0);
                curve.AddKey(1, 1);
            }

            return new BakeResult
            {
                IsValid = true,
                MoveStartTime = (float)startIndex / sampleRate,
                MoveEndTime = (float)endIndex / sampleRate,
                Duration = (float)(endIndex - startIndex) / sampleRate,
                TotalDistance = pathDistance, // 使用沿路径的累积距离
                TotalOffset = totalOffset,    // 实际空间位移向量
                MoveDirection = totalDistance > 0.001f ? totalOffset.normalized : Vector3.forward,
                TotalYawChange = totalYaw,
                MotionCurve = curve
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Bake failed: {e.Message}");
            return new BakeResult { IsValid = false };
        }
        finally
        {
            // --- 7. 还原 ---
            target.transform.position = originalPos;
            target.transform.rotation = originalRot;
            if(animator != null)
            {
                animator.runtimeAnimatorController = originalController;
                animator.cullingMode = originalCulling;
            }
            AssetDatabase.DeleteAsset(tempPath);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

public static class HelperUtility
{
    /// <summary>
    /// 递归查找指定类型的组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="transform">起始Transform</param>
    /// <param name="includeInactive">是否包含非激活对象</param>
    /// <returns>找到的组件，未找到则返回null</returns>
    public static T FindComponentInChildrenRecursive<T>(this Transform transform, bool includeInactive = false) where T : Component
    {
        // 先在当前对象查找
        T component = transform.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        // 递归查找子对象
        foreach (Transform child in transform)
        {
            if (!includeInactive && !child.gameObject.activeSelf)
                continue;

            component = child.FindComponentInChildrenRecursive<T>(includeInactive);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>
    /// 递归查找所有指定类型的组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="transform">起始Transform</param>
    /// <param name="includeInactive">是否包含非激活对象</param>
    /// <returns>找到的所有组件列表</returns>
    public static List<T> FindComponentsInChildrenRecursive<T>(this Transform transform, bool includeInactive = false) where T : Component
    {
        List<T> components = new List<T>();
        FindComponentsInChildrenRecursiveInternal(transform, includeInactive, components);
        return components;
    }

    private static void FindComponentsInChildrenRecursiveInternal<T>(Transform transform, bool includeInactive, List<T> components) where T : Component
    {
        // 添加当前对象的组件
        T[] currentComponents = transform.GetComponents<T>();
        foreach (T component in currentComponents)
        {
            components.Add(component);
        }

        // 递归查找子对象
        foreach (Transform child in transform)
        {
            if (!includeInactive && !child.gameObject.activeSelf)
                continue;

            FindComponentsInChildrenRecursiveInternal(child, includeInactive, components);
        }
    }
    
    public static Vector3 YawToForward(float yaw)
    {
        float x = MathF.Sin(yaw);
        float z = MathF.Cos(yaw);
        return new Vector3(x, 0f, z);
    }
    
    public static Vector3 FloatArrayToVector3(float[] value)
    {
        return new Vector3(value[0], value[1], value[2]);
    }
    
    public static byte[] MessageSerializer(object data)
    {
        return MessagePackSerializer.Serialize(data);
    }
    
    public static short[] Vector3ToShortArray(Vector3 vec)
    {
        return new short[] { (short)(vec.x * 100), (short)(vec.y * 100), (short)(vec.z * 100) };
    }

    public static Vector3 ShortArrayToVector3(short[] arr)
    {
        return new Vector3(arr[0] / 100f, arr[1] / 100f, arr[2] / 100f);
    }

    public static sbyte[] Vector3ToSbyteArray(Vector3 vec)
    {
        return new sbyte[] { (sbyte)(vec.x * 127), (sbyte)(vec.y * 127), (sbyte)(vec.z * 127) };
    }

    public static Vector3 SbyteArrayToVector3(sbyte[] arr)
    {
        return new Vector3(arr[0] / 127f, arr[1] / 127f, arr[2] / 127f);
    }

    public static short YawToShort(float yaw)
    {
        return (short)(Mathf.Repeat(yaw, 360) * 10);
    }

    public static float ShortToYaw(short yaw)
    {
        return yaw / 10f;
    }
}

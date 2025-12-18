// Assets/Editor/AutoBinderHierarchyIcon.cs
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoBinderHierarchyIcon
{
    private static Texture2D star;
    private static Texture2D warn;

    static AutoBinderHierarchyIcon()
    {
        star = EditorGUIUtility.IconContent("d_Favorite Icon").image as Texture2D;
        warn = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
        EditorApplication.hierarchyWindowItemOnGUI += Draw;
    }

    private static void Draw(int instanceID, Rect rect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (!go) return;

        var binder = go.GetComponent<AutoBinder>();
        bool isRoot = binder != null;

        // 查找是否有引用此对象
        bool isBound = false;
        AutoBinder rootBinder = null;
        if (!isRoot)
        {
            rootBinder = go.GetComponentInParent<AutoBinder>();
            if (rootBinder && rootBinder.references != null)
            {
                isBound = System.Array.Exists(rootBinder.references, r =>
                    r != null && (r as Component)?.gameObject == go);
            }
        }

        if (!isRoot && !isBound) return;

        Rect r = new Rect(rect.x + rect.width - 20, rect.y, 16, 16);

        if (isRoot && binder.hasMissingReferences)
        {
            GUI.color = Color.red;
            GUI.DrawTexture(r, warn);
            GUI.color = Color.white;
        }
        else
        {
            GUI.DrawTexture(r, star);
        }
    }
}
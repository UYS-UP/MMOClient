// Assets/Editor/AutoBinderMenu.cs
using UnityEditor;
using UnityEngine;

public class AutoBinderMenu
{
    [MenuItem("GameObject/UI/Auto Binder (Add Component)", false, 0)]
    static void AddAutoBinder()
    {
        var go = Selection.activeGameObject;
        if (go != null)
        {
            Undo.AddComponent<AutoBinder>(go);
        }
    }

    [MenuItem("GameObject/UI/Auto Binder (Add Component)", true)]
    static bool ValidateAdd()
    {
        return Selection.activeGameObject != null;
    }
}
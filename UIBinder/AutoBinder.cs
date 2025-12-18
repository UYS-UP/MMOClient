// Assets/Scripts/AutoBinder.cs
using UnityEngine;

public class AutoBinder : MonoBehaviour
{
    [HideInInspector] public bool isBound = false;

#if UNITY_EDITOR
    [SerializeField, HideInInspector] public string[] fieldNames = new string[0];
    [SerializeField, HideInInspector] public Object[] references = new Object[0];
    [HideInInspector] public bool hasMissingReferences = false;
    
    public AutoBinder()
    {
        fieldNames = new string[0];
        references = new Object[0];
    }
#endif
}
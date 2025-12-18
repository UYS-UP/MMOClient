#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;

[CustomEditor(typeof(AutoBinder))]
public class AutoBinderEditor : Editor
{
    private SerializedProperty referencesProp;
    private SerializedProperty fieldNamesProp;

    private ReorderableList list;
    private AutoBinder binder;

    // 拖拽添加区域高度
    private const float DropAreaHeight = 60f;

    private void OnEnable()
    {
        referencesProp = serializedObject.FindProperty("references");
        fieldNamesProp = serializedObject.FindProperty("fieldNames");
        SetupList();
    }

    private void SetupList()
    {
        list = new ReorderableList(serializedObject, referencesProp, true, true, true, true);

        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Auto Binder - 引用列表（名称 ←→ 组件）");
        };

        // “＋”按钮：手动新增空行
        list.onAddCallback = l =>
        {
            int index = l.serializedProperty.arraySize;
            l.serializedProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

            SyncArrayLengths();

            if (fieldNamesProp.arraySize > index)
                fieldNamesProp.GetArrayElementAtIndex(index).stringValue = MakeUniqueName($"field{index}");

            serializedObject.ApplyModifiedProperties();
        };

        list.onRemoveCallback = l =>
        {
            if (!EditorUtility.DisplayDialog("移除绑定", "确定要移除当前选中的绑定吗？", "确定", "取消"))
                return;

            int index = Mathf.Clamp(l.index, 0, referencesProp.arraySize - 1);
            if (index < 0) return;

            referencesProp.DeleteArrayElementAtIndex(index);
            if (index < fieldNamesProp.arraySize)
                fieldNamesProp.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
        };

#if UNITY_2019_3_OR_NEWER
        // 重排时同步字段名数组
        list.onReorderCallbackWithDetails = (l, oldIndex, newIndex) =>
        {
            serializedObject.ApplyModifiedProperties();
            MoveArrayElementSafe(fieldNamesProp, oldIndex, newIndex);
            serializedObject.ApplyModifiedProperties();
        };
#endif

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            rect.y += 2f;
            float lineH = EditorGUIUtility.singleLineHeight;

            var nameProp = fieldNamesProp.GetArrayElementAtIndex(index);
            var objProp  = referencesProp.GetArrayElementAtIndex(index);

            // 左：字段名
            float nameWidth = Mathf.Min(180f, rect.width * 0.35f);
            var nameRect = new Rect(rect.x, rect.y, nameWidth, lineH);
            nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.stringValue);

            // 中：ObjectField（接受 Component；若拖入 GO，先用其 Transform）
            float objWidth = Mathf.Min(260f, rect.width * 0.40f);
            var objRect = new Rect(nameRect.xMax + 6f, rect.y, objWidth, lineH);
            Object current = objProp.objectReferenceValue;
            Object newObj = EditorGUI.ObjectField(objRect, current, typeof(Object), true);

            if (newObj is GameObject goFromRow)
            {
                newObj = goFromRow.transform; // 先临时用 Transform，随后可通过右侧下拉切换具体组件
            }
            if (newObj != current)
            {
                objProp.objectReferenceValue = newObj as Component;
            }

            // 右：下拉框（基于当前行的 GO，枚举其所有组件供选择）
            var popupRect = new Rect(objRect.xMax + 6f, rect.y, rect.width - (objRect.xMax + 6f - rect.x), lineH);
            DrawComponentPopupForRow(popupRect, objProp);
        };

        list.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
    }

    public override void OnInspectorGUI()
    {
        // —— 先更新再读写 ——
        serializedObject.Update();
        binder = (AutoBinder)target;

        // 防御式同步长度
        SyncArrayLengths();

        EditorGUILayout.Space(4);
        list.DoLayoutList();

        EditorGUILayout.Space(6);
        // —— 新增专用拖拽区域（只有拖到这里才会新增） ——
        var dropRect = GUILayoutUtility.GetRect(0, DropAreaHeight, GUILayout.ExpandWidth(true));
        DrawAddDropArea(dropRect);

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("复制绑定代码"))
            {
                EditorGUIUtility.systemCopyBuffer = GenerateBindingCode();
                Debug.Log("已复制生成的绑定代码到剪贴板");
            }

            if (GUILayout.Button("清理空引用"))
            {
                ClearMissing();
                Debug.Log("已清理所有空引用行");
            }
        }

        // —— 写回序列化数据 ——
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 绘制“仅在此区域内才新增”的拖拽区域；支持一次拖多个对象
    /// </summary>
    private void DrawAddDropArea(Rect dropRect)
    {
        var style = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic
        };
        GUI.Box(dropRect, "拖入此区域新增绑定", style);

        var evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                int added = 0;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is Component c)
                    {
                        AddReference(c);
                        added++;
                    }
                    else if (obj is GameObject go)
                    {
                        Component comp = go.transform;
                        System.Type[] componentTypes = new System.Type[]
                        {
                            typeof(TextMeshProUGUI),
                            typeof(Button),
                            typeof(TMP_InputField),
                            typeof(Toggle),
                            typeof(Slider),
                            typeof(ScrollRect),
                            typeof(Image),
                            typeof(RawImage),
                            typeof(TMP_Dropdown),
                            typeof(Dropdown),
                            typeof(Scrollbar),
                            typeof(Canvas),
                            typeof(CanvasGroup),
                            typeof(RectTransform),
                            typeof(VerticalLayoutGroup),
                            typeof(HorizontalLayoutGroup),
                            typeof(GridLayoutGroup),
                            typeof(ContentSizeFitter),
                            typeof(AspectRatioFitter),
                            typeof(LayoutGroup)
                        };

                        foreach (var type in componentTypes)
                        {
                            Component component = go.GetComponent(type);
                            if (component != null)
                            {
                                comp = component;
                                break;
                            }
                        }
                        AddReference(comp);
                        added++;
                    }
                }

                if (added > 0)
                {
                    serializedObject.ApplyModifiedProperties();
                    GUI.FocusControl(null);
                    Debug.Log($"已新增 {added} 个绑定");
                }
            }

            evt.Use();
        }
    }

    /// <summary>
    /// 在行内绘制基于当前引用 GameObject 的组件下拉框；切换时替换为所选组件
    /// </summary>
    private void DrawComponentPopupForRow(Rect rect, SerializedProperty objProp)
    {
        using (new EditorGUI.DisabledScope(objProp.objectReferenceValue == null))
        {
            var currentComp = objProp.objectReferenceValue as Component;
            GameObject go = currentComp != null ? currentComp.gameObject : null;

            string[] options;
            int currentIndex = -1;

            if (go == null)
            {
                options = new[] { "-- 无对象可选 --" };
                currentIndex = 0;
                EditorGUI.Popup(rect, currentIndex, options);
                return;
            }

            var comps = GetEligibleComponents(go);
            options = new string[comps.Count];
            for (int i = 0; i < comps.Count; i++)
            {
                options[i] = MakeComponentLabel(comps[i]);
                if (comps[i] == currentComp)
                    currentIndex = i;
            }
            if (currentIndex < 0) currentIndex = 0; // 若当前组件不在列表（极少见），默认选第一个

            int newIndex = EditorGUI.Popup(rect, currentIndex, options);
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < comps.Count)
            {
                objProp.objectReferenceValue = comps[newIndex];
                // 不在这里 Apply，等外层 OnInspectorGUI 统一 Apply
            }
        }
    }

    /// <summary>
    /// 列出一个 GameObject 上“可选择”的组件列表（默认包含 Transform）
    /// </summary>
    private static List<Component> GetEligibleComponents(GameObject go)
    {
        var list = new List<Component>();
        if (go == null) return list;

        if (go.transform != null) list.Add(go.transform);

        var all = go.GetComponents<Component>();
        foreach (var c in all)
        {
            if (c == null) continue; // 处理缺失脚本
            if (c is Transform) continue; // 已加入
            list.Add(c);
        }

        return list;
    }

    private static string MakeComponentLabel(Component comp)
    {
        if (comp == null) return "Null";
        string typeName = comp.GetType().Name;
        return $"{typeName}";
    }

    /// <summary>
    /// 新增一条（用于拖拽或行内替换后）
    /// </summary>
    private void AddReference(Component comp)
    {
        if (comp == null) return;
        
        int index = referencesProp.arraySize;
        referencesProp.arraySize++;
        serializedObject.ApplyModifiedProperties();
        
        SyncArrayLengths();
        
        referencesProp.GetArrayElementAtIndex(index).objectReferenceValue = comp;
        string fieldName = comp.gameObject.name;
        switch (comp)
        {
            case RectTransform:
                fieldName += "Rect";
                break;
            case Transform:
                fieldName += "Trans";
                break;
        }
        string baseName = SafeFieldName(fieldName);
        string unique = MakeUniqueName(string.IsNullOrEmpty(baseName) ? $"field{index}" : baseName);
        fieldNamesProp.GetArrayElementAtIndex(index).stringValue = unique;
    }

    /// <summary>
    /// 将任意字符串清洗为较安全的字段名
    /// </summary>
    private static string SafeFieldName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "field";
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            else sb.Append('_');
        }
        if (sb.Length > 0 && char.IsDigit(sb[0])) sb.Insert(0, '_');
        return sb.ToString();
    }

    /// <summary>
    /// 生成唯一名称（name, name_1, name_2...）
    /// </summary>
    private string MakeUniqueName(string baseName)
    {
        var used = new HashSet<string>();
        for (int i = 0; i < fieldNamesProp.arraySize; i++)
        {
            used.Add(fieldNamesProp.GetArrayElementAtIndex(i).stringValue);
        }

        string name = baseName;
        int suffix = 1;
        while (used.Contains(name))
        {
            name = $"{baseName}_{suffix++}";
        }
        return name;
    }


    private void SyncArrayLengths()
    {
        if (referencesProp == null || fieldNamesProp == null) return;

        int refs = referencesProp.arraySize;
        int names = fieldNamesProp.arraySize;

        if (names < refs)
        {
            while (fieldNamesProp.arraySize < refs)
            {
                int i = fieldNamesProp.arraySize;
                fieldNamesProp.arraySize++;
                fieldNamesProp.GetArrayElementAtIndex(i).stringValue = MakeUniqueName($"field{i}");
            }
        }
        else if (names > refs)
        {
            while (fieldNamesProp.arraySize > refs)
            {
                fieldNamesProp.DeleteArrayElementAtIndex(fieldNamesProp.arraySize - 1);
            }
        }
    }

    private static void MoveArrayElementSafe(SerializedProperty array, int oldIndex, int newIndex)
    {
        if (array == null || !array.isArray) return;
        if (oldIndex == newIndex) return;
        if (oldIndex < 0 || oldIndex >= array.arraySize) return;
        if (newIndex < 0 || newIndex >= array.arraySize) return;
        array.MoveArrayElement(oldIndex, newIndex);
    }

    private void ClearMissing()
    {
        for (int i = referencesProp.arraySize - 1; i >= 0; --i)
        {
            var elem = referencesProp.GetArrayElementAtIndex(i);
            if (elem.objectReferenceValue == null)
            {
                referencesProp.DeleteArrayElementAtIndex(i);
                if (i < fieldNamesProp.arraySize)
                    fieldNamesProp.DeleteArrayElementAtIndex(i);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
    
    private string GenerateBindingCode()
    {
        var sb = new StringBuilder();
        var root = ((AutoBinder)target).transform;
        
        sb.AppendLine();
        
        // 生成字段声明
        for (int i = 0; i < referencesProp.arraySize; i++)
        {
            var comp = referencesProp.GetArrayElementAtIndex(i).objectReferenceValue as Component;
            var name = GetValidFieldName(i);
            
            if (comp == null) continue;
            
            string typeName = comp.GetType().FullName;
            sb.AppendLine($"private {typeName} {name};");
        }

        sb.AppendLine();
        sb.AppendLine("private void BindComponent()");
        sb.AppendLine("{");
        sb.AppendLine("    var root = this.transform;");
        sb.AppendLine();

        for (int i = 0; i < referencesProp.arraySize; i++)
        {
            var comp = referencesProp.GetArrayElementAtIndex(i).objectReferenceValue as Component;
            var name = GetValidFieldName(i);
            
            if (comp == null) continue;

            string typeName = comp.GetType().FullName;
            string relative = GetRelativePath(root, comp.transform);
            
            if (!string.IsNullOrEmpty(relative))
            {
                // 相对路径：在根节点下
                if (comp is Transform || comp is RectTransform)
                {
                    // Transform/RectTransform 直接查找
                    sb.AppendLine($"    {name} = root.Find(\"{relative}\"){(comp is RectTransform ? " as RectTransform" : "")};");
                }
                else
                {
                    // 其他组件需要 GetComponent
                    sb.AppendLine($"    {name} = root.Find(\"{relative}\")?.GetComponent<{typeName}>();");
                }
            }
            else
            {
                // 绝对路径：在整个场景中查找
                string absolute = GetAbsolutePath(comp.transform);
                if (comp is Transform || comp is RectTransform)
                {
                    sb.AppendLine($"    {name} = GameObject.Find(\"{absolute}\")?.transform{(comp is RectTransform ? " as RectTransform" : "")};");
                }
                else
                {
                    sb.AppendLine($"    {name} = GameObject.Find(\"{absolute}\")?.GetComponent<{typeName}>();");
                }
            }
            
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GetValidFieldName(int index)
    {
        var name = fieldNamesProp.GetArrayElementAtIndex(index).stringValue;
        if (string.IsNullOrEmpty(name)) 
            name = $"field{index}";
        return name;
    }
    
    
    private static string GetRelativePath(Transform root, Transform child)
    {
        if (root == null || child == null) return null;

        // 判断 child 是否在 root 子树里
        Transform t = child;
        var stack = new System.Collections.Generic.Stack<string>();
        while (t != null && t != root)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        if (t != root) return null; // 不是子孙

        // 组合路径
        if (stack.Count == 0) return string.Empty; // root 自己
        var sb = new StringBuilder();
        bool first = true;
        foreach (var seg in stack)
        {
            if (!first) sb.Append("/");
            first = false;
            sb.Append(seg);
        }
        return sb.ToString();
    }
    
    private static string GetAbsolutePath(Transform leaf)
    {
        if (leaf == null) return string.Empty;
        var stack = new System.Collections.Generic.Stack<string>();
        Transform t = leaf;
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        var sb = new StringBuilder();
        sb.Append("/");
        bool first = true;
        foreach (var seg in stack)
        {
            if (!first) sb.Append("/");
            first = false;
            sb.Append(seg);
        }
        return sb.ToString();
    }

}
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NpcInfoExport : EditorWindow
{
   private List<NpcInfo> npcInfos = new List<NpcInfo>();
   private Vector2 scrollPosition;
   private GameObject draggedObject;
   private int selectedIndex = -1;
   
   
   [MenuItem("Tools/NPC信息导出")]
   public static void ShowWindow()
   {
       GetWindow<NpcInfoExport>("NPC信息导出");
   }
   
   private void OnGUI()
   {
       GUILayout.Label("NPC信息导出工具", EditorStyles.boldLabel);

       // 拖拽区域
       Rect dragArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
       GUI.Box(dragArea, "将GameObject拖拽到这里");
        
       // 处理拖拽事件
       DragAndDropArea(dragArea);

       // 显示已添加的NPC信息列表
       DisplayNpcList();

       // 导出按钮
       if (GUILayout.Button("导出JSON") && npcInfos.Count > 0)
       {
           ExportToJson();
       }
   }
   
   private void DragAndDropArea(Rect area)
   {
       Event evt = Event.current;
        
       switch (evt.type)
       {
           case EventType.DragUpdated:
           case EventType.DragPerform:
               if (!area.Contains(evt.mousePosition))
                   return;

               DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

               if (evt.type == EventType.DragPerform)
               {
                   DragAndDrop.AcceptDrag();

                   foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                   {
                       if (obj is GameObject gameObject)
                       {
                           AddNpcInfo(gameObject);
                       }
                   }

                   evt.Use();
               }
               break;
       }
   }
   
   private void AddNpcInfo(GameObject gameObject)
   {
       Scene objectScene = gameObject.scene;
       string sceneName = objectScene.name;
       NpcInfo info = new NpcInfo
       {
           name = gameObject.name,
           templateId = "",
           pos = gameObject.transform.position,
           yaw = gameObject.transform.eulerAngles.y,
           regionId = SceneToRegionId(sceneName)
       };

       npcInfos.Add(info);
       selectedIndex = npcInfos.Count - 1;
   }

   private short SceneToRegionId(string sceneName)
   {
       short id = -1;
       switch (sceneName)
       {
           case "GameScene_0":
               id = 0;
               break;
       }
       return id;
   }
   
   private void DisplayNpcList()
   {
       if (npcInfos.Count == 0)
       {
           GUILayout.Label("暂无NPC信息");
           return;
       }

       scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
       for (int i = 0; i < npcInfos.Count; i++)
       {
           EditorGUILayout.BeginVertical("box");
            
           bool isSelected = i == selectedIndex;
           if (isSelected)
           {
               EditorGUILayout.BeginVertical(GUI.skin.box);
           }

           // 显示基本信息
           EditorGUILayout.LabelField($"NPC {i + 1}: {npcInfos[i].name}");
            
           if (GUILayout.Button("选择", GUILayout.Width(60)))
           {
               selectedIndex = i;
           }

           if (GUILayout.Button("删除", GUILayout.Width(60)))
           {
               npcInfos.RemoveAt(i);
               if (selectedIndex >= npcInfos.Count) selectedIndex = npcInfos.Count - 1;
               return;
           }

           // 如果当前项被选中，显示详细编辑界面
           if (isSelected)
           {
               EditorGUILayout.Space();
               EditNpcInfo(npcInfos[i]);
               EditorGUILayout.EndVertical();
           }

           EditorGUILayout.EndVertical();
           EditorGUILayout.Space();
       }
        
       EditorGUILayout.EndScrollView();
   }

   private void EditNpcInfo(NpcInfo info)
   {
       info.name = EditorGUILayout.TextField("名称", info.name);
       info.entityType = (EntityType)EditorGUILayout.EnumPopup("类型", info.entityType);
       info.templateId = EditorGUILayout.TextField("模板ID", info.templateId);
       info.pos = EditorGUILayout.Vector3Field("位置", info.pos);
       info.yaw = EditorGUILayout.FloatField("朝向", info.yaw);
       info.regionId = (short)EditorGUILayout.IntField("区域ID", info.regionId);
   }
   
   private void ExportToJson()
   {
       string json = JsonUtility.ToJson(new NpcInfoListWrapper { npcInfos = npcInfos }, true);
       string path = EditorUtility.SaveFilePanel("导出JSON文件", "", "npc_info.json", "json");
        
       if (!string.IsNullOrEmpty(path))
       {
           File.WriteAllText(path, json);
           EditorUtility.DisplayDialog("导出成功", $"已成功导出 {npcInfos.Count} 个NPC信息", "确定");
       }
   }
   
   [Serializable]
   private class NpcInfoListWrapper
   {
       public List<NpcInfo> npcInfos;
   }
}

[Serializable]
public class NpcInfo
{
    public string name;
    public string templateId;
    public EntityType entityType;
    public Vector3 pos;
    public float yaw;
    public short regionId;
}

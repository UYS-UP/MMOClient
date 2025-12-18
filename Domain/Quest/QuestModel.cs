using System;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;


[MessagePackObject]
public class QuestProgressUpdate
{
    [Key(0)] public string NodeId;
    [Key(1)] public bool IsCompleted;
    [Key(2)] public List<(int, QuestObjective)> Objectives;
}

[MessagePackObject]
public class ServerQuestProgressUpdate
{
    [Key(0)] public List<QuestProgressUpdate> QuestUpdates;
}

public class QuestModel : IDisposable
{
    public event Action<QuestNode> OnQuestAdded;
    public event Action<string, int, QuestObjective> OnQuestUpdated;
    public event Action<QuestNode> OnQuestCompleted;
    public event Action<QuestNode> OnQuestSubmitted;

    private readonly Dictionary<string, QuestNode> activeQuests = new();
    private readonly Dictionary<string, QuestNode> completedQuests = new();

 
    public QuestModel()
    {
        GameClient.Instance.RegisterHandler(Protocol.QuestAccept, QuestAcceptSync);
        GameClient.Instance.RegisterHandler(Protocol.QuestCompleted, QuestCompletedSync);
        GameClient.Instance.RegisterHandler(Protocol.QuestUpdated, QuestUpdatedSync);
        GameClient.Instance.RegisterHandler(Protocol.QuestListSync, QuestListSync);
    }

    private void OnDisable()
    {
        GameClient.Instance.UnregisterHandler(Protocol.QuestAccept);
        GameClient.Instance.UnregisterHandler(Protocol.QuestCompleted);
        GameClient.Instance.UnregisterHandler(Protocol.QuestUpdated);
    }

    public bool TryGetQuestNode(string questNodeId, out QuestNode questNode)
    {
        return activeQuests.TryGetValue(questNodeId, out questNode);
    }

    private void QuestListSync(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerQuestListSync>();
        activeQuests.Clear();
        foreach (var questNode in data.Quests)
        {
            activeQuests[questNode.NodeId] = questNode;
            OnQuestAdded?.Invoke(questNode);
        }
    }

    private void QuestAcceptSync(GamePacket packet)
    {
        var data = packet.DeSerializePayload<QuestNode>();
        activeQuests[data.NodeId] = data;
        OnQuestAdded?.Invoke(data);
    }
    
    private void QuestUpdatedSync(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerQuestProgressUpdate>();
        foreach (var questProgress in data.QuestUpdates)
        {
            if(!activeQuests.TryGetValue(questProgress.NodeId, out var questNode)) continue;
            foreach (var (index, objective) in questProgress.Objectives)
            {
                questNode.Objectives[index] = objective;
                OnQuestUpdated?.Invoke(questProgress.NodeId, index, objective);
            }

            if (questProgress.IsCompleted)
            {
                completedQuests[questProgress.NodeId] = questNode;
                activeQuests.Remove(questNode.NodeId);
                OnQuestCompleted?.Invoke(questNode);
            }
        }
    }
    
    private void QuestCompletedSync(GamePacket packet)
    {

       

    }
    
    public IEnumerable<QuestNode> GetAllQuests() => activeQuests.Values;
    
    public void Dispose()
    {
        
    }

    public void RequestAdvanceQuestNode(string currentNpcId, string optionAdvanceToQuestNode)
    {
        // 发给服务端推进任务
    }
}
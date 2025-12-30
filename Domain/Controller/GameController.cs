
using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameController : IDisposable
{
    private readonly EntityModel entityModel;
    private readonly SkillModel skillModel;
    private readonly StorageModel storageModel;
    private readonly QuestModel questModel;
    
    private readonly GameView gameView;

    public GameController(GameView gameView)
    {
        entityModel = GameContext.Instance.Get<EntityModel>();
        skillModel = GameContext.Instance.Get<SkillModel>();
        storageModel = GameContext.Instance.Get<StorageModel>();
        questModel = GameContext.Instance.Get<QuestModel>();
        this.gameView = gameView;
        RegisterEvents();
    }
    
    
    public void Dispose()
    {
        UnregisterEvents();
    }

    private void RegisterEvents()
    {
        entityModel.OnEntityCreated += OnEntityCreated;
        entityModel.OnEntityDestroyed += OnEntityDestroyed;
        entityModel.OnEntityHpUpdated += OnEntityHpUpdated;
        skillModel.OnSkillReleased += OnSkillReleased;
        questModel.OnQuestUpdated += OnQuestUpdated;
        questModel.OnQuestAdded += OnQuestAdded;
        questModel.OnQuestCompleted += OnQuestCompleted;
        
        GameClient.Instance.RegisterHandler(Protocol.SC_ApplyBuff, OnApplyBuff);

    }

    private void OnApplyBuff(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerApplyBuff>();
        gameView.ApplyBuff(data.BuffId, data.Duration);
    }

    private void UnregisterEvents()
    {
        entityModel.OnEntityCreated -= OnEntityCreated;
        entityModel.OnEntityDestroyed -= OnEntityDestroyed;
        entityModel.OnEntityHpUpdated -= OnEntityHpUpdated;
        skillModel.OnSkillReleased -= OnSkillReleased;
        questModel.OnQuestUpdated -= OnQuestUpdated;
        questModel.OnQuestAdded -= OnQuestAdded;
        questModel.OnQuestCompleted -= OnQuestCompleted;
    }
    
    public void Update()
    {

        if (Input.GetKeyDown(KeyCode.I))
        {
            UIService.Instance.ShowView<NavigationView>(view =>
            {
                var character = (NetworkCharacter)entityModel.LocalEntity.NetworkEntity;
                view.OpenProfile(
                    character.Name,
                    "暂无工会",
                    "暂无称号",
                    character.Level,
                    100,
                    character.Ex,
                    character.MaxEx
                );
            }, layer: UILayer.Popup);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            UIService.Instance.ShowView<NavigationView>(view =>
            {
                view.OpenInventory(storageModel.MaxSize);
            }, layer: UILayer.Popup);
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            UIService.Instance.ShowView<FriendView>();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            UIService.Instance.ShowView<DungeonView>();
            InputBindService.Instance.UIIsOpen = true;
        }
  
    }

    #region QuestModel Events

    private void OnQuestAdded(QuestNode node)
    {
        Debug.Log(node.Objectives.Count);
        gameView.AddQuest(node);
    }

    private void OnQuestUpdated(string nodeId, int index, QuestObjective objective)
    {
        gameView.UpdateQuest(nodeId, index, objective);
    }

    private void OnQuestCompleted(QuestNode node)
    {
        gameView.CompleteQuest(node);
    }

    #endregion


    #region EntityModel Events
    
    private void OnEntityCreated(int entityId)
    {
        if(!entityModel.TryGetEntity(entityId, out var entity)) return;
        if (!entityModel.IsLocalEntity(entityId)) return;
        var character = (NetworkCharacter)entity.NetworkEntity;
        gameView.UpdatePlayerExperience(character.Ex, character.MaxEx);
        gameView.UpdatePlayerHealth(character.Hp, character.MaxHp);
    }
    
    private void OnEntityDestroyed(int entityId)
    {
        
    }

    private void OnEntityHpUpdated(int entityId, float currentHp, float maxHp, EntityType type)
    {
        if(!entityModel.IsLocalEntity(entityId)) return;
        gameView.UpdatePlayerHealth( currentHp, maxHp);
        
    }
    #endregion
    
    
    #region SkillModel Events

    private void OnSkillReleased(int index)
    {
        gameView.ReleaseSkill(0);
    }

    #endregion
    
    


}

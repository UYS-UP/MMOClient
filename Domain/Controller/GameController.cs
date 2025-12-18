
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
        
        GameClient.Instance.RegisterHandler(Protocol.ApplyBuff, OnApplyBuff);

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
        if (InputBindService.Instance.IsDown(PlayerAction.OpenInventory) && storageModel.IsFullyLoaded)
        {
            UIService.Instance.ShowView<CharacterInfoView>((view) =>
            {
                view.Initialize(storageModel.MaxSize);
            });
            InputBindService.Instance.UIIsOpen = true;
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
    
    private void OnEntityCreated(string entityId)
    {
        if(!entityModel.TryGetEntity(entityId, out var entity)) return;
        if (!entityModel.IsLocalEntity(entityId)) return;
        var character = (NetworkCharacter)entity.NetworkEntity;
        gameView.UpdatePlayerExperience(character.Ex, character.MaxEx);
        gameView.UpdatePlayerHealth(character.Hp, character.MaxHp);
        gameView.UpdatePlayerMana(character.Mp, character.MaxMp);
    }
    
    private void OnEntityDestroyed(string entityId)
    {
        
    }

    private void OnEntityHpUpdated(string entityId, int currentHp, int maxHp, EntityType type)
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

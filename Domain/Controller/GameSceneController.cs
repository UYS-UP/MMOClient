using System;
using UnityEngine;

public class GameSceneController  : IDisposable
{
    private readonly EntityModel entityModel;
    private readonly DialogueModel dialogueModel;
    private readonly StorageModel storageModel;
    
    private readonly GameSceneView gameSceneView;

    public GameSceneController(GameSceneView gameSceneView)
    {
        entityModel = GameContext.Instance.Get<EntityModel>();
        dialogueModel = GameContext.Instance.Get<DialogueModel>();
        storageModel = GameContext.Instance.Get<StorageModel>();

        this.gameSceneView = gameSceneView;
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
        dialogueModel.OnDialogueTip += OnDialogueTip;
        entityModel.OnEntityHit += OnEntityHit;
        
        GameClient.Instance.RegisterHandler(Protocol.SC_EnterRegion, HandleEnterRegion);
        GameClient.Instance.RegisterHandler(Protocol.SC_EnterGame, HandleEnterGame);
    }

    private void UnregisterEvents()
    {
        entityModel.OnEntityCreated -= OnEntityCreated;
        entityModel.OnEntityDestroyed -= OnEntityDestroyed;
        entityModel.OnEntityHpUpdated -= OnEntityHpUpdated;
        entityModel.OnEntityHit -= OnEntityHit;
        dialogueModel.OnDialogueTip -= OnDialogueTip;
    }

    public void Update()
    {
        if (!InputBindService.Instance.IsDown(PlayerAction.Dialogue)
            || dialogueModel.CurrentNpcId == -1) return;
        InputBindService.Instance.UIIsOpen = true;
        UIService.Instance.ShowView<DialogueView>();
        dialogueModel.StartDialogue();
    }

    private void HandleEnterGame(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerEnterGame>();
        entityModel.CharacterId = data.CharacterId;
        gameSceneView.LoadRegionScene(data.MapId);
        storageModel.PreloadInventory();
    }

    private void HandleEnterRegion(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerEnterRegion>();
        gameSceneView.LoadRegionScene(data.MapId);
        storageModel.PreloadInventory();
    }
    
    #region EntityModel Events
    private void OnEntityCreated(int entityId)
    {
        if(!entityModel.TryGetEntity(entityId, out var entity)) return;
        
        if (entity.NetworkEntity is NetworkMonster monster)
        {
            gameSceneView.CreateHealthBar(entity.EntityId, entity.transform, monster.Hp, monster.MaxHp);
        }
    }

    private void OnEntityDestroyed(int entityId)
    {
        gameSceneView.DestroyHealthBar(entityId);
    }

    private void OnEntityHpUpdated(int entityId, float currentHp, float maxHp, EntityType type)
    {
        if (type == EntityType.Monster)
        {
            gameSceneView.UpdateHealthBar(entityId, currentHp, maxHp);
            
        }
    }

    private void OnEntityHit(Vector3 position, float damage)
    {
        gameSceneView.CreateDamageText(position + new Vector3(0, 1.5f, 0), damage);
    }
    #endregion
    
    
    #region DialogueModel Events

    private void OnDialogueTip(bool open)
    {
        if (open)
        {
            gameSceneView.ShowDialogueTip("按[E]进行对话");
            return;
        }
        gameSceneView.HideDialogueTip();
      
    }

    #endregion
}

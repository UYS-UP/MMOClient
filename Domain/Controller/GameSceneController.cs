using System;

public class GameSceneController  : IDisposable
{
    private readonly EntityModel entityModel;
    private readonly DialogueModel dialogueModel;
    
    private readonly GameSceneView gameSceneView;

    public GameSceneController(GameSceneView gameSceneView)
    {
        entityModel = GameContext.Instance.Get<EntityModel>();
        dialogueModel = GameContext.Instance.Get<DialogueModel>();

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
    }

    private void UnregisterEvents()
    {
        entityModel.OnEntityCreated -= OnEntityCreated;
        entityModel.OnEntityDestroyed -= OnEntityDestroyed;
        entityModel.OnEntityHpUpdated -= OnEntityHpUpdated;
        
        dialogueModel.OnDialogueTip -= OnDialogueTip;
    }

    public void Update()
    {
        if (!InputBindService.Instance.IsDown(PlayerAction.Dialogue)
            || dialogueModel.CurrentNpcId == "") return;
        InputBindService.Instance.UIIsOpen = true;
        UIService.Instance.ShowView<DialogueView>();
        dialogueModel.StartDialogue();
    }
    
    #region EntityModel Events
    private void OnEntityCreated(string entityId)
    {
        if(!entityModel.TryGetEntity(entityId, out var entity)) return;
        
        if (entity.NetworkEntity is NetworkMonster monster)
        {
            gameSceneView.CreateHealthBar(entity.EntityId, entity.transform, monster.Hp, monster.MaxHp);
        }
    }

    private void OnEntityDestroyed(string entityId)
    {
        gameSceneView.DestroyHealthBar(entityId);
    }

    private void OnEntityHpUpdated(string entityId, int currentHp, int maxHp, EntityType type)
    {
        if (type == EntityType.Monster)
        {
            gameSceneView.UpdateHealthBar(entityId, currentHp, maxHp);
        }
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

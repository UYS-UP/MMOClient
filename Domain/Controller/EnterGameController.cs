
using System;

public class EnterGameController : IDisposable
{
    private readonly EnterGameView enterGameView;
    private readonly PlayerModel playerModel;
    
    public EnterGameController(EnterGameView enterGameView)
    {
        this.enterGameView = enterGameView;
        playerModel =  GameContext.Instance.Get<PlayerModel>();
        RegisterEvents();
    }
    
    public void Dispose()
    {
        UnregisterEvents();
    }
    
    
    private void RegisterEvents()
    {
        GameClient.Instance.RegisterHandler(Protocol.SC_CreateCharacter, HandleCreateCharacter);

    }

    private void UnregisterEvents()
    {
        GameClient.Instance.UnregisterHandler(Protocol.SC_CreateCharacter);
    }
    
    private void HandleCreateCharacter(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerCreateCharacter>();
        if (!data.Success)
        {
            enterGameView.ShowTip(data.Message);
            return;
        }
        enterGameView.ShowTip(data.Message);
        UIService.Instance.HidePanel<EnterGameView>();
        UIService.Instance.ShowView<NotificationView>(layer: UILayer.Toast);
        UIService.Instance.ShowView<GameSceneView>(layer: UILayer.Scene);
        UIService.Instance.ShowView<GameView>(layer: UILayer.Normal);
        UIService.Instance.ShowView<GMView>(layer: UILayer.System);
        GameClient.Instance.Send(Protocol.CS_EnterGame, new ClientEnterGame {CharacterId = data.CharacterId});
    }
    
}

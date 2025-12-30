
using System;

public class LoginController : IDisposable
{
    private readonly PlayerModel playerModel;
    private readonly LoginView loginView;
    
    public LoginController(LoginView loginView)
    {
        playerModel = GameContext.Instance.Get<PlayerModel>();
        
        this.loginView = loginView;
        RegisterEvents();
    }
    
    
    public void Dispose()
    {
        UnregisterEvents();
    }
    
    
    private void RegisterEvents()
    {
        GameClient.Instance.RegisterHandler(Protocol.SC_Login, HandleLogin);
        GameClient.Instance.RegisterHandler(Protocol.SC_Register, HandleRegister);
    }

    private void UnregisterEvents()
    {
        GameClient.Instance.UnregisterHandler(Protocol.SC_Login);
        GameClient.Instance.UnregisterHandler(Protocol.SC_Register);
    }

    private void HandleLogin(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerPlayerLogin>();
        if (data.Sucess)
        { 
            UIService.Instance.HidePanel<LoginView>();
            playerModel.Initialize(data.Player);
            UIService.Instance.ShowView<EnterGameView>((panel) =>
            {
                 panel.AddCharacter(data.Previews);
            });
        }
    }

    private void HandleRegister(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerPlayerRegister>();
        if (data.Sucess)
        {
            loginView.Register(data.Username);
        }
    }


}

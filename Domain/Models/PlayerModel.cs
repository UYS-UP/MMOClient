
using System;

public class PlayerModel : Singleton<PlayerModel>, IDisposable
{
    public NetworkPlayer Player { get; private set; }

    public void Initialize(NetworkPlayer player)
    {
        Player = player;
    }
    
    public void Dispose()
    {
    }
}

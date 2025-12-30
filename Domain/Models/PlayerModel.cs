
using System;

public class PlayerModel : IDisposable
{
    public NetworkPlayer Player { get; private set; }

    public void Initialize(NetworkPlayer player)
    {
        Player = player;
    }

    public bool IsLocal(string playerId)
    {
        return Player.PlayerId == playerId;
    }
    
    public void Dispose()
    {
    }
}

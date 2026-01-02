
using System;

public class PlayerModel : IDisposable
{
    public NetworkPlayer Player { get; private set; }
    public string CharacterId { get; set; }

    public void Initialize(NetworkPlayer player)
    {
        Player = player;
    }

    public bool IsLocal(string characterId)
    {
        return CharacterId == characterId;
    }
    
    
    public void Dispose()
    {
    }
}

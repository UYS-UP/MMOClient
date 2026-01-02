
using System;

public class GMController : IDisposable
{
    
    
    public void Dispose()
    {
        
    }
    
    public void GMAddItem(string itemTemplateId)
    {
        GameClient.Instance.Send(Protocol.GM_AddItem, new GMAddItem {TemplateId = itemTemplateId, Count = 1});
    }
    
}

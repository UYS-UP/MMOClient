



using MessagePack;

[MessagePackObject]
public class GMAddItem
{
    [Key(0)] public string TemplateId;
    [Key(1)] public int Count;
}
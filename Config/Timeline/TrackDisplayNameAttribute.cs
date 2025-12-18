using System;


/// <summary>
/// 用于给 SkillTrack 派生类指定在编辑器中显示的名称
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TrackDisplayNameAttribute : Attribute
{
    public string DisplayName { get; }

    public TrackDisplayNameAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}


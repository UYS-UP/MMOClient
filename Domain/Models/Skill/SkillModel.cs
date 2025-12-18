using System;
using System.Collections.Generic;
using System.Linq;

public class SkillModel : IDisposable
{
    public static Dictionary<int, SkillConfig> SkillConfigs = null;

    // skillId -> remaining cooldown seconds
    private readonly Dictionary<int, float> cooldowns = new Dictionary<int, float>();
    private readonly List<int> entitySkills = new List<int>();

    public event Action<int> OnSkillReleased;

    public void UpdateEntitySkills(int index, int skillId)
    {
        while (entitySkills.Count <= index)
            entitySkills.Add(-1);

        entitySkills[index] = skillId;
    }

    public void UpdateEntitySkills(List<int> skills)
    {
        entitySkills.Clear();
        entitySkills.AddRange(skills);
    }

    public SkillConfig GetSkill(int index)
    {
        if (index < 0 || index >= entitySkills.Count) return null;
        int skillId = entitySkills[index];
        return SkillConfigs.GetValueOrDefault(skillId);
    }

    public void CastSkill(int skillId, bool success)
    {
        // if (!SkillConfigs.TryGetValue(skillId, out var data)) return;

        if (success)
        {
            cooldowns[skillId] = 2;
            // OnSkillReleased?.Invoke(skillId);
        }
    }

    public bool CheckSkill(int skillId)
    {
        if (cooldowns.TryGetValue(skillId, out var cd) && cd > 0f)
            return false;
        return true;
    }

    public float GetCooldownRemaining(int skillId)
    {
        return cooldowns.TryGetValue(skillId, out var cd) ? Math.Max(0f, cd) : 0f;
    }

    public void UpdateCooldown(float deltaTime)
    {
        foreach (var key in cooldowns.Keys.ToList())
        {
            cooldowns[key] -= deltaTime;
            if (cooldowns[key] <= 0f)
                cooldowns.Remove(key);
        }
    }

    public void Dispose()
    {
    }
}

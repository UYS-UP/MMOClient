public enum EntityType
{
    Character,
    Monster,
    Npc,
}

public enum EntityState
{
    None = 0,
    Dead = 1 << 0,
    Stunned = 1 << 1,
    Invincible = 1 << 2,
    Stealth = 1 << 3,
    Rooted = 1 << 4,
    Silenced = 1 << 5,
    Idle = 1 << 6,
    Move = 1 << 7,
    CastSkill = 1 << 8,
        
}

public enum ProfessionType
{
    Warrior,
    Mage,
}
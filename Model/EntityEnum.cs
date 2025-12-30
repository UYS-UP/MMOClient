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
    Hit = 1 << 1,
    Invincible = 1 << 2,
    Stealth = 1 << 3,
    Rooted = 1 << 4,
    Silenced = 1 << 5,
    Idle = 1 << 6,
    Move = 1 << 7,
    CastSkill = 1 << 8,
    Attack = 1 << 9,
    Roll = 1 << 10,
}

public enum ProfessionType
{
    Warrior,
    Mage,
}
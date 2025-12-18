using UnityEngine;

public class EntityFsmContext
{
        public readonly EntityBase Entity;
        public readonly AnimatorComponent Animator;
        public bool LockMove;
        public bool LockTurn;

        public Vector3 WishDir;
        public bool HasMoveInput;

        public bool HitRequested;
        public bool DeathRequested;

        public bool CastRequested;
        public int CastSkillId;
        public SkillInstance CastSkill;
        
        public bool ComboWindowOpen;
        public int ComboNextSkillId = -1;

        public bool ComboRequested;
        
        
        public void RequestCast(int skillId)
        {
                if (CastSkill != null && !CastSkill.IsFinished) return;

                CastRequested = true;
                CastSkillId = skillId;
        }
        

        public void ConsumeOneFrameFlags()
        {
                HitRequested = false;
                DeathRequested = false;
                CastRequested = false;
        }

        public EntityFsmContext(EntityBase entity)
        {
                Entity = entity;
                Animator = entity.GetEntityComponent<AnimatorComponent>();
        }
        

        public void RequestHit() => HitRequested = true;
        public void RequestDeath() => DeathRequested = true;
}

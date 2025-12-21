
using System.Collections.Generic;
using System.Linq;

public class SkillTimelineRunner
{
    public float CurrentTime { get; private set; }
    public bool IsFinished { get; private set; }

    private readonly float duration;
    private readonly List<SkillEvent> events;
    private int nextEventIndex;
    
    private readonly List<SkillPhase> phases;
    private int nextPhaseStartIndex;
    private readonly List<SkillPhase> activePhases = new();

    const float EPS = 0.000001f;
    
    public SkillTimelineRunner(float duration, IEnumerable<SkillEvent> events = null, IEnumerable<SkillPhase> phases = null)
    {
        this.duration = duration;
        CurrentTime = 0f;
        nextEventIndex = 0;
        IsFinished = false;
        if (events != null)
        {
            this.events = events.OrderBy(e => e.Time).ToList();
        }

        if (phases != null)
        {
            this.phases = phases.OrderBy(e => e.StartTime).ToList();
        }
    }
    
    public void Start(EntityBase caster)
    {
        FireDueEvents(caster, 0f);
        StartDuePhases(caster, 0f);
        UpdateActivePhases(caster, 0f); // 可选：让 Start 帧也跑一次 Update
        EndDuePhases(caster, 0f);
    }

    public void Tick(EntityBase caster, float dt)
    {
        if (IsFinished) return;

        CurrentTime += dt;

        FireDueEvents(caster, CurrentTime);

        StartDuePhases(caster, CurrentTime);
        UpdateActivePhases(caster, dt);
        EndDuePhases(caster, CurrentTime);

        if (CurrentTime >= duration)
            Finish(caster);
    }

    public void Interrupt(EntityBase caster)
    {
        if (IsFinished) return;
        IsFinished = true;
        EndAllActivePhases(caster);
    }

    private void FireDueEvents(EntityBase caster, float time)
    {
        while (nextEventIndex < events.Count && events[nextEventIndex].Time <= time + EPS)
        {
            events[nextEventIndex].Execute(caster);
            nextEventIndex++;
        }
    }

    private void StartDuePhases(EntityBase caster, float time)
    {
        while (nextPhaseStartIndex < phases.Count && phases[nextPhaseStartIndex].StartTime <= time + EPS)
        {
            var p = phases[nextPhaseStartIndex++];
            // 忽略非法区间
            if (p.EndTime <= p.StartTime) continue;

            p.OnStart(caster);
            activePhases.Add(p);
        }
    }

    private void UpdateActivePhases(EntityBase caster, float dt)
    {
        for (int i = 0; i < activePhases.Count; i++)
            activePhases[i].OnUpdate(caster, dt);
    }

    private void EndDuePhases(EntityBase caster, float time)
    {
        for (int i = activePhases.Count - 1; i >= 0; i--)
        {
            if (activePhases[i].EndTime <= time + EPS)
            {
                activePhases[i].OnExit(caster);
                activePhases.RemoveAt(i);
            }
        }
    }

    private void EndAllActivePhases(EntityBase caster)
    {
        for (int i = activePhases.Count - 1; i >= 0; i--)
            activePhases[i].OnExit(caster);
        activePhases.Clear();
    }

    private void Finish(EntityBase caster)
    {
        if (IsFinished) return;
        IsFinished = true;
        EndAllActivePhases(caster);
    }
    
}

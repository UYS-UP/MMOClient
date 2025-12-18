using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public interface ISequence
{
    bool IsDone { get; }
    void Start();
    bool Update();
}

public delegate UniTask PhaseStep(CancellationToken ct);

public class SequentialPhase : ISequence
{
    private readonly List<PhaseStep> steps;
    private readonly CancellationToken ct;
    private int index = -1;
    private UniTask current;
    
    public bool IsDone { get; private set; }

    public SequentialPhase(List<PhaseStep> steps, CancellationToken ct)
    {
        this.steps = steps;
        this.ct = ct;
    }
    
    public void Start()
    {
        Next();
    }
    
    public bool Update()
    {
        if (IsDone) return true;
        if(current.Status == UniTaskStatus.Succeeded) Next();
        return IsDone;
    }

    private void Next()
    {
        index++;
        if (index >= steps.Count)
        {
            IsDone = true;
            return;
        }
        current = steps[index](ct);
    }
}


public class ParallelPhase : ISequence
{
    private readonly List<PhaseStep> steps;
    private readonly CancellationToken ct;
    private List<UniTask> tasks;
    
    public bool IsDone { get; private set; }

    public ParallelPhase(List<PhaseStep> steps, CancellationToken ct)
    {
        this.steps = steps;
        this.ct = ct;
    }
    
    public void Start()
    {
        if (steps == null || steps.Count == 0)
        {
            IsDone = true;
            return;
        }
        
        tasks = new List<UniTask>();
        foreach (var step in steps)
        {
            tasks.Add(step.Invoke(ct));
        }
    }
    
    public bool Update()
    {
        if (IsDone) return true;
        IsDone = tasks.TrueForAll(t => t.Status.IsCompleted());
        return IsDone;
    }
}

public class NoopPase : ISequence
{
    public bool IsDone { get; private set; }
    public void Start()
    {
        IsDone = true;
    }

    public bool Update()
    {
        return IsDone;
    }
}
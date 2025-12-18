
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;


public class TransitionSequencer
{
    public readonly HStateMachine Machine;
    
    private ISequence sequence;
    private Action nextPhase;
    private (HState from, HState to)? pending;
    private HState lastFrom, lastTo;
    private CancellationTokenSource cts;
    public readonly bool UseSequential = true;
    
    public enum Phase { Deactivate, Activate }
    
    public TransitionSequencer(HStateMachine machine)
    {
        Machine = machine;
    }

    private void BeginTransition(HState from, HState to)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        var lca = Lca(from, to);
        var exitChain = StatesToExit(from, lca);
        var enterChain = StatesToEnter(to, lca);

        var exitSteps = GatherPhaseSteps(exitChain, Phase.Deactivate);
        sequence = UseSequential ? new SequentialPhase(exitSteps, cts.Token) : new ParallelPhase(exitSteps, cts.Token);
        sequence.Start();

        nextPhase = () =>
        {
            Machine.ChangeState(from, to);
            var enterSteps = GatherPhaseSteps(enterChain, Phase.Activate);
            sequence = UseSequential ? new SequentialPhase(enterSteps, cts.Token) : new ParallelPhase(enterSteps, cts.Token);
            sequence.Start();
        };
    }

    private void EndTransition()
    {
        sequence = null;
        if (pending.HasValue)
        {
            (HState from, HState to) = pending.Value;
            pending = null;
            BeginTransition(from, to);
        }
    }

    public void Update(float deltaTime)
    {
        if (sequence != null)
        {
            if (sequence.Update())
            {
                if (nextPhase != null)
                {
                    var n = nextPhase;
                    nextPhase = null;
                    n?.Invoke();
                }
                else
                {
                    EndTransition();
                }
            }
            return;
        }
        Machine.InternalUpdate(deltaTime);
    }

    public void RequestTransition(HState from, HState to)
    {
        if(to == null || from == to) return;
        if (sequence == null)
        {
            BeginTransition(from, to);
            return;
        }
        pending = (from, to);
    }

    public static HState Lca(HState a, HState b)
    {
        var aPrent = new HashSet<HState>();
        
        for (var s = a;  s != null; s = s.Parent)
        {
            aPrent.Add(s);
        }

        for (var s = b;  s != null; s = s.Parent)
        {
            if (aPrent.Contains(s)) return s;
        }
        return null;
    }

    public static List<HState> StatesToExit(HState from, HState lca)
    {
        var list = new List<HState>();
        for (var s = from;  s != null && s != lca; s = s.Parent)
        {
            list.Add(s);
        }
        return list;
    }

    public static List<HState> StatesToEnter(HState to, HState lca)
    {
        var stack = new Stack<HState>();
        for (var s = to; s != lca && s != lca; s = s.Parent)
        {
            stack.Push(s);
        }
        return stack.ToList();
    }

    public static List<PhaseStep> GatherPhaseSteps(List<HState> chain, Phase phase)
    {
        var steps = new List<PhaseStep>();
        if (chain == null) return steps;

        for (int i = 0; i < chain.Count; i++)
        {
            var acts = chain[i].Activities;
            for (int j = 0; j < acts.Count; j++)
            {
                var a = acts[j];
                if (a == null) continue;

                switch (phase)
                {
                    case Phase.Deactivate:
                        if (a.Mode == ActivityMode.Active)
                            steps.Add(ct => a.DeactivateAsync(ct));
                        break;

                    case Phase.Activate:
                        if (a.Mode == ActivityMode.Inactive)
                            steps.Add(ct => a.ActivateAsync(ct));
                        break;
                }
            }
        }

        return steps;
    }
}






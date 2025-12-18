
using System.Collections.Generic;
using System.Linq;

public class HStateMachine
{
    public HState Root { get; private set; }
    public readonly TransitionSequencer Sequencer;
    private bool started;
    
    public HStateMachine()
    {
        Sequencer = new TransitionSequencer(this);
    }

    public void SetRoot(HState root)
    {
        Root = root;
    }

    public void Start()
    {
        if (started) return;
        started = true;
        Root.Enter();
    }

    public void Update(float deltaTime)
    {
        if(!started) Start();
        Sequencer.Update(deltaTime);
    }
    
    public void InternalUpdate(float deltaTime) => Root.Update(deltaTime);

    public void ChangeState(HState from, HState to)
    {
        if (from == to || from == null || to == null) return;

        var lca = TransitionSequencer.Lca(from, to);
        
        for (var s = from; s != null && s != lca; s = s.Parent)
            s.Exit();
        
        var stack = new Stack<HState>();
        for (var s = to; s != null && s != lca; s = s.Parent)
            stack.Push(s);

        while (stack.Count > 0)
            stack.Pop().Enter();
    }

    public static string StatePath(HState s)
    {
        return string.Join("> ", s.PathToRoot().Reverse().Select(n => n.GetType().Name));
    }
}

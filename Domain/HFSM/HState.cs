
using System.Collections.Generic;

public abstract class HState
{
    public readonly HStateMachine Machine;
    public readonly HState Parent;
    public HState ActiveChild;
    private readonly List<IActivity> activities = new List<IActivity>();
    public IReadOnlyList<IActivity> Activities => activities;

    protected HState(HStateMachine machine, HState parent)
    {
        this.Machine = machine;
        this.Parent = parent;
    }

    protected void Add(IActivity activity)
    {
        if(activity != null) activities.Add(activity);
    }

    /// <summary>
    /// 入此状态时，默认进入哪个子节点
    /// </summary>
    /// <returns>目标状态</returns>
    protected virtual HState GetInitialState() => null;
    
    /// <summary>
    /// 当前状态想切到哪个状态
    /// </summary>
    /// <returns>目标叶子</returns>
    protected virtual HState GetTransition()  => null;
    
    // 生命周期函数
    protected virtual void OnEnter() { }
    protected virtual void OnExit() { }
    protected virtual void OnUpdate(float deltaTime) { }

    public void Enter()
    {
        if(Parent != null) Parent.ActiveChild = this;
        OnEnter();
        HState init = GetInitialState();
        if(init != null) init.Enter();
    }

    public void Exit()
    {
        if(ActiveChild != null) ActiveChild.Exit();
        ActiveChild = null;
        OnExit();
    }

    public void Update(float deltaTime)
    {
        var t = GetTransition();
        if (t != null)
        {
            var fromLeaf = Leaf();
            Machine.Sequencer.RequestTransition(fromLeaf, t);
            return;
        }

        ActiveChild?.Update(deltaTime);
        OnUpdate(deltaTime);
    }

    /// <summary>
    /// 返回一个激活的叶子节点
    /// </summary>
    /// <returns></returns>
    public HState Leaf()
    {
        HState s = this;
        while (s.ActiveChild != null)
        {
            s = s.ActiveChild;
        }

        return s;
    }

    /// <summary>
    /// 返回从当前节点到根节点的所有状态
    /// </summary>
    /// <returns></returns>
    public IEnumerable<HState> PathToRoot()
    {
        for (var s  = this;  s.Parent != null; s = s.Parent)
        {
            yield return s;
        }
    }

    public T AsTo<T>() where T : HState
    {
        return this as T;
    }
    
}

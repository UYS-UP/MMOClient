
public class EntityHFSM
{
    public readonly HStateMachine Machine = new HStateMachine();
    public readonly EntityFsmContext Ctx;

    private RootState root;

    public EntityHFSM(EntityBase entity)
    {
        Ctx = new EntityFsmContext(entity);

        root = new RootState(Ctx, Machine, null);
        
        Machine.SetRoot(root);
        Machine.Start();
    }

    public void Update(float deltaTime) => Machine.Update(deltaTime);
}

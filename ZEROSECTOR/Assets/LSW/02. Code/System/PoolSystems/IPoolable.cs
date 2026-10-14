namespace LSW._02._Code.System.PoolSystems
{
    public interface IPoolable
    {
        void Initialize();
        void Spawn();
        void Despawn();
    }
}
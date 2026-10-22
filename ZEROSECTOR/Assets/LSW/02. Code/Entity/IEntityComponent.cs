namespace LSW._02._Code.Entity
{
    public interface IEntityComponent
    {
        public void Initialize(BaseEntity owner);
        public void Reset();
    }
}
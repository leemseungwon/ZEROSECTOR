using LSW._02._Code.Entity;

namespace LSW._02._Code.Combat
{
    public interface IWeapon
    {
        void Initialize(BaseEntity owner);
        bool Attack();
    }
}
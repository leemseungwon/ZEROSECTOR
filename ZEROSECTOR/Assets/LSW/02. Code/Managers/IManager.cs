using LSW._02._Code.Common;

namespace LSW._02._Code.Managers
{
    public interface IManager
    {
        public void Initialize(ManagerHandler managerHandler);
        public void LoadScene(SceneType sceneType);
        public void Reset();
    }
}
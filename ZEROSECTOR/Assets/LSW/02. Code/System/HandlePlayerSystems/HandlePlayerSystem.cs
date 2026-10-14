using LSW._02._Code.Common;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.UI;
using UnityEngine;

namespace LSW._02._Code.System.HandlePlayerSystems
{
    [SystemOrder(-1)]
    public class HandlePlayerSystem : MonoBehaviour, ISystem
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InGameUI inGameUI;
        
        public PlayerController Player => playerController;
        public Camera Camera => playerController.PlayerCamera;
        public InGameUI InGameUI => inGameUI;
        
        public void Initialize(SystemHandler systemHandler) { }
        
        public void Reset() { } 
    }
}
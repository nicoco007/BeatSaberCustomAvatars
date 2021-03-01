using CustomAvatar.Player;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    internal class AvatarCenterAdjust : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;

        [Inject]
        public void Construct(PlayerAvatarManager playerAvatarManager)
        {
            _playerAvatarManager = playerAvatarManager;
        }

        public void OnEnable()
        {
            _playerAvatarManager?.SetParent(transform);
        }

        public void Start()
        {
            OnEnable();
        }

        public void OnDestroy()
        {
            _playerAvatarManager?.SetParent(null);
        }
    }
}

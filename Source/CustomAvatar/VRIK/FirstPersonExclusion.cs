using System;
using UnityEngine;

#if PLUGIN
using System.Linq;
using CustomAvatar;
#endif

namespace AvatarScriptPack
{
    public class FirstPersonExclusion : MonoBehaviour
    {
        public GameObject[] Exclude;
#if PLUGIN
        private int[] _startLayers;
        private bool _deadSwitch;

        private void OnEnable()
        {
            if (Exclude == null)
            {
                Destroy(this);
                return;
            }

            _startLayers = Exclude.Select(x => x.layer).ToArray();

            OnFirstPersonEnabledChanged();
        }

        public void OnFirstPersonEnabledChanged()
        {
            try
            {
                if (_deadSwitch)
                {
                    return;
                }
                for (var i = 0; i < Exclude.Length; i++)
                {
                    var excludeObject = Exclude[i];
                    excludeObject.layer = AvatarLayers.OnlyInThirdPerson;
                }
            }
            catch (Exception ex)
            {
                Plugin.logger.Error(ex);
            }
        }

        public void SetVisible()
        {
            _deadSwitch = true;
            for (var i = 0; i < Exclude.Length; i++)
            {
                var excludeObject = Exclude[i];
                excludeObject.layer = _startLayers[i];
            }
        }
#endif //PLUGIN
    }
}

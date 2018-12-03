using System;
using System.Linq;
using CustomAvatar;
using UnityEngine;

namespace AvatarScriptPack
{
	public class FirstPersonExclusion : MonoBehaviour
	{
		public GameObject[] Exclude;

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

			Plugin.Instance.FirstPersonEnabledChanged += OnFirstPersonEnabledChanged;
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
		}

		private void OnDisable()
		{
			Plugin.Instance.FirstPersonEnabledChanged -= OnFirstPersonEnabledChanged;
		}

		public void OnFirstPersonEnabledChanged(bool firstPersonEnabled)
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

		public void SetVisible()
		{
			_deadSwitch = true;
			for (var i = 0; i < Exclude.Length; i++)
			{
				var excludeObject = Exclude[i];
				excludeObject.layer = _startLayers[i];
			}
		}
	}
}
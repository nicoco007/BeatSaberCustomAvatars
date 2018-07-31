using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class AvatarSpawner
	{
		private float _playerHeight;
		private GameObject _spawnedAvatar;
		private readonly Dictionary<GameObject, int> _prevLayers = new Dictionary<GameObject, int>();

		public AvatarSpawner(float playerHeight)
		{
			_playerHeight = playerHeight;
			Plugin.Instance.FirstPersonEnabledChanged += OnFirstPersonEnabledChanged;
		}

		~AvatarSpawner()
		{
			Plugin.Instance.FirstPersonEnabledChanged -= OnFirstPersonEnabledChanged;
		}

		public GameObject SpawnAvatar(IAvatarInput avatarInput, CustomAvatar customAvatar)
		{
			if (customAvatar.GameObject == null)
			{
				Plugin.Log("Can't spawn " + customAvatar.FullPath + " because it hasn't been loaded!");
				return null;
			}

			if (_spawnedAvatar != null)
			{
				Object.Destroy(_spawnedAvatar);
			}

			_spawnedAvatar = Object.Instantiate(customAvatar.GameObject);

			var behaviour = _spawnedAvatar.AddComponent<AvatarBehaviour>();
			behaviour.Init(avatarInput);
			
			Object.DontDestroyOnLoad(_spawnedAvatar);

			if (customAvatar.AllowHeightCalibration)
			{
				var startScale = _spawnedAvatar.transform.localScale;
				_spawnedAvatar.transform.localScale = startScale * (_playerHeight / customAvatar.Height);
			}

			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
			FixSkinnedMeshRendererOffscreen();
			
			return _spawnedAvatar;
		}

		private void OnFirstPersonEnabledChanged(bool firstPersonEnabled)
		{
			if (_spawnedAvatar == null) return;
			foreach (var transform in _spawnedAvatar.GetComponentsInChildren<Transform>(true))
			{
				if (!firstPersonEnabled)
				{
					if (!_prevLayers.ContainsKey(transform.gameObject))
					{
						_prevLayers.Add(transform.gameObject, transform.gameObject.layer);
					}

					transform.gameObject.layer = (int) AvatarLayer.NotShownInFirstPerson;
				}
				else
				{
					_prevLayers.TryGetValue(transform.gameObject, out var prevLayer);
					transform.gameObject.layer = prevLayer;
				}
			}
		}

		private void FixSkinnedMeshRendererOffscreen()
		{
			foreach (var skinnedMeshRenderer in _spawnedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				skinnedMeshRenderer.updateWhenOffscreen = true;
			}
		}
	}
}
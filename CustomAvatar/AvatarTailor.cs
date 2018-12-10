using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomAvatar
{
	public class AvatarTailor
	{
		private float? _currentAvatarArmLength = null;
		private Vector3? _initialPlatformPosition = null;
		private float? _initialAvatarPositionY = null;
		private Vector3 _initialAvatarLocalScale = Vector3.one;

		private const string _kPlayerArmLengthKey = "CustomAvatar.Tailoring.PlayerArmLength";
		private const string _kResizePolicyKey = "CustomAvatar.Tailoring.ResizePolicy";
		private const string _kFloorMovePolicyKey = "CustomAvatar.Tailoring.FloorMovePolicy";

		public enum ResizePolicyType
		{
			AlignArmLength,
			AlignHeight,
			NeverResize
		}

		public enum FloorMovePolicyType
		{
			AllowMove,
			NeverMove
		}

		public float PlayerArmLength
		{
			get => PlayerPrefs.GetFloat(_kPlayerArmLengthKey, BeatSaberUtil.GetPlayerHeight() * 0.88f);
			private set => PlayerPrefs.SetFloat(_kPlayerArmLengthKey, value);
		}

		public ResizePolicyType ResizePolicy
		{
			get => (ResizePolicyType)PlayerPrefs.GetInt(_kResizePolicyKey, 1);
			set => PlayerPrefs.SetInt(_kResizePolicyKey, (int)value);
		}

		public FloorMovePolicyType FloorMovePolicy
		{
			get => (FloorMovePolicyType)PlayerPrefs.GetInt(_kFloorMovePolicyKey, 1);
			set => PlayerPrefs.SetInt(_kFloorMovePolicyKey, (int)value);
		}

		private Animator FindAvatarAnimator(GameObject gameObject)
		{
			var vrik = gameObject.GetComponentInChildren<AvatarScriptPack.VRIK>();
			if (vrik == null) return null;
			var animator = vrik.gameObject.GetComponentInChildren<Animator>();
			if (animator.avatar == null || !animator.isHuman) return null;
			return animator;
		}

		public void OnAvatarLoaded(SpawnedAvatar avatar)
		{
			_initialAvatarLocalScale = avatar.GameObject.transform.localScale;
			_initialAvatarPositionY = null;
			_currentAvatarArmLength = null;
		}

		public void ResizeAvatar(SpawnedAvatar avatar)
		{
			var animator = FindAvatarAnimator(avatar.GameObject);
			if (animator == null)
			{
				Plugin.Log("Tailor: Animator not found");
				return;
			}

			// compute scale
			float scale = 1.0f;
			if (ResizePolicy == ResizePolicyType.AlignArmLength)
			{
				float playerArmLength = PlayerArmLength;
				_currentAvatarArmLength = _currentAvatarArmLength ?? AvatarMeasurement.MeasureArmLength(animator);
				var avatarArmLength = _currentAvatarArmLength ?? playerArmLength;
				Plugin.Log("Avatar arm length: " + avatarArmLength);

				scale = playerArmLength / avatarArmLength;
			}
			else if (ResizePolicy == ResizePolicyType.AlignHeight)
			{
				scale = BeatSaberUtil.GetPlayerHeight() / avatar.CustomAvatar.Height;
			}

			// apply scale
			avatar.GameObject.transform.localScale = _initialAvatarLocalScale * scale;

			// compute offset
			float floorOffset = 0f;
			// give up moving original foot floors
			var originalFloor = GameObject.Find("MenuPlayersPlace") ?? GameObject.Find("Static/PlayersPlace");
			if (originalFloor != null && originalFloor.activeSelf == true) floorOffset = 0f;

			if (FloorMovePolicy == FloorMovePolicyType.AllowMove)
			{
				float playerViewPointHeight = BeatSaberUtil.GetPlayerViewPointHeight();
				float avatarViewPointHeight = avatar.CustomAvatar.ViewPoint?.position.y ?? playerViewPointHeight;
				_initialAvatarPositionY = _initialAvatarPositionY ?? animator.transform.position.y;
				const float FloorLevelOffset = 0.04f; // a heuristic value from testing on oculus rift
				floorOffset = playerViewPointHeight - (avatarViewPointHeight * scale) + FloorLevelOffset;
			}

			// apply offset
			animator.transform.position = new Vector3(animator.transform.position.x, floorOffset + _initialAvatarPositionY ?? 0, animator.transform.position.z);

			var customFloor = GameObject.Find("Platform Loader");
			if (customFloor != null)
			{
				_initialPlatformPosition = _initialPlatformPosition ?? customFloor.transform.position;
				var floorTailor = customFloor.AddComponent<FloorLevelTailor>();
				floorTailor.destination = (Vector3.up * floorOffset) + _initialPlatformPosition ?? Vector3.zero;
			}

			Plugin.Log("Avatar resized with scale: " + scale + " floor-offset: " + floorOffset);
		}

		private class FloorLevelTailor : MonoBehaviour
		{
			public Vector3 destination;

			private void LateFix()
			{
				transform.position = destination;
				Plugin.Log("Custom Platform moved to: " + transform.position.y);
				Destroy(this);
			}

			private void Start()
			{
				Invoke("LateFix", 0.1f);
			}
		}

		public void MeasurePlayerArmLength(Action<float> onProgress, Action<float> onFinished)
		{
			var active = SceneManager.GetActiveScene().GetRootGameObjects()[0].GetComponent<PlayerArmLengthMeasurement>();
			if (active != null)
			{
				GameObject.Destroy(active);
			}
			active = SceneManager.GetActiveScene().GetRootGameObjects()[0].AddComponent<PlayerArmLengthMeasurement>();
			active.onProgress = onProgress;
			active.onFinished = (result) =>
			{
				PlayerArmLength = result;
				onFinished(result);
			};
		}

		private class PlayerArmLengthMeasurement : MonoBehaviour
		{
			private PlayerAvatarInput playerInput = new PlayerAvatarInput();
			private const float initialValue = 0.5f;
			private float maxHandToHandLength = initialValue;
			private float updateTime = 0;
			public Action<float> onFinished = null;
			public Action<float> onProgress = null;

			void Scan()
			{
				var handToHandLength = Vector3.Distance(playerInput.LeftPosRot.Position, playerInput.RightPosRot.Position);
				if (maxHandToHandLength < handToHandLength)
				{
					maxHandToHandLength = handToHandLength;
					updateTime = Time.timeSinceLevelLoad;
				}
				else if (Time.timeSinceLevelLoad - updateTime > 2.0f)
				{
					onFinished?.Invoke(maxHandToHandLength);
					Destroy(this);
					return;
				}
				onProgress?.Invoke(maxHandToHandLength);
			}

			void Start()
			{
				InvokeRepeating("Scan", 1.0f, 0.2f);
			}

			void OnDestroy()
			{
				CancelInvoke();
			}
		}
	}
}

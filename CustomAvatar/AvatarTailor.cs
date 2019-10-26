using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static IPA.Logging.Logger;

namespace CustomAvatar
{
	public class AvatarTailor
	{
		private float? _currentAvatarArmLength = null;
		private Vector3? _initialPlatformPosition = null;
		private float? _initialAvatarPositionY = null;
		private Vector3 _initialAvatarLocalScale = Vector3.one;

		private const string kPlayerArmLengthKey = "CustomAvatar.Tailoring.PlayerArmLength";
		private const string kResizePolicyKey = "CustomAvatar.Tailoring.ResizePolicy";
		private const string kFloorMovePolicyKey = "CustomAvatar.Tailoring.FloorMovePolicy";

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
			get => PlayerPrefs.GetFloat(kPlayerArmLengthKey, BeatSaberUtil.GetPlayerHeight() * 0.88f);
			private set => PlayerPrefs.SetFloat(kPlayerArmLengthKey, value);
		}

		public ResizePolicyType ResizePolicy
		{
			get => (ResizePolicyType)PlayerPrefs.GetInt(kResizePolicyKey, 1);
			set => PlayerPrefs.SetInt(kResizePolicyKey, (int)value);
		}

		public FloorMovePolicyType FloorMovePolicy
		{
			get => (FloorMovePolicyType)PlayerPrefs.GetInt(kFloorMovePolicyKey, 1);
			set => PlayerPrefs.SetInt(kFloorMovePolicyKey, (int)value);
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
				Plugin.Logger.Log(Level.Error, "Tailor: Animator not found");
				return;
			}

			// compute scale
			float scale = 1.0f;
			if (ResizePolicy == ResizePolicyType.AlignArmLength)
			{
				float playerArmLength = PlayerArmLength;
				_currentAvatarArmLength = _currentAvatarArmLength ?? MeasureAvatarArmSpan(animator);
				var avatarArmLength = _currentAvatarArmLength ?? playerArmLength;
				Plugin.Logger.Log(Level.Debug, "Avatar arm length: " + avatarArmLength);

				scale = playerArmLength / avatarArmLength;
			}
			else if (ResizePolicy == ResizePolicyType.AlignHeight)
			{
				scale = BeatSaberUtil.GetPlayerEyeHeight() / avatar.CustomAvatar.eyeHeight;
			}

			// apply scale
			avatar.GameObject.transform.localScale = _initialAvatarLocalScale * scale;

			Plugin.Logger.Log(Level.Info, "Avatar resized with scale: " + scale);

			SharedCoroutineStarter.instance.StartCoroutine(FloorMendingWithDelay(avatar, animator, scale));
		}

		private IEnumerator FloorMendingWithDelay(SpawnedAvatar avatar, Animator animator, float scale)
		{
			yield return new WaitForEndOfFrame(); // wait for CustomFloorPlugin:PlatformManager:Start hides original platform
			// compute offset
			float floorOffset = 0f;
			// give up moving original foot floors
			var originalFloor = GameObject.Find("MenuPlayersPlace") ?? GameObject.Find("Static/PlayersPlace");
			if (originalFloor != null && originalFloor.activeSelf == true)
			{
				floorOffset = 0f;
			}
			else if (FloorMovePolicy == FloorMovePolicyType.AllowMove)
			{
				float playerViewPointHeight = BeatSaberUtil.GetPlayerEyeHeight();
				float avatarViewPointHeight = avatar.CustomAvatar.viewPoint?.position.y ?? playerViewPointHeight;
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
				customFloor.transform.position = (Vector3.up * floorOffset) + _initialPlatformPosition ?? Vector3.zero;
				Plugin.Logger.Log(Level.Info, "CustomFloor moved to " + customFloor.transform.position.y + " with offset " + floorOffset);
			}
		}

		public void MeasurePlayerArmSpan(Action<float> onProgress, Action<float> onFinished)
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

		public static float MeasureAvatarArmSpan(Animator animator)
		{
			var indexFinger1 = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal).position;
			var leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
			var leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
			var rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
			var leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
			var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;

			var shoulderLength = Vector3.Distance(leftUpperArm, leftShoulder) * 2.0f + Vector3.Distance(leftShoulder, rightShoulder);
			var armLength = (Vector3.Distance(indexFinger1, leftHand) * 0.5f + Vector3.Distance(leftHand, leftElbow) + Vector3.Distance(leftElbow, leftUpperArm)) * 2.0f;

			return shoulderLength + armLength;
		}

		private class PlayerArmLengthMeasurement : MonoBehaviour
		{
			private TrackedDeviceManager playerInput = PersistentSingleton<TrackedDeviceManager>.instance;
			private const float initialValue = 0.5f;
			private float maxHandToHandLength = initialValue;
			private float updateTime = 0;
			public Action<float> onFinished = null;
			public Action<float> onProgress = null;

			void Scan()
			{
				var handToHandLength = Vector3.Distance(playerInput.LeftHand.Position, playerInput.RightHand.Position);
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

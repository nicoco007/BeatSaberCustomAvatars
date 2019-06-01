using System.Linq;
using UnityEngine;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	class AvatarPreviewBehaviour : MonoBehaviour
	{
		GameObject _avatarMirror;

		Transform _body;
		Transform _head;
		Transform _leftHand;
		Transform _rightHand;
		Transform _pelvis;
		Transform _leftLeg;
		Transform _rightLeg;

		Transform _playerBody;
		Transform _playerHead;
		Transform _playerLeftHand;
		Transform _playerRightHand;
		Transform _playerPelvis;
		Transform _playerLeftLeg;
		Transform _playerRightLeg;

		float mirrorOffset = 2.0f;
		bool isFBT = false;

		bool isValid = true;

		public void SetVRTargets(Transform body, Transform head, Transform leftHand, Transform rightHand)
		{
			if (!(body && head && leftHand && rightHand))
			{
				Logger.Log("Something went wrong - IK Targets not found for player");
				Logger.Log("Body: " + body.name);
				Logger.Log("HeadTarget: " + head.name);
				Logger.Log("LeftHandTarget: " + leftHand.name);
				Logger.Log("RightHandTarget: " + rightHand.name);
				isValid = false;
				return;
			}

			_playerBody = body;
			_playerHead = head;
			_playerLeftHand = leftHand;
			_playerRightHand = rightHand;
		}

		public void Init(GameObject avatarMirror)
		{
			Logger.Log("Begin Mirror Init");
			_avatarMirror = avatarMirror;
			var _VRIK = _avatarMirror.GetComponentsInChildren<AvatarScriptPack.VRIK>().FirstOrDefault();

			Logger.Log("Obtaining IK Targets for Mirror");
			_head = _avatarMirror.transform.Find("Head/HeadTarget").transform;
			_leftHand = _avatarMirror.transform.Find("LeftHand/LeftHandTarget").transform;
			_rightHand = _avatarMirror.transform.Find("RightHand/RightHandTarget").transform;

			if (!(_head && _leftHand && _rightHand))
			{
				Logger.Log("Something went wrong - IK Targets not found for mirror");
				isValid = false;
			}

			Logger.Log("Setting IK Targets for mirror");
			_VRIK.solver.spine.headTarget = _head.transform;
			_VRIK.solver.leftArm.target = _leftHand.transform;
			_VRIK.solver.rightArm.target = _rightHand.transform;
		}

		void LateUpdate()
		{
			if (_avatarMirror && isValid)
			{
				_head.position = _playerHead.position + new Vector3(0, 0, mirrorOffset);
				_leftHand.position = _playerRightHand.position + new Vector3(0, 0, mirrorOffset);
				_rightHand.position = _playerLeftHand.position + new Vector3(0, 0, mirrorOffset);
			}
		}


	}
}

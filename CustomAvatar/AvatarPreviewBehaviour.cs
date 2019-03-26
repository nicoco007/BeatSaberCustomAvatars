using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
				Plugin.Log("Something went wrong - IK Targets not found for player");
				Plugin.Log("Body: " + body.name);
				Plugin.Log("HeadTarget: " + head.name);
				Plugin.Log("LeftHandTarget: " + leftHand.name);
				Plugin.Log("RightHandTarget: " + rightHand.name);
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
			Plugin.Log("Begin Mirror Init");
			_avatarMirror = avatarMirror;
			var _VRIK = _avatarMirror.GetComponentsInChildren<AvatarScriptPack.VRIK>().FirstOrDefault();

			Plugin.Log("Obtaining IK Targets for Mirror");
			_head = _avatarMirror.transform.Find("Head/HeadTarget").transform;
			_leftHand = _avatarMirror.transform.Find("LeftHand/LeftHandTarget").transform;
			_rightHand = _avatarMirror.transform.Find("RightHand/RightHandTarget").transform;

			if (!(_head && _leftHand && _rightHand))
			{
				Plugin.Log("Something went wrong - IK Targets not found for mirror");
				isValid = false;
			}

			Plugin.Log("Setting IK Targets for mirror");
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

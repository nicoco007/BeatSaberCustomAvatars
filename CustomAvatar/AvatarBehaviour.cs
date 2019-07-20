using System;
using UnityEngine;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	public class AvatarBehaviour : MonoBehaviour
	{
		private IAvatarInput _avatarInput;

		private Transform _head;
		private Transform _body;
		private Transform _leftHand;
		private Transform _rightHand;
		private Transform _leftLeg;
		private Transform _rightLeg;
		private Transform _pelvis;

		private Vector3 _prevBodyPos;

		private Vector3 _prevLeftLegPos = default(Vector3);
		private Vector3 _prevRightLegPos = default(Vector3);
		private Quaternion _prevLeftLegRot = default(Quaternion);
		private Quaternion _prevRightLegRot = default(Quaternion);

		private Vector3 _prevPelvisPos = default(Vector3);
		private Quaternion _prevPelvisRot = default(Quaternion);

		public void Init(IAvatarInput avatarInput)
		{
			_avatarInput = avatarInput;

			_head = GetHeadTransform();
			_body = gameObject.transform.Find("Body");
			_leftHand = gameObject.transform.Find("LeftHand");
			_rightHand = gameObject.transform.Find("RightHand");
			_leftLeg = gameObject.transform.Find("LeftLeg");
			_rightLeg = gameObject.transform.Find("RightLeg");
			_pelvis = gameObject.transform.Find("Pelvis");
		}

		private void LateUpdate()
		{
			try
			{
				var headPosRot = _avatarInput.HeadPosRot;
				var leftPosRot = _avatarInput.LeftPosRot;
				var rightPosRot = _avatarInput.RightPosRot;

				_head.position = headPosRot.Position;
				_head.rotation = headPosRot.Rotation;

				_leftHand.position = leftPosRot.Position;
				_leftHand.rotation = leftPosRot.Rotation;

				_rightHand.position = rightPosRot.Position;
				_rightHand.rotation = rightPosRot.Rotation;

				if (_leftLeg != null && _rightLeg != null && _avatarInput is IAvatarFullBodyInput)
				{
					var _fbinput = _avatarInput as IAvatarFullBodyInput;
					var leftLegPosRot = _fbinput.LeftLegPosRot;
					var rightLegPosRot = _fbinput.RightLegPosRot;
					_prevLeftLegPos = Vector3.Lerp(_prevLeftLegPos, leftLegPosRot.Position, 1 / Time.deltaTime * 0.0018f);
					_prevLeftLegRot = Quaternion.Slerp(_prevLeftLegRot, leftLegPosRot.Rotation, 1 / Time.deltaTime * 0.0012f);
					_leftLeg.position = _prevLeftLegPos;
					_leftLeg.rotation = _prevLeftLegRot;

					_prevRightLegPos = Vector3.Lerp(_prevRightLegPos, rightLegPosRot.Position, 1 / Time.deltaTime * 0.0018f);
					_prevRightLegRot = Quaternion.Slerp(_prevRightLegRot, rightLegPosRot.Rotation, 1 / Time.deltaTime * 0.0012f);
					_rightLeg.position = _prevRightLegPos;
					_rightLeg.rotation = _prevRightLegRot;
				}

				if(_pelvis != null && _avatarInput is IAvatarFullBodyInput)
				{
					var _fbinput = _avatarInput as IAvatarFullBodyInput;
					var pelvisPosRot = _fbinput.PelvisPosRot;

					_prevPelvisPos = Vector3.Lerp(_prevPelvisPos, pelvisPosRot.Position, 1 / Time.deltaTime * 0.0018f);
					_prevPelvisRot = Quaternion.Slerp(_prevPelvisRot, pelvisPosRot.Rotation, 1 / Time.deltaTime * 0.0012f);
					_pelvis.position = _prevPelvisPos;
					_pelvis.rotation = _prevPelvisRot;
				}

				var vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

				vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_leftHand);
				vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_rightHand);

				if (_body == null) return;
				_body.position = _head.position - (_head.transform.up * 0.1f);

				var vel = new Vector3(_body.transform.localPosition.x - _prevBodyPos.x, 0.0f,
					_body.localPosition.z - _prevBodyPos.z);

				var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
				var tiltAxis = Vector3.Cross(gameObject.transform.up, vel);
				_body.localRotation = Quaternion.Lerp(_body.localRotation,
					Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
					Time.deltaTime * 10.0f);

				_prevBodyPos = _body.transform.localPosition;
			} catch(Exception e)
			{
				Logger.Log($"{e.Message}\n{e.StackTrace}", Logger.LogLevel.Error);
			}
		}

		private Transform GetHeadTransform()
		{
			var descriptor = GetComponent<AvatarDescriptor>();
			if (descriptor != null)
			{
				//if (descriptor.ViewPoint != null) return descriptor.ViewPoint;
			}

			return gameObject.transform.Find("Head");
		}
	}
}

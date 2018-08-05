using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarBehaviour : MonoBehaviour
	{
		private IAvatarInput _avatarInput;

		private Transform _head;
		private Transform _body;
		private Transform _left;
		private Transform _right;

		private Vector3 _prevBodyPos;
		
		public void Init(IAvatarInput avatarInput)
		{
			_avatarInput = avatarInput;

			_head = GetHeadTransform();
			_body = gameObject.transform.Find("Body");
			_left = gameObject.transform.Find("LeftHand");
			_right = gameObject.transform.Find("RightHand");
		}

		private void LateUpdate()
		{
			var headPosRot = _avatarInput.HeadPosRot;
			var leftPosRot = _avatarInput.LeftPosRot;
			var rightPosRot = _avatarInput.RightPosRot;

			_head.position = headPosRot.Position;
			_head.rotation = headPosRot.Rotation;
			
			_left.position = leftPosRot.Position;
			_left.rotation = leftPosRot.Rotation;
			
			_right.position = rightPosRot.Position;
			_right.rotation = rightPosRot.Rotation;
			
			if (_body == null) return;
			_body.position = _head.position - (_head.transform.up * 0.1f);

			var vel = new Vector3(_body.transform.localPosition.x - _prevBodyPos.x, 0.0f, _body.localPosition.z - _prevBodyPos.z);

			var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
			var tiltAxis = Vector3.Cross(gameObject.transform.up, vel);
			_body.localRotation = Quaternion.Lerp(_body.localRotation,
				Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
				Time.deltaTime * 10.0f);

			_prevBodyPos = _body.transform.localPosition;
		}

		private Transform GetHeadTransform()
		{
			var descriptor = GetComponent<AvatarDescriptor>();
			if (descriptor != null)
			{
				if (descriptor.ViewPoint != null) return descriptor.ViewPoint;
			}

			return gameObject.transform.Find("Head");
		}
	}
}
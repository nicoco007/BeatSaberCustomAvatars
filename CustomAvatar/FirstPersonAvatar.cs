using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	/// <summary>
	/// This is a clone of the current avatar that is only shown in first person perspective.
	/// </summary>
	public class FirstPersonAvatar
	{
		private bool _enabled = true;
		private Animator _animatorRig;
		
		public FirstPersonAvatar(GameObject avatarGameObject, bool enabled = false)
		{
			ParentGameObject = avatarGameObject;
			CopyParentAvatar();
			Enabled = enabled;
		}
		
		public GameObject ParentGameObject { get; }
		public GameObject CloneGameObject { get; private set; }

		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled == value) return;
				_enabled = value;
				CloneGameObject.SetActive(_enabled);
			}
		}

		public void DestroyClone()
		{
			if (CloneGameObject != null)
			{
				Object.Destroy(CloneGameObject);
			}
		}

		private void CopyParentAvatar()
		{
			CloneGameObject = Object.Instantiate(ParentGameObject);
			AvatarLayers.SetChildrenToLayer(CloneGameObject, AvatarLayers.OnlyInFirstPerson);
			var clone = CloneGameObject.AddComponent<AvatarClone>();
			clone.Init(ParentGameObject);
			Object.DontDestroyOnLoad(CloneGameObject);

			_animatorRig = clone.GetComponentsInChildren<Animator>().FirstOrDefault(x => x.isHuman);
			if (_animatorRig != null)
			{
				_animatorRig.GetBoneTransform(HumanBodyBones.Head).localScale = Vector3.zero;
				return;
			}
			
			var head = clone.transform.Find("Head");
			if (head == null) return;
			head.localScale = Vector3.zero;
		}
	}
}
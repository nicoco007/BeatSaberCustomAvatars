﻿using System;
using System.Linq;
using AvatarScriptPack;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarClone : MonoBehaviour
	{
		private static readonly Type[] BannedTypes =
		{
			typeof(DynamicBone),
			typeof(DynamicBoneCollider),
			typeof(AudioSource),
			typeof(AvatarBehaviour),
			typeof(VRIK),
			typeof(IKManagerAdvanced),
			typeof(IKManager)
		};

		private Transform[] _parentTransforms;
		private Renderer[] _parentRenderers;
		private Transform[] _cloneTransforms;
		private Renderer[] _cloneRenderers;

		private bool _init;

		public GameObject ParentAvatar { get; private set; }
		
		public void Init(GameObject parentAvatar)
		{
			ParentAvatar = parentAvatar;

			_parentTransforms = ParentAvatar.GetComponentsInChildren<Transform>();
			_parentRenderers = ParentAvatar.GetComponentsInChildren<Renderer>();
			_cloneTransforms = GetComponentsInChildren<Transform>();
			_cloneRenderers = GetComponentsInChildren<Renderer>();

			_init = true;
		}

		private void Awake()
		{
			RemoveOtherComponents();
		}

		private void LateUpdate()
		{
			if (!_init || ParentAvatar == null) return;

			transform.position = ParentAvatar.transform.position;
			transform.rotation = ParentAvatar.transform.rotation;
			transform.localScale = ParentAvatar.transform.localScale;

			for (var i = 0; i < _parentTransforms.Length; i++)
			{
				if (_parentTransforms[i] == null) continue;
				_cloneTransforms[i].position = _parentTransforms[i].position;
				_cloneTransforms[i].rotation = _parentTransforms[i].rotation;
				//_cloneTransforms[i].localScale = _parentTransforms[i].localScale; //This overrides the head chopping, so hopefully no avatar is changing scale in animation
			}

			for (var i = 0; i < _parentRenderers.Length; i++)
			{
				if (_parentRenderers[i] == null) continue;
				_cloneRenderers[i].enabled = _parentRenderers[i].enabled;
				_cloneRenderers[i].sharedMaterials = _parentRenderers[i].sharedMaterials;
				
				var skin = _parentRenderers[i] as SkinnedMeshRenderer;
				var skinClone = _cloneRenderers[i] as SkinnedMeshRenderer;
				
				if (skin == null || skinClone == null) continue;
				
				var mesh = skin.sharedMesh;
				var meshClone = skinClone.sharedMesh;
				
				if (mesh == null || mesh.blendShapeCount == 0 || mesh.blendShapeCount != meshClone.blendShapeCount) continue;
				for (var j = 0; j < mesh.blendShapeCount; j++)
				{
					var weight = skin.GetBlendShapeWeight(j);
					skinClone.SetBlendShapeWeight(j, weight);
				}
			}
		}
		
		private void RemoveOtherComponents()
		{
			foreach (var component in GetComponentsInChildren<Component>(true))
			{
				if (component == null) continue;
				if (component == this) continue;

				if (IsComponentTypeAllowed(component)) continue;
				
				Destroy(component);
			}
		}

		private bool IsComponentTypeAllowed(Component component)
		{
			if (component == null) return true;
			var type = component.GetType();
			if (BannedTypes.Contains(type)) return false;
			
			foreach (var bannedType in BannedTypes)
			{
				if (type.IsSubclassOf(bannedType)) return false;
			}

			return true;
		}
	}
}
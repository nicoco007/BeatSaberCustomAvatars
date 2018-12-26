using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x0200016E RID: 366
	public class Hierarchy
	{
		// Token: 0x06000980 RID: 2432 RVA: 0x00042E04 File Offset: 0x00041204
		public static bool HierarchyIsValid(Transform[] bones)
		{
			for (int i = 1; i < bones.Length; i++)
			{
				if (!Hierarchy.IsAncestor(bones[i], bones[i - 1]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000981 RID: 2433 RVA: 0x00042E3C File Offset: 0x0004123C
		public static UnityEngine.Object ContainsDuplicate(UnityEngine.Object[] objects)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				for (int j = 0; j < objects.Length; j++)
				{
					if (i != j && objects[i] == objects[j])
					{
						return objects[i];
					}
				}
			}
			return null;
		}

		// Token: 0x06000982 RID: 2434 RVA: 0x00042E90 File Offset: 0x00041290
		public static bool IsAncestor(Transform transform, Transform ancestor)
		{
			return transform == null || ancestor == null || (!(transform.parent == null) && (transform.parent == ancestor || Hierarchy.IsAncestor(transform.parent, ancestor)));
		}

		// Token: 0x06000983 RID: 2435 RVA: 0x00042EEC File Offset: 0x000412EC
		public static bool ContainsChild(Transform transform, Transform child)
		{
			if (transform == child)
			{
				return true;
			}
			Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>();
			foreach (Transform x in componentsInChildren)
			{
				if (x == child)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000984 RID: 2436 RVA: 0x00042F38 File Offset: 0x00041338
		public static void AddAncestors(Transform transform, Transform blocker, ref Transform[] array)
		{
			if (transform.parent != null && transform.parent != blocker)
			{
				if (transform.parent.position != transform.position && transform.parent.position != blocker.position)
				{
					Array.Resize<Transform>(ref array, array.Length + 1);
					array[array.Length - 1] = transform.parent;
				}
				Hierarchy.AddAncestors(transform.parent, blocker, ref array);
			}
		}

		// Token: 0x06000985 RID: 2437 RVA: 0x00042FC8 File Offset: 0x000413C8
		public static Transform GetAncestor(Transform transform, int minChildCount)
		{
			if (transform == null)
			{
				return null;
			}
			if (!(transform.parent != null))
			{
				return null;
			}
			if (transform.parent.childCount >= minChildCount)
			{
				return transform.parent;
			}
			return Hierarchy.GetAncestor(transform.parent, minChildCount);
		}

		// Token: 0x06000986 RID: 2438 RVA: 0x0004301C File Offset: 0x0004141C
		public static Transform GetFirstCommonAncestor(Transform t1, Transform t2)
		{
			if (t1 == null)
			{
				return null;
			}
			if (t2 == null)
			{
				return null;
			}
			if (t1.parent == null)
			{
				return null;
			}
			if (t2.parent == null)
			{
				return null;
			}
			if (Hierarchy.IsAncestor(t2, t1.parent))
			{
				return t1.parent;
			}
			return Hierarchy.GetFirstCommonAncestor(t1.parent, t2);
		}

		// Token: 0x06000987 RID: 2439 RVA: 0x00043090 File Offset: 0x00041490
		public static Transform GetFirstCommonAncestor(Transform[] transforms)
		{
			if (transforms == null)
			{
				Debug.LogWarning("Transforms is null.");
				return null;
			}
			if (transforms.Length == 0)
			{
				Debug.LogWarning("Transforms.Length is 0.");
				return null;
			}
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i] == null)
				{
					return null;
				}
				if (Hierarchy.IsCommonAncestor(transforms[i], transforms))
				{
					return transforms[i];
				}
			}
			return Hierarchy.GetFirstCommonAncestorRecursive(transforms[0], transforms);
		}

		// Token: 0x06000988 RID: 2440 RVA: 0x00043104 File Offset: 0x00041504
		public static Transform GetFirstCommonAncestorRecursive(Transform transform, Transform[] transforms)
		{
			if (transform == null)
			{
				Debug.LogWarning("Transform is null.");
				return null;
			}
			if (transforms == null)
			{
				Debug.LogWarning("Transforms is null.");
				return null;
			}
			if (transforms.Length == 0)
			{
				Debug.LogWarning("Transforms.Length is 0.");
				return null;
			}
			if (Hierarchy.IsCommonAncestor(transform, transforms))
			{
				return transform;
			}
			if (transform.parent == null)
			{
				return null;
			}
			return Hierarchy.GetFirstCommonAncestorRecursive(transform.parent, transforms);
		}

		// Token: 0x06000989 RID: 2441 RVA: 0x0004317C File Offset: 0x0004157C
		public static bool IsCommonAncestor(Transform transform, Transform[] transforms)
		{
			if (transform == null)
			{
				Debug.LogWarning("Transform is null.");
				return false;
			}
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i] == null)
				{
					Debug.Log("Transforms[" + i + "] is null.");
					return false;
				}
				if (!Hierarchy.IsAncestor(transforms[i], transform) && transforms[i] != transform)
				{
					return false;
				}
			}
			return true;
		}
	}
}

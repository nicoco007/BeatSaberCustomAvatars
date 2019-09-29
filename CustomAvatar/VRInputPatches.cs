using Harmony;
using UnityEngine.XR;

namespace CustomAvatar.VRInputPatches
{
	[HarmonyPatch(typeof(VRControllersInputManager))]
	[HarmonyPatch("TriggerValue", MethodType.Normal)]
	class TriggerValuePatch
	{
		public static bool Prefix(XRNode node, ref float __result)
		{
			OpenVRInputManager inputManager = PersistentSingleton<OpenVRInputManager>.instance;

			if (node == XRNode.LeftHand)
			{
				__result = inputManager.LeftTrigger.GetAxisRaw();
			}
			else if (node == XRNode.RightHand)
			{
				__result = inputManager.RightTrigger.GetAxisRaw();
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(VRControllersInputManager))]
	[HarmonyPatch("MenuButtonDown", MethodType.Normal)]
	class MenuButtonDownPatch
	{
		public static bool Prefix(ref bool __result)
		{
			OpenVRInputManager inputManager = PersistentSingleton<OpenVRInputManager>.instance;

			__result = inputManager.Menu.GetButtonDown();

			return false;
		}
	}

	[HarmonyPatch(typeof(VRControllersInputManager))]
	[HarmonyPatch("MenuButton", MethodType.Normal)]
	class MenuButtonPatch
	{
		public static bool Prefix(ref bool __result)
		{
			OpenVRInputManager inputManager = PersistentSingleton<OpenVRInputManager>.instance;

			__result = inputManager.Menu.GetButton();

			return false;
		}
	}

	[HarmonyPatch(typeof(OpenVRHelper))]
	[HarmonyPatch("TriggerHapticPulse", MethodType.Normal)]
	class TriggerHapticPulsePatch
	{
		public static bool Prefix(XRNode node, float strength = 1f)
		{
			OpenVRInputManager inputManager = PersistentSingleton<OpenVRInputManager>.instance;

			if (node == XRNode.LeftHand)
			{
				inputManager.LeftSlice.TriggerHapticPulse(0.05f, strength);
			}
			else if (node == XRNode.RightHand)
			{
				inputManager.RightSlice.TriggerHapticPulse(0.05f, strength);
			}

			return false;
		}
	}
}

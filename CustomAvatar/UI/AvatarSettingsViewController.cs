using System.Linq;
using UnityEngine;
using VRUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using CustomUI.Settings;
using TMPro;
using CustomAvatar.UI;

namespace CustomAvatar
{
	class AvatarSettingsViewController : VRUIViewController
	{
		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			if (firstActivation) FirstActivation();
		}

		private void FirstActivation()
		{
			RectTransform container = new GameObject("AvatarSettingsContainer", typeof(RectTransform)).transform as RectTransform;
			container.SetParent(rectTransform, false);
			container.anchorMin = new Vector2(0.05f, 0.0f);
			container.anchorMax = new Vector2(0.95f, 1.0f);
			container.sizeDelta = new Vector2(0, 0);

			System.Action<RectTransform, float, float, float, float, float> relative_layout =
				(RectTransform rt, float x, float y, float w, float h, float pivotx) =>
				{
					rt.anchorMin = new Vector2(x, y);
					rt.anchorMax = new Vector2(x + w, y + h);
					rt.pivot = new Vector2(pivotx, 1f);
					rt.sizeDelta = Vector2.zero;
					rt.anchoredPosition = Vector2.zero;
				};

			TextMeshProUGUI text = BeatSaberUI.CreateText(container, "AVATAR SETTINGS", Vector2.zero);
			text.fontSize = 6.0f;
			text.alignment = TextAlignmentOptions.Center;
			relative_layout(text.rectTransform, 0f, 0.85f, 1f, 0.166f, 0.5f);

            var boolFirstPerson = AddBool("Visible In First Person View", container);
            relative_layout(boolFirstPerson.transform as RectTransform, 0, 0.66f, 1, 0.166f, 0);

            var boolRotatePreviewAvatar = AddBool("Rotate Avatar Preview", container);
            relative_layout(boolRotatePreviewAvatar.transform as RectTransform, 0, 0.55f, 1, 0.166f, 0);


            boolFirstPerson.GetValue += delegate
            {
                return Plugin.Instance.FirstPersonEnabled;
            };
            boolFirstPerson.SetValue += delegate (bool value)
            {
                Plugin.Instance.FirstPersonEnabled = value;
            };
            boolFirstPerson.Init();


            boolRotatePreviewAvatar.GetValue += delegate
            {
                return AvatarPreviewRotation.rotatePreview;
            };
            boolRotatePreviewAvatar.SetValue += delegate (bool value)
            {
                AvatarPreviewRotation.rotatePreview = value;
            };
            boolRotatePreviewAvatar.Init();
        }

		private class ImmediateBoolController : BoolViewController
		{
			public override void IncButtonPressed()
			{
				base.IncButtonPressed();
				ApplySettings();
			}
			public override void DecButtonPressed()
			{
				base.DecButtonPressed();
				ApplySettings();
			}
		}

		private BoolViewController AddBool(string name, Transform parent)
		{
			return AddSettingController<SwitchSettingsController, ImmediateBoolController>(name, parent);
		}

		private T AddSettingController<TORIG, T>(string name, Transform parent) where T : Behaviour
		{
			var volumeSettings = Resources.FindObjectsOfTypeAll<WindowModeSettingsController>().FirstOrDefault();
			GameObject newSettingsObject = MonoBehaviour.Instantiate(volumeSettings.gameObject, parent);
			newSettingsObject.name = name;

			WindowModeSettingsController volume = newSettingsObject.GetComponent<WindowModeSettingsController>();
			T newToggleSettingsController = (T)ReflectionUtil.CopyComponent(volume, typeof(TORIG), typeof(T), newSettingsObject);
			MonoBehaviour.DestroyImmediate(volume);

			newSettingsObject.GetComponentInChildren<TMP_Text>().text = name;

			return newToggleSettingsController;
		}
	}
}

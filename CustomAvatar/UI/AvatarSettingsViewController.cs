using UnityEngine;
using VRUI;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using TMPro;
using System.Collections.Generic;
using IPA.Utilities;

namespace CustomAvatar.UI
{
	class AvatarSettingsViewController : VRUIViewController
	{
		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			if (firstActivation) FirstActivation();
		}

		private void FirstActivation()
		{
			RectTransform containerRect = new GameObject("AvatarSettingsContainer", typeof(RectTransform)).transform as RectTransform;
			containerRect.SetParent(rectTransform, false);
			containerRect.anchorMin = new Vector2(0.05f, 0.0f);
			containerRect.anchorMax = new Vector2(0.95f, 1.0f);
			containerRect.sizeDelta = new Vector2(0, 0);

			SubMenu container = new SubMenu(containerRect);
			List<ListViewController> loadedSettings = new List<ListViewController>();

			System.Action<RectTransform, float, float, float, float, float, float> relative_layout =
				(RectTransform rt, float x, float y, float w, float h, float pivotx, float pivoty) =>
				{
					rt.anchorMin = new Vector2(x, y);
					rt.anchorMax = new Vector2(x + w, y + h);
					rt.pivot = new Vector2(pivotx, pivoty);
					rt.sizeDelta = Vector2.zero;
					rt.anchoredPosition = Vector2.zero;
				};

			gameObject.SetActive(false);

			TextMeshProUGUI text = BeatSaberUI.CreateText(containerRect, "AVATAR SETTINGS (Klouder is cute)", Vector2.zero);
			text.fontSize = 6.0f;
			text.alignment = TextAlignmentOptions.Center;
			relative_layout(text.rectTransform, 0f, 0.85f, 1f, 0.166f, 0.5f, 1f);

			var boolFirstPerson = container.AddList("Visible In First Person View", new float[] { 0, 1 });
			boolFirstPerson.applyImmediately = true;
			relative_layout(boolFirstPerson.transform as RectTransform, 0, 0.66f, 1, 0.166f, 0, 1f);
			BeatSaberUI.AddHintText(boolFirstPerson.transform as RectTransform, "Allows you to see the avatar inside of VR");

			var listResizePolicy = container.AddList("Resize Avatars To Player's", new float[] { 0, 1, 2 });
			listResizePolicy.applyImmediately = true;
			relative_layout(listResizePolicy.transform as RectTransform, 0, 0.55f, 1, 0.166f, 0, 1f);
			BeatSaberUI.AddHintText(listResizePolicy.transform as RectTransform, "Use 'Arms Length' to resize the avatar based on your proportions, 'Height' to resize based on your height, and 'Never' to not resize");

			var boolFloorMovePolicy = container.AddList("Floor Height Adjust", new float[] { 0, 1 });
			boolFloorMovePolicy.applyImmediately = true;
			relative_layout(boolFloorMovePolicy.transform as RectTransform, 0, 0.44f, 1, 0.166f, 0, 1f);
			BeatSaberUI.AddHintText(boolFloorMovePolicy.transform as RectTransform, "Move the floor to compensate for height when using 'Arms Length' resize, requires CustomPlatforms");

			var labelMeasure = BeatSaberUI.CreateText(containerRect, $"Hand To Hand Length = {Mathf.Ceil(AvatarManager.Instance.AvatarTailor.PlayerArmLength * 100.0f) / 100.0f}", Vector2.zero);
			relative_layout(labelMeasure.transform as RectTransform, 0f, 0.18f, 0.5f, 0.11f, 0, .5f);
			BeatSaberUI.AddHintText(labelMeasure.transform as RectTransform, "Value used for 'Arms Length' resize, press on the 'MEASURE!' button and T-Pose");
			labelMeasure.fontSize = 5f;
			labelMeasure.alignment = TextAlignmentOptions.MidlineLeft;

			gameObject.SetActive(true);

			var buttonMeasure = BeatSaberUI.CreateUIButton(containerRect, "QuitButton", () =>
			{
				labelMeasure.text = "Measuring ...";
				AvatarManager.Instance.AvatarTailor.MeasurePlayerArmSpan((value) =>
				{
					labelMeasure.text = $"Measuring ... {Mathf.Ceil(value * 100.0f) / 100.0f}";
				},
				(result) =>
				{
					labelMeasure.text = $"Hand To Hand Length = {Mathf.Ceil(result * 100.0f) / 100.0f}";
					if (AvatarManager.Instance.AvatarTailor.ResizePolicy == AvatarTailor.ResizePolicyType.AlignArmLength)
						AvatarManager.Instance.ResizePlayerAvatar();
				});
			}, "Measure!");
			relative_layout(buttonMeasure.transform as RectTransform, 0.65f, 0.18f, 0.35f, 0.11f, .5f, .5f);
			BeatSaberUI.AddHintText(buttonMeasure.transform as RectTransform, "Press this and T-Pose to measure your arms, needed to use 'Arms Length' resize");

			boolFirstPerson.GetTextForValue = (value) => (value != 0f) ? "ON" : "OFF";
			boolFirstPerson.GetValue = () => Plugin.Instance.FirstPersonEnabled ? 1f : 0f;
			boolFirstPerson.SetValue = (value) => Plugin.Instance.FirstPersonEnabled = value != 0f;
			boolFirstPerson.Init();
			loadedSettings.Add(boolFirstPerson);

			listResizePolicy.GetTextForValue = (value) => new string[] { "Arms Length", "Height", "Never" }[(int)value];
			listResizePolicy.GetValue = () => (int)AvatarManager.Instance.AvatarTailor.ResizePolicy;
			listResizePolicy.SetValue = (value) =>
			{
				AvatarManager.Instance.AvatarTailor.ResizePolicy = (AvatarTailor.ResizePolicyType)(int)value;
				AvatarManager.Instance.ResizePlayerAvatar();
			};
			listResizePolicy.Init();
			loadedSettings.Add(listResizePolicy);

			boolFloorMovePolicy.GetTextForValue = (value) => (value != 0f) ? "ON" : "OFF";
			boolFloorMovePolicy.GetValue = () => AvatarManager.Instance.AvatarTailor.FloorMovePolicy == AvatarTailor.FloorMovePolicyType.AllowMove ? 1f : 0f;
			boolFloorMovePolicy.SetValue = (value) =>
			{
				AvatarManager.Instance.AvatarTailor.FloorMovePolicy = (value != 0f) ? AvatarTailor.FloorMovePolicyType.AllowMove : AvatarTailor.FloorMovePolicyType.NeverMove;
				AvatarManager.Instance.ResizePlayerAvatar();
			};
			boolFloorMovePolicy.Init();
			loadedSettings.Add(boolFloorMovePolicy);

			foreach (ListViewController list in loadedSettings)
			{
				list.InvokePrivateMethod("OnDisable", new object[] { });
				list.InvokePrivateMethod("OnEnable", new object[] { });
				Plugin.Logger.Debug("Reset " + list.name);
			}
		}
	}
}

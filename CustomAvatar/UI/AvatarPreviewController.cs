using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using VRUI;
using TMPro;
using CustomUI.BeatSaber;


namespace CustomAvatar
{
	public class AvatarPreviewController : VRUIViewController
	{
		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			base.DidActivate(firstActivation, activationType);
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

			TextMeshProUGUI text = BeatSaberUI.CreateText(container, "Preview", Vector2.zero);
			text.fontSize = 6.0f;
			text.alignment = TextAlignmentOptions.Center;
			relative_layout(text.rectTransform, 0f, 0.85f, 1f, 0.166f, 0.5f);
		}
	}
}

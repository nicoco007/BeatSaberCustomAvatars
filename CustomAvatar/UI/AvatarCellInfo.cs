using UnityEngine;

namespace CustomAvatar
{
	class AvatarCellInfo : IPreviewBeatmapLevel
	{
		#region Aliases

		public string name { get; set; }
		public string subName { get; set; }
		public string authorName { get; set; }

		#endregion

		#region Properties

		public string levelID => null;
		public string songName => name;
		public string songSubName => subName;
		public string songAuthorName => authorName;
		public string levelAuthorName => null;
		public float beatsPerMinute => 0f;
		public float songTimeOffset => 0f;
		public float shuffle => 0f;
		public float shufflePeriod => 0f;
		public AudioClip previewAudioClip => null;
		public float previewStartTime => 0f;
		public float previewDuration => 0f;
		public float songDuration => 0f;
		public Sprite coverImage { get; set; }
		public SceneInfo environmentSceneInfo => null;
		public BeatmapCharacteristicSO[] beatmapCharacteristics => new BeatmapCharacteristicSO[0];

		#endregion
	}
}

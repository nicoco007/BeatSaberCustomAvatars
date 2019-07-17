using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using HMUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using TMPro;
using System.Collections.Generic;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	class AvatarListViewController : VRUIViewController, TableView.IDataSource
	{
		private Button _backButton;
		private Button _pageUpButton;
		private Button _pageDownButton;
		private TextMeshProUGUI _versionNumber;
		private TableView _tableView;
		private LevelListTableCell _tableCellTemplate;
		public GameObject _avatarPreview;
		private GameObject _previewParent;
		private GameObject PreviewAvatar;
		private AvatarScriptPack.FirstPersonExclusion _exclusionScript;
		private AvatarScriptPack.VRIK _VRIK;
		private float _previewHeight;
		private float _previewHeightOffset;
		private float _previewScale;
		private Vector3 _center = Vector3.zero;
		private IReadOnlyList<CustomAvatar> AvatarList = Plugin.Instance.AvatarLoader.Avatars;
		private int LastAvatar = -1;
		private int CurrentAvatar;
		private int AvatarIndex;
		public GameObject[] __AvatarPrefabs;
		public string[] __AvatarNames;
		public string[] __AvatarAuthors;
		public string[] __AvatarPaths;
		public Sprite[] __AvatarCovers;
		public AvatarLoadResult[] __AvatarLoadResults;
		private bool PreviewStatus;
		private int _loadedCount = 0;

		public Action onBackPressed;

		protected override void DidActivate(bool firstActivation, ActivationType type)
		{
			if (firstActivation) FirstActivation();

			SelectRowWithAvatar(Plugin.Instance.PlayerAvatarManager.CurrentPlayerAvatar, false, true);

			Plugin.Instance.PlayerAvatarManager.AvatarChanged += OnAvatarChanged;
			PreviewCurrent();
		}

		private void PreviewCurrent()
		{
			CurrentAvatar = Plugin.Instance.AvatarLoader.IndexOf(Plugin.Instance.PlayerAvatarManager.CurrentPlayerAvatar);
		}

		protected override void DidDeactivate(DeactivationType deactivationType)
		{
			Plugin.Instance.PlayerAvatarManager.AvatarChanged -= OnAvatarChanged;
		}

		private void OnAvatarChanged(CustomAvatar avatar)
		{
			SelectRowWithAvatar(avatar, true, false);
			PreviewCurrent();
		}

		private void SelectRowWithAvatar(CustomAvatar avatar, bool reload, bool scroll)
		{
			int currentRow = Plugin.Instance.AvatarLoader.IndexOf(avatar);
			if (scroll) _tableView.ScrollToCellWithIdx(currentRow, TableView.ScrollPositionType.Center, false);
			if (reload) _tableView.ReloadData();
			_tableView.SelectCellWithIdx(currentRow);
		}

		public void LoadAllAvatars()
		{
			int _AvatarIndex = 0;
			__AvatarPrefabs = new GameObject[AvatarList.Count()];
			__AvatarNames = new string[AvatarList.Count()];
			__AvatarAuthors = new string[AvatarList.Count()];
			__AvatarPaths = new string[AvatarList.Count()];
			__AvatarCovers = new Sprite[AvatarList.Count()];
			__AvatarLoadResults = new AvatarLoadResult[AvatarList.Count()];

			for (int i = 0; i < AvatarList.Count(); i++)
			{
				_AvatarIndex = i;
				var avatar = AvatarList[_AvatarIndex];
				try
				{
#if DEBUG
					Logger.Log("AddToArray -> " + _AvatarIndex);
#endif
					avatar.Load(AddToArray);
#if DEBUG
					Logger.Log("AddToArray => " + _AvatarIndex + " (" + Plugin.Instance.AvatarLoader.IndexOf(avatar) + ") | " + avatar.FullPath);
#endif
				}
				catch (Exception e)
				{
#if DEBUG
					Logger.Log(_AvatarIndex + " | " + e);
#endif
				}
			}

			void AddToArray(CustomAvatar avatar, AvatarLoadResult _loadResult)
			{
#if DEBUG
				Logger.Log("AddToArray == " + AvatarLoadResult.Completed);
#endif
				if (_loadResult != AvatarLoadResult.Completed)
				{
					Logger.Log("Avatar " + avatar.FullPath + " failed to load");
					return;
				}
				AvatarIndex = Plugin.Instance.AvatarLoader.IndexOf(avatar);

				__AvatarNames[AvatarIndex] = avatar.Name;
				__AvatarAuthors[AvatarIndex] = avatar.AuthorName;
				__AvatarCovers[AvatarIndex] = avatar.CoverImage;
				__AvatarPaths[AvatarIndex] = avatar.FullPath;
				__AvatarPrefabs[AvatarIndex] = avatar.GameObject;
				__AvatarLoadResults[AvatarIndex] = _loadResult;

				_loadedCount++;
#if DEBUG
				Logger.Log("(" + _loadedCount + "/" + ((int)AvatarList.Count()) + ") #" + AvatarIndex);
#endif
				//if (_loadedCount == (AvatarList.Count()))
				if (true)
				{
					_tableView.ReloadData();
					PreviewCurrent();
				}
			}
		}

		private void FirstActivation()
		{
			LoadAllAvatars();

			_tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");

			RectTransform container = new GameObject("AvatarsListContainer", typeof(RectTransform)).transform as RectTransform;
			container.SetParent(rectTransform, false);
			container.sizeDelta = new Vector2(70f, 0f);


			var tableViewObject = new GameObject("AvatarsListTableView");
			tableViewObject.SetActive(false);
			_tableView = tableViewObject.AddComponent<TableView>();
			_tableView.gameObject.AddComponent<RectMask2D>();
			_tableView.transform.SetParent(container, false);

			(_tableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
			(_tableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
			(_tableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
			(_tableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

			_tableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
			_tableView.SetPrivateField("_isInitialized", false);
			_tableView.dataSource = this;

			_tableView.didSelectCellWithIdxEvent += _TableView_DidSelectRowEvent;

			tableViewObject.SetActive(true);

			_pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), container, false);
			(_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 40f);
			_pageUpButton.interactable = true;
			_pageUpButton.onClick.AddListener(delegate ()
			{
				_tableView.PageScrollUp();
			});

			_pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), container, false);
			(_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -30f);
			_pageDownButton.interactable = true;
			_pageDownButton.onClick.AddListener(delegate ()
			{
				_tableView.PageScrollDown();
			});

			_versionNumber = BeatSaberUI.CreateText(rectTransform, Plugin.Instance.Version, new Vector2(-10f, 10f));
			(_versionNumber.transform as RectTransform).anchorMax = new Vector2(1f, 0f);
			(_versionNumber.transform as RectTransform).anchorMin = new Vector2(1f, 0f);
			_versionNumber.fontSize = 5;
			_versionNumber.color = Color.white;

			if (_backButton == null)
			{
				_backButton = BeatSaberUI.CreateBackButton(rectTransform as RectTransform);

				_backButton.onClick.AddListener(delegate ()
				{
					onBackPressed();
				});
			}
		}

		private void _TableView_DidSelectRowEvent(TableView sender, int row)
		{
			Plugin.Instance.PlayerAvatarManager.SwitchToAvatar(Plugin.Instance.AvatarLoader.Avatars[row]);
			//GeneratePreview(row);
			LastAvatar = row;
		}

		TableCell TableView.IDataSource.CellForIdx(int row)
		{
			LevelListTableCell tableCell = _tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;
			if (tableCell == null)
			{
				tableCell = Instantiate(_tableCellTemplate);

				// remove level type icons
				tableCell.transform.Find("LevelTypeIcon0").gameObject.SetActive(false);
				tableCell.transform.Find("LevelTypeIcon1").gameObject.SetActive(false);
				tableCell.transform.Find("LevelTypeIcon2").gameObject.SetActive(false);

				tableCell.reuseIdentifier = "AvatarListCell";
			}

			var cellInfo = new AvatarCellInfo();

			if (__AvatarLoadResults[row] != AvatarLoadResult.Completed)
			{
				cellInfo.name = System.IO.Path.GetFileName(AvatarList[row].FullPath) +" failed to load";
				cellInfo.authorName = "Make sure it's not a duplicate avatar.";
				cellInfo.rawImageTexture = null;
			}
			else
			{
				try
				{
					cellInfo.name = __AvatarNames[row];
					cellInfo.authorName = __AvatarAuthors[row];
					cellInfo.rawImageTexture = __AvatarCovers[row] ? __AvatarCovers[row].texture : Texture2D.blackTexture;
				}
				catch (Exception e)
				{
					cellInfo.name = "If you see this yell at Assistant";
					cellInfo.authorName = "because she fucked up";
					cellInfo.rawImageTexture = Texture2D.blackTexture;
					Logger.Log(e.StackTrace, Logger.LogLevel.Error);
				}
			}

			tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
			tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);

			tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = cellInfo.name;
			tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = cellInfo?.authorName;
			tableCell.GetPrivateField<UnityEngine.UI.RawImage>("_coverRawImage").texture = cellInfo.rawImageTexture;

			return tableCell;
		}

		int TableView.IDataSource.NumberOfCells()
		{
			return AvatarList.Count;
		}

		float TableView.IDataSource.CellSize()
		{
			return 8.5f;
		}
	}
}

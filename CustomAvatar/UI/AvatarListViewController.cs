using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using HMUI;
using CustomUI.BeatSaber;
using TMPro;
using System.Collections.Generic;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	class AvatarListViewController : VRUIViewController, TableView.IDataSource
	{
		private Button _backButton;
		private TableView _tableView;
		private LevelListTableCell _tableCellTemplate;
		private IReadOnlyList<CustomAvatar> AvatarList = Plugin.Instance.AvatarLoader.Avatars;
		private readonly List<CustomAvatar> _loadedAvatars = new List<CustomAvatar>();

		public Action onBackPressed;

		protected override void DidActivate(bool firstActivation, ActivationType type)
		{
			if (firstActivation) FirstActivation();

			SelectRowWithAvatar(Plugin.Instance.PlayerAvatarManager.GetCurrentAvatar(), false, true);

			Plugin.Instance.PlayerAvatarManager.AvatarChanged += OnAvatarChanged;
		}

		protected override void DidDeactivate(DeactivationType deactivationType)
		{
			Plugin.Instance.PlayerAvatarManager.AvatarChanged -= OnAvatarChanged;
		}

		private void OnAvatarChanged(CustomAvatar avatar)
		{
			SelectRowWithAvatar(avatar, true, false);
		}

		private void SelectRowWithAvatar(CustomAvatar avatar, bool reload, bool scroll)
		{
			int currentRow =  _loadedAvatars.IndexOf(avatar);
			if (scroll) _tableView.ScrollToCellWithIdx(currentRow, TableView.ScrollPositionType.Center, false);
			if (reload) _tableView.ReloadData();
			_tableView.SelectCellWithIdx(currentRow);
		}

		public void LoadAllAvatars()
		{
			_loadedAvatars.Clear();

			for (int i = 0; i < AvatarList.Count; i++)
			{
				var avatar = AvatarList[i];

				try
				{
					Logger.Log("Loading avatar from " + avatar.AssetBundle.FullPath, Logger.LogLevel.Notice);
					avatar.Load(OnAvatarLoaded);
				}
				catch (Exception e)
				{
					Logger.Log("Failed to load avatar: " + e.Message, Logger.LogLevel.Error);
				}
			}
		}

		private void OnAvatarLoaded(CustomAvatar avatar, AvatarLoadResult _loadResult)
		{
			if (_loadResult != AvatarLoadResult.Completed)
			{
				Logger.Log("Avatar " + avatar.FullPath + " failed to load");
				return;
			}

			_loadedAvatars.Add(avatar);
			_loadedAvatars.Sort((a, b) => string.Compare(a.Name, b.Name));

			_tableView.ReloadData();
		}

		private void FirstActivation()
		{
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

			Button pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), container, false);
			(pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 40f);
			pageUpButton.interactable = true;
			pageUpButton.onClick.AddListener(delegate ()
			{
				_tableView.PageScrollUp();
			});

			Button pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), container, false);
			(pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -30f);
			pageDownButton.interactable = true;
			pageDownButton.onClick.AddListener(delegate ()
			{
				_tableView.PageScrollDown();
			});

			TextMeshProUGUI versionNumber = BeatSaberUI.CreateText(rectTransform, Plugin.Instance.Version, new Vector2(-10f, 10f));
			(versionNumber.transform as RectTransform).anchorMax = new Vector2(1f, 0f);
			(versionNumber.transform as RectTransform).anchorMin = new Vector2(1f, 0f);
			versionNumber.fontSize = 5;
			versionNumber.color = Color.white;

			if (_backButton == null)
			{
				_backButton = BeatSaberUI.CreateBackButton(rectTransform as RectTransform);

				_backButton.onClick.AddListener(delegate ()
				{
					onBackPressed();
				});
			}

			LoadAllAvatars();
		}

		private void _TableView_DidSelectRowEvent(TableView sender, int row)
		{
			Plugin.Instance.PlayerAvatarManager.SwitchToAvatar(_loadedAvatars[row]);
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

			tableCell.SetText(_loadedAvatars[row]?.Name ?? "No Name");
			tableCell.SetSubText(_loadedAvatars[row]?.AuthorName ?? "Unknown Author");
			tableCell.SetIcon(_loadedAvatars[row]?.CoverImage?.texture ?? Texture2D.blackTexture);

			tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
			tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);

			return tableCell;
		}

		int TableView.IDataSource.NumberOfCells()
		{
			return _loadedAvatars.Count;
		}

		float TableView.IDataSource.CellSize()
		{
			return 8.5f;
		}
	}
}

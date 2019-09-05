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
		private IReadOnlyList<CustomAvatar> AvatarList = Plugin.Instance.AvatarLoader.Avatars;

		public Action onBackPressed;

		protected override void DidActivate(bool firstActivation, ActivationType type)
		{
			if (firstActivation) FirstActivation();

			SelectRowWithAvatar(Plugin.Instance.PlayerAvatarManager.CurrentPlayerAvatar, false, true);

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
			int currentRow = Plugin.Instance.AvatarLoader.IndexOf(avatar);
			if (scroll) _tableView.ScrollToCellWithIdx(currentRow, TableViewScroller.ScrollPositionType.Center, false);
			if (reload) _tableView.ReloadData();
			_tableView.SelectCellWithIdx(currentRow);
		}

		private void LoadAllAvatars()
		{
			for (int i = 0; i < AvatarList.Count; i++)
			{
				AvatarList[i].Load(OnAvatarLoaded);
			}
		}

		private void OnAvatarLoaded(CustomAvatar avatar, AvatarLoadResult loadResult)
		{
			if (loadResult != AvatarLoadResult.Completed)
			{
				Plugin.Logger.Error("Avatar " + avatar.FullPath + " failed to load");
				return;
			}

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

			var scrollRect = tableViewObject.AddComponent<ScrollRect>();
			scrollRect.viewport = tableViewObject.transform as RectTransform;

			_tableView = tableViewObject.AddComponent<TableView>();
			_tableView.gameObject.AddComponent<RectMask2D>();
			_tableView.transform.SetParent(container, false);

			(_tableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
			(_tableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
			(_tableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
			(_tableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

			_tableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);

			_pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), container, false);
			(_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 40f);
			_tableView.SetPrivateField("_pageUpButton", _pageUpButton);

			_pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), container, false);
			(_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -30f);
			_tableView.SetPrivateField("_pageDownButton", _pageDownButton);

			_tableView.dataSource = this;
			_tableView.didSelectCellWithIdxEvent += _TableView_DidSelectRowEvent;

			tableViewObject.SetActive(true);

			
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

			LoadAllAvatars();
		}

		private void _TableView_DidSelectRowEvent(TableView sender, int row)
		{
			Plugin.Instance.PlayerAvatarManager.SwitchToAvatar(Plugin.Instance.AvatarLoader.Avatars[row]);
		}

		TableCell TableView.IDataSource.CellForIdx(TableView tableView, int row)
		{
			LevelListTableCell tableCell = _tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;
			if (tableCell == null)
			{
				tableCell = Instantiate(_tableCellTemplate);
				tableCell.reuseIdentifier = "AvatarListCell";
			}

			var cellInfo = new AvatarCellInfo();
			CustomAvatar avatar = AvatarList[row];

			if (!avatar.IsLoaded)
			{
				cellInfo.Name = System.IO.Path.GetFileName(avatar.FullPath) + " failed to load";
				cellInfo.AuthorName = "Make sure it's not a duplicate avatar.";
				cellInfo.RawImageTexture = Texture2D.blackTexture;
			}
			else
			{
				cellInfo.Name = avatar.Name;
				cellInfo.AuthorName = avatar.AuthorName;
				cellInfo.RawImageTexture = avatar.CoverImage ? avatar.CoverImage.texture : Texture2D.blackTexture;
			}

			tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
			tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);

			tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = cellInfo.Name;
			tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = cellInfo?.AuthorName;
			tableCell.GetPrivateField<UnityEngine.UI.RawImage>("_coverRawImage").texture = cellInfo.RawImageTexture;

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

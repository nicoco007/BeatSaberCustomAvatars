using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using HMUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using TMPro;

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
			int currentRow = Plugin.Instance.AvatarLoader.IndexOf(avatar);
			if (scroll) _tableView.ScrollToRow(currentRow, false);
			if (reload) _tableView.ReloadData();
			_tableView.SelectRow(currentRow);
		}

		private void FirstActivation()
		{
			try
			{
				_tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");

				RectTransform container = new GameObject("AvatarsListContainer", typeof(RectTransform)).transform as RectTransform;
				container.SetParent(rectTransform, false);
				container.sizeDelta = new Vector2(70f, 0f);

				_tableView = new GameObject("AvatarsListTableView").AddComponent<TableView>();
				_tableView.gameObject.AddComponent<RectMask2D>();
				_tableView.transform.SetParent(container, false);

				(_tableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
				(_tableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
				(_tableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
				(_tableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

				_tableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
				_tableView.SetPrivateField("_isInitialized", false);
				_tableView.dataSource = this;

				_tableView.didSelectRowEvent += _TableView_DidSelectRowEvent;

				_pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), container, false);
				(_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 30f);
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
			} catch (Exception e)
			{
				Plugin.Log("" + e);
			}
		}

		private void _TableView_DidSelectRowEvent(TableView sender, int row)
		{
			Plugin.Instance.PlayerAvatarManager.SwitchToAvatar(Plugin.Instance.AvatarLoader.Avatars[row]);
		}

		TableCell TableView.IDataSource.CellForRow(int row)
		{
			LevelListTableCell tableCell = _tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;
			if (tableCell == null)
			{
				tableCell = Instantiate(_tableCellTemplate);
				tableCell.reuseIdentifier = "AvatarListCell";
			}

			var avatar = Plugin.Instance.AvatarLoader.Avatars[row];
			if (avatar.IsLoaded)
			{
				tableCell.songName = avatar.Name;
				tableCell.author = avatar.AuthorName;
				tableCell.coverImage = avatar.CoverImage;
			}
			else
			{
				tableCell.songName = System.IO.Path.GetFileName(avatar.FullPath);
				tableCell.author = "";
				tableCell.coverImage = null;
			}
			

			return tableCell;
		}

		int TableView.IDataSource.NumberOfRows()
		{
			return Plugin.Instance.AvatarLoader.Avatars.Count;
		}

		float TableView.IDataSource.RowHeight()
		{
			return 10f;
		}
	}
}

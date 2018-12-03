using System;
using System.Collections;
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
		private int PreviewStatus;
		private int _loadedCount = 0;




		public Action onBackPressed;

		protected override void DidActivate(bool firstActivation, ActivationType type)
		{
			if (firstActivation) FirstActivation();

			SelectRowWithAvatar(Plugin.Instance.PlayerAvatarManager.GetCurrentAvatar(), false, true);

			Plugin.Instance.PlayerAvatarManager.AvatarChanged += OnAvatarChanged;
			PreviewCurrent();
		}

		private void PreviewCurrent()
		{
			CurrentAvatar = PathToInt(Plugin.Instance.PlayerAvatarManager.GetCurrentAvatar().FullPath);
			GeneratePreview(CurrentAvatar);
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
			if (scroll) _tableView.ScrollToRow(currentRow, false);
			if (reload) _tableView.ReloadData();
			_tableView.SelectRow(currentRow);
		}

		private int PathToInt(string path)
		{
			for (int i = 0; i < AvatarList.Count; i++)
				if (AvatarList[i].FullPath == path)
					return i;
			return -1;
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
					avatar.Load(AddToArray);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
			void AddToArray(CustomAvatar avatar, AvatarLoadResult _loadResult)
			{
				if (_loadResult != AvatarLoadResult.Completed)
				{
					Plugin.Log("Avatar " + avatar.FullPath + " failed to load");
					return;
				}
				AvatarIndex = PathToInt(avatar.FullPath);

				__AvatarNames[AvatarIndex] = avatar.Name;
				__AvatarAuthors[AvatarIndex] = avatar.AuthorName;
				__AvatarCovers[AvatarIndex] = avatar.CoverImage;
				__AvatarPaths[AvatarIndex] = avatar.FullPath;
				__AvatarPrefabs[AvatarIndex] = avatar.GameObject;
				__AvatarLoadResults[AvatarIndex] = _loadResult;

				_loadedCount++;
				if (_loadedCount == AvatarList.Count())
				{
					_tableView.ReloadData();
					PreviewCurrent();
				}
			}
		}

		private void FirstActivation()
		{
			try
			{

				LoadAllAvatars();

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
						DestroyPreview();
					});
				}
			} catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private void _TableView_DidSelectRowEvent(TableView sender, int row)
		{
			Plugin.Instance.PlayerAvatarManager.SwitchToAvatar(Plugin.Instance.AvatarLoader.Avatars[row]);
			GeneratePreview(row);
			LastAvatar = row;
		}

		public void DestroyPreview()
		{
			Destroy(_avatarPreview);
			PreviewAvatar = null;
			Destroy(_previewParent);
		}

		TableCell TableView.IDataSource.CellForRow(int row)
		{
			LevelListTableCell tableCell = _tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;
			if (tableCell == null)
			{
				tableCell = Instantiate(_tableCellTemplate);
				tableCell.reuseIdentifier = "AvatarListCell";
			}
			try
			{
				tableCell.songName = __AvatarNames[row];
				tableCell.author = __AvatarAuthors[row];
				tableCell.coverImage = __AvatarCovers[row];
			}
			catch
			{
				tableCell.songName = "If you see this yell at Assistant";
				tableCell.author = "because she fucked up";
				tableCell.coverImage = Sprite.Create(Texture2D.blackTexture, new Rect(), Vector2.zero);
			}
			return tableCell;
		}


		public void GeneratePreview(int AvatarIndex)
		{
			if (PreviewStatus == 1)
			{
				return;
			}
			PreviewStatus = 1;
			if (PreviewAvatar != null)
			{
				DestroyPreview();
			}

			if (__AvatarLoadResults[AvatarIndex] == AvatarLoadResult.Completed)
			{
				PreviewAvatar = __AvatarPrefabs[AvatarIndex];

				_previewParent = new GameObject();
				_previewParent.transform.Translate(2f, 0, 1f);
				_previewParent.transform.Rotate(0, -120, 0);
				_avatarPreview = Instantiate(PreviewAvatar, _previewParent.transform);

				_VRIK = _avatarPreview.GetComponentsInChildren<AvatarScriptPack.VRIK>().FirstOrDefault();

				if (_VRIK != null)
				{
					//_center = _avatarPreview.GetComponentInChildren<Renderer>().bounds.center;
					_previewHeight = _avatarPreview.GetComponentInChildren<Renderer>().bounds.size.y;
					//_previewHeightOffset = _avatarPreview.GetComponentInChildren<Renderer>().bounds.min.y;
					_previewHeightOffset = 0;
					_previewScale = (1f / _previewHeight);
				}
				else
				{
					foreach (Transform child in _avatarPreview.transform)
					{
						_center += child.gameObject.GetComponentInChildren<Renderer>().bounds.center;
					}
					_center /= _avatarPreview.transform.childCount;

					Bounds bounds = new Bounds(_center, Vector3.zero);

					foreach (Transform child in _avatarPreview.transform)
					{
						bounds.Encapsulate(child.gameObject.GetComponentInChildren<Renderer>().bounds);
					}

					_previewHeight = bounds.size.y;
					_previewHeightOffset = bounds.min.y;
					_previewScale = (1f / _previewHeight);
				}

				_previewParent.transform.Translate(0, 0.85f - (_previewHeightOffset), 0);
				_previewParent.transform.localScale = new Vector3(_previewScale, _previewScale, _previewScale);

				Destroy(_avatarPreview);
				_avatarPreview = Instantiate(PreviewAvatar, _previewParent.transform);
				_VRIK = _avatarPreview.GetComponentsInChildren<AvatarScriptPack.VRIK>().FirstOrDefault();
				_exclusionScript = _avatarPreview.GetComponentsInChildren<AvatarScriptPack.FirstPersonExclusion>().FirstOrDefault();

				if (_VRIK != null)
				{
					Destroy(_VRIK);
				}
				else
				{
					_avatarPreview.transform.Find("LeftHand").transform.Translate(-0.333f, -0.475f, 0);
					_avatarPreview.transform.Find("LeftHand").transform.Rotate(0, 0, -30);
					_avatarPreview.transform.Find("RightHand").transform.Translate(0.333f, -0.475f, 0);
					_avatarPreview.transform.Find("RightHand").transform.Rotate(0, 0, 30);
				}
				if (_exclusionScript != null)
				{
					_exclusionScript.SetVisible();
				}
			}
			else
			{
				Console.WriteLine("Failed to load preview. Status " + __AvatarLoadResults[AvatarIndex]);
			}
			PreviewStatus = 0;
		}

		int TableView.IDataSource.NumberOfRows()
		{
			return AvatarList.Count;
		}

		float TableView.IDataSource.RowHeight()
		{
			return 10f;
		}
	}
}

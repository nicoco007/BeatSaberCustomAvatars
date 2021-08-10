//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using HMUI;
using IPA.Utilities;
using Polyglot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarListViewController : ViewController, TableView.IDataSource
    {
        private const string kTableCellReuseIdentifier = "AvatarListTableCell";

        private PlayerAvatarManager _avatarManager;
        private DiContainer _container;
        private MirrorViewController _mirrorViewController;
        private PlayerOptionsViewController _playerOptionsViewController;
        private LevelCollectionViewController _levelCollectionViewController;
        private PlatformLeaderboardViewController _leaderboardViewController;

        private TableView _tableView;
        private GameObject _loadingIndicator;

        private readonly List<AvatarListItem> _avatars = new List<AvatarListItem>();
        private AvatarListTableCell _tableCellPrefab;

        private Texture2D _atlas;
        private Sprite _blankAvatarSprite;
        private Sprite _noAvatarSprite;
        private Sprite _reloadSprite;

        private bool _isLoading
        {
            get => _loadingIndicator.activeSelf;
            set => _loadingIndicator.SetActive(value);
        }

        [Inject]
        internal void Construct(PlayerAvatarManager avatarManager, DiContainer container, MirrorViewController mirrorViewController, PlayerOptionsViewController playerOptionsViewController, LevelCollectionViewController levelCollectionViewController, PlatformLeaderboardViewController leaderboardViewController)
        {
            _avatarManager = avatarManager;
            _container = container;
            _mirrorViewController = mirrorViewController;
            _playerOptionsViewController = playerOptionsViewController;
            _levelCollectionViewController = levelCollectionViewController;
            _leaderboardViewController = leaderboardViewController;
        }

        protected void Start()
        {
            using (Stream fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomAvatar.Resources.ui.dds"))
            {
                _atlas = SimpleDdsLoader.LoadImage(fileStream);
                _noAvatarSprite = Sprite.Create(_atlas, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
                _blankAvatarSprite = Sprite.Create(_atlas, new Rect(256, 0, 256, 256), new Vector2(0.5f, 0.5f));
                _reloadSprite = Sprite.Create(_atlas, new Rect(0, 256, 128, 128), new Vector2(0.5f, 0.5f));
            }
        }

        protected override void OnDestroy()
        {
            Destroy(_noAvatarSprite);
            Destroy(_blankAvatarSprite);
            Destroy(_reloadSprite);
            Destroy(_atlas);

            base.OnDestroy();
        }

        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                _tableCellPrefab = CreateTableCellPrefab();

                CreateTableView();
                CreateRefreshButton();
            }

            _avatarManager.avatarChanged += OnAvatarChanged;
            _avatarManager.avatarAdded += OnAvatarAdded;
            _avatarManager.avatarRemoved += OnAvatarRemoved;

            await ReloadAvatars();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarAdded -= OnAvatarAdded;
            _avatarManager.avatarRemoved -= OnAvatarRemoved;
        }

        private AvatarListTableCell CreateTableCellPrefab()
        {
            GameObject gameObject = Instantiate(_levelCollectionViewController.transform.Find("LevelsTableView/TableView/Viewport/Content/LevelListTableCell").gameObject);
            gameObject.name = "AvatarListTableCell";

            LevelListTableCell originalTableCell = gameObject.GetComponent<LevelListTableCell>();

            AvatarListTableCell tableCell = gameObject.AddComponent<AvatarListTableCell>();
            tableCell.Init(originalTableCell);

            DestroyImmediate(originalTableCell);
            DestroyImmediate(gameObject.transform.Find("FavoritesIcon").gameObject);
            DestroyImmediate(gameObject.transform.Find("SongTime").gameObject);
            DestroyImmediate(gameObject.transform.Find("SongBpm").gameObject);
            DestroyImmediate(gameObject.transform.Find("BpmIcon").gameObject);

            return tableCell;
        }

        // temporary while BSML doesn't support the new scroll buttons & indicator
        private void CreateTableView()
        {
            var tableViewContainer = (RectTransform)new GameObject("AvatarsTableView", typeof(RectTransform)).transform;
            var tableView = (RectTransform)new GameObject("AvatarsTableView", typeof(RectTransform), typeof(ScrollRect), typeof(Touchable), typeof(EventSystemListener)).transform;
            var viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).transform;
            var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;

            tableViewContainer.gameObject.SetActive(false);

            tableViewContainer.anchorMin = new Vector2(0.1f, 0f);
            tableViewContainer.anchorMax = new Vector2(0.9f, 0.85f);
            tableViewContainer.sizeDelta = new Vector2(-10, 0);
            tableViewContainer.offsetMin = new Vector2(0, 0);
            tableViewContainer.offsetMax = new Vector2(-10, 0);

            tableView.anchorMin = Vector2.zero;
            tableView.anchorMax = Vector2.one;
            tableView.sizeDelta = Vector2.zero;
            tableView.anchoredPosition = Vector2.zero;

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = Vector2.zero;
            viewport.anchoredPosition = Vector2.zero;

            tableViewContainer.SetParent(rectTransform, false);
            tableView.SetParent(tableViewContainer, false);
            viewport.SetParent(tableView, false);
            content.SetParent(viewport, false);

            tableView.GetComponent<ScrollRect>().viewport = viewport;

            ScrollView scrollView = tableView.gameObject.AddComponent<ScrollView>();
            scrollView.SetField("_contentRectTransform", content);
            scrollView.SetField("_viewport", viewport);

            RectTransform header = Instantiate((RectTransform)_leaderboardViewController.transform.Find("HeaderPanel"), rectTransform, false);

            header.name = "HeaderPanel";

            Destroy(header.GetComponentInChildren<LocalizedTextMeshProUGUI>());

            TextMeshProUGUI textMesh = header.Find("Text").GetComponent<TextMeshProUGUI>();
            textMesh.text = "Avatars";

            _loadingIndicator = Instantiate(_leaderboardViewController.transform.Find("Container/LeaderboardTableView/LoadingControl/LoadingContainer/LoadingIndicator").gameObject, rectTransform, false);

            _loadingIndicator.name = "LoadingIndicator";

            // buttons and indicator have images so it's easier to just copy from an existing component
            Transform scrollBar = Instantiate(_levelCollectionViewController.transform.Find("LevelsTableView/ScrollBar"), tableViewContainer, false);

            scrollBar.name = "ScrollBar";

            Button upButton = scrollBar.Find("UpButton").GetComponent<Button>();
            Button downButton = scrollBar.Find("DownButton").GetComponent<Button>();
            VerticalScrollIndicator verticalScrollIndicator = scrollBar.Find("VerticalScrollIndicator").GetComponent<VerticalScrollIndicator>();

            scrollView.SetField("_pageUpButton", upButton);
            scrollView.SetField("_pageDownButton", downButton);
            scrollView.SetField("_verticalScrollIndicator", verticalScrollIndicator);

            _tableView = _container.InstantiateComponent<TableView>(tableView.gameObject);
            _tableView.SetField("_preallocatedCells", new TableView.CellsGroup[0]);
            _tableView.SetField("_isInitialized", false);
            _tableView.SetField("_scrollView", scrollView);

            _tableView.SetDataSource(this, true);

            _tableView.didSelectCellWithIdxEvent += OnAvatarClicked;

            tableViewContainer.gameObject.SetActive(true);
        }

        private void CreateRefreshButton()
        {
            GameObject gameObject = _container.InstantiatePrefab(_playerOptionsViewController.transform.Find("PlayerOptions/ViewPort/Content/CommonSection/PlayerHeight/MeassureButton").gameObject, transform);
            GameObject iconObject = gameObject.transform.Find("Icon").gameObject;

            gameObject.name = "RefreshButton";

            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.offsetMin = new Vector2(-12, 2);
            rectTransform.offsetMax = new Vector2(-2, 10);

            Button button = gameObject.GetComponent<Button>();
            button.onClick.AddListener(OnRefreshButtonPressed);
            button.transform.SetParent(transform);

            ImageView image = iconObject.GetComponent<ImageView>();
            image.sprite = _reloadSprite;

            HoverHint hoverHint = _container.InstantiateComponent<HoverHint>(gameObject);
            hoverHint.text = "Force reload all avatars, including the one currently spawned. This will most likely lag your game for a few seconds if you have many avatars loaded.";

            Destroy(gameObject.GetComponent<LocalizedHoverHint>());
        }

        private async void OnAvatarClicked(TableView table, int row)
        {
            await _avatarManager.SwitchToAvatarAsync(_avatars[row].fileName, _mirrorViewController);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            UpdateSelectedRow();
        }

        private void OnAvatarAdded(AvatarInfo avatarInfo)
        {
            if (_isLoading)
            {
                return;
            }

            _avatars.Add(new AvatarListItem(avatarInfo));
            ReloadData();
        }

        private void OnAvatarRemoved(AvatarInfo avatarInfo)
        {
            if (_isLoading)
            {
                return;
            }

            _avatars.RemoveAll(a => a.fileName == avatarInfo.fileName);
            ReloadData();
        }

        private async void OnRefreshButtonPressed()
        {
            await ReloadAvatars(true);
        }

        private async Task ReloadAvatars(bool force = false)
        {
            _avatars.Clear();
            _tableView.ReloadData();

            _isLoading = true;

            _avatars.Add(new AvatarListItem("No Avatar", _noAvatarSprite));
            _avatars.AddRange((await _avatarManager.GetAvatarInfosAsync(force)).Select(i => new AvatarListItem(i)));
            ReloadData();
        }

        private void ReloadData()
        {
            _avatars.Sort((a, b) =>
            {
                if (string.IsNullOrEmpty(a.fileName)) return -1;
                if (string.IsNullOrEmpty(b.fileName)) return 1;

                return string.Compare(a.name, b.name, StringComparison.CurrentCulture);
            });

            _tableView.ReloadData();

            _isLoading = false;

            UpdateSelectedRow(true);
        }

        private void UpdateSelectedRow(bool scroll = false)
        {
            int currentRow = _avatarManager.currentlySpawnedAvatar ? _avatars.FindIndex(a => a.fileName == _avatarManager.currentlySpawnedAvatar.prefab.fileName) : 0;

            if (scroll) _tableView.ScrollToCellWithIdx(currentRow, TableView.ScrollPositionType.Center, false);

            _tableView.SelectCellWithIdx(currentRow);
        }

        public float CellSize()
        {
            return 8.5f;
        }

        public int NumberOfCells()
        {
            return _avatars.Count;
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
            var tableCell = _tableView.DequeueReusableCellForIdentifier(kTableCellReuseIdentifier) as AvatarListTableCell;

            if (!tableCell)
            {
                tableCell = Instantiate(_tableCellPrefab);
                tableCell.reuseIdentifier = kTableCellReuseIdentifier;
            }

            AvatarListItem avatar = _avatars[idx];
            Sprite icon = avatar.icon ? avatar.icon : _blankAvatarSprite;

            tableCell.nameText.text = avatar.name;
            tableCell.authorText.text = avatar.author;
            tableCell.cover.sprite = icon;

            return tableCell;
        }
    }
}

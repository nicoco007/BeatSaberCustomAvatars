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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using Polyglot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarListViewController : ViewController, TableView.IDataSource
    {
        private const int kMaxNumberOfConcurrentLoadingTasks = 4;
        private const string kTableCellReuseIdentifier = "AvatarListTableCell";

        private ILogger<AvatarListViewController> _logger;
        private PlayerAvatarManager _avatarManager;
        private DiContainer _container;
        private MirrorViewController _mirrorViewController;
        private PlayerOptionsViewController _playerOptionsViewController;
        private LevelCollectionViewController _levelCollectionViewController;
        private PlatformLeaderboardViewController _leaderboardViewController;

        private FileSystemWatcher _fileSystemWatcher;
        private TableView _tableView;

        private readonly List<AvatarListItem> _avatars = new List<AvatarListItem>();
        private AvatarListTableCell _tableCellPrefab;

        private Texture2D _atlas;
        private Sprite _blankAvatarSprite;
        private Sprite _noAvatarSprite;
        private Sprite _reloadSprite;
        private Sprite _loadErrorSprite;

        [Inject]
        internal void Construct(ILogger<AvatarListViewController> logger, PlayerAvatarManager avatarManager, DiContainer container, MirrorViewController mirrorViewController, PlayerOptionsViewController playerOptionsViewController, LevelCollectionViewController levelCollectionViewController, PlatformLeaderboardViewController leaderboardViewController)
        {
            _logger = logger;
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
                _loadErrorSprite = Sprite.Create(_atlas, new Rect(256, 256, 256, 256), new Vector2(0.5f, 0.5f));
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

            try
            {
                _fileSystemWatcher = new FileSystemWatcher(PlayerAvatarManager.kCustomAvatarsPath, "*.avatar")
                {
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _fileSystemWatcher.Created += OnAvatarFileCreatedOrChanged;
                _fileSystemWatcher.Changed += OnAvatarFileCreatedOrChanged;
                _fileSystemWatcher.Deleted += OnAvatarFileDeleted;

                _fileSystemWatcher.EnableRaisingEvents = true;

                _logger.Trace($"Watching files in '{_fileSystemWatcher.Path}' ({_fileSystemWatcher.Filter})");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to create FileSystemWatcher");
                _logger.Error(ex);
            }

            await ReloadAvatars();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _avatarManager.avatarChanged -= OnAvatarChanged;

            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.Created -= OnAvatarFileCreatedOrChanged;
                _fileSystemWatcher.Changed -= OnAvatarFileCreatedOrChanged;
                _fileSystemWatcher.Deleted -= OnAvatarFileDeleted;

                _fileSystemWatcher.Dispose();
            }
        }

        private AvatarListTableCell CreateTableCellPrefab()
        {
            GameObject gameObject = Instantiate(_levelCollectionViewController.transform.Find("LevelsTableView/TableView/Viewport/Content/LevelListTableCell").gameObject);
            gameObject.name = "AvatarListTableCell";

            LevelListTableCell originalTableCell = gameObject.GetComponent<LevelListTableCell>();

            AvatarListTableCell tableCell = gameObject.AddComponent<AvatarListTableCell>();
            tableCell.Init(originalTableCell, _leaderboardViewController);

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
            await _avatarManager.SwitchToAvatarAsync(_avatars[row].fileName, new Progress<float>(_mirrorViewController.UpdateProgress));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            UpdateSelectedRow();
        }

        private async void OnAvatarFileCreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            _logger.Trace($"File {e.ChangeType}: '{fileName}'");

            await UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                AvatarListItem item = _avatars.Find(a => a.fileName == fileName);

                if (item != null)
                {
                    item.isLoaded = false;
                }
                else
                {
                    if (_avatarManager.TryGetCachedAvatarInfo(fileName, out AvatarInfo avatarInfo))
                    {
                        item = new AvatarListItem(avatarInfo, false, _blankAvatarSprite);
                    }
                    else
                    {
                        item = new AvatarListItem(Path.GetFileNameWithoutExtension(fileName), _blankAvatarSprite, fileName, false);
                    }

                    _avatars.Add(item);
                    ReloadData();
                }

                await GetAvatarInfoAsync(item, true);
            });
        }

        private async void OnAvatarFileDeleted(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            _logger.Trace($"File Deleted: '{fileName}'");

            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                _avatars.RemoveAll(a => a.fileName == fileName);
                ReloadData();
            });
        }

        private async void OnRefreshButtonPressed()
        {
            await ReloadAvatars(true);
        }

        private async Task ReloadAvatars(bool force = false)
        {
            _avatars.Clear();
            _avatars.Add(new AvatarListItem("No Avatar", _noAvatarSprite, null, true));

            List<string> fileNames = _avatarManager.GetAvatarFileNames();
            var avatarsToLoad = new List<AvatarListItem>();

            foreach (string fileName in fileNames)
            {
                if (_avatarManager.TryGetCachedAvatarInfo(fileName, out AvatarInfo avatarInfo))
                {
                    var item = new AvatarListItem(avatarInfo, !force, _blankAvatarSprite);
                    _avatars.Add(item);

                    if (force)
                    {
                        avatarsToLoad.Add(item);
                    }
                }
                else
                {
                    var item = new AvatarListItem(Path.GetFileNameWithoutExtension(fileName), _blankAvatarSprite, fileName, false);
                    _avatars.Add(item);
                    avatarsToLoad.Add(item);
                }
            }

            ReloadData();

            var tasks = new List<Task>();

            using (var semaphore = new SemaphoreSlim(kMaxNumberOfConcurrentLoadingTasks))
            {
                foreach (AvatarListItem avatarToLoad in avatarsToLoad)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(GetAvatarInfoAsync(avatarToLoad, force).ContinueWith((t) =>
                    {
                        semaphore.Release();
                    }));
                }

                await Task.WhenAll(tasks);
            }
        }

        private async Task GetAvatarInfoAsync(AvatarListItem avatar, bool forceReload)
        {
            try
            {
                AvatarInfo avatarInfo = await _avatarManager.GetAvatarInfo(avatar.fileName, new Progress<float>((p) =>
                {
                    avatar.UpdateProgress(p);
                }), forceReload);

                avatar.SetLoadedInfo(avatarInfo, _blankAvatarSprite);

                // in case the order is different with the actual name
                ReloadData();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load avatar '{avatar.fileName}'");
                _logger.Error(ex);

                avatar.SetException(ex, _loadErrorSprite);
            }
        }

        private void ReloadData()
        {
            _avatars.Sort((a, b) =>
            {
                if (string.IsNullOrEmpty(a.fileName)) return -1;
                if (string.IsNullOrEmpty(b.fileName)) return 1;

                return string.Compare(a.name, b.name, StringComparison.CurrentCulture);
            });

            _tableView.ReloadDataKeepingPosition();
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
            tableCell.listItem = avatar;

            return tableCell;
        }
    }
}

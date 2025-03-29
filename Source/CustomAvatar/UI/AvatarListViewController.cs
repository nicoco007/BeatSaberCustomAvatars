//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using HMUI;
using IPA.Utilities.Async;
using UnityEngine;
using UnityEngine.U2D;
using Zenject;

namespace CustomAvatar.UI
{
    [ViewDefinition("CustomAvatar.UI.Views.AvatarList.bsml")]
    [HotReload(RelativePathToLayout = "Views/AvatarList.bsml")]
    internal class AvatarListViewController : BSMLAutomaticViewController, TableView.IDataSource
    {
        private const int kMaxNumberOfConcurrentLoadingTasks = 4;
        private const string kTableCellReuseIdentifier = "AvatarListTableCell";

        private ILogger<AvatarListViewController> _logger;
        private PlayerAvatarManager _avatarManager;
        private MirrorViewController _mirrorViewController;
        private LevelCollectionViewController _levelCollectionViewController;
        private PlatformLeaderboardViewController _leaderboardViewController;
        private AssetLoader _assetLoader;

        private FileSystemWatcher _fileSystemWatcher;
        private TableView _tableView;
        private AvatarListTableCell _tableCellPrefab;

        private Sprite _blankAvatarSprite;
        private Sprite _noAvatarSprite;
        private Sprite _loadErrorSprite;

#pragma warning disable CS0649, IDE0044
        [UIComponent("avatar-list")]
        private CustomCellListTableData _tableData;
#pragma warning restore CS0649, IDE0044

        protected List<AvatarListItem> avatars { get; } = new();

        protected Sprite reloadSprite { get; set; }

        [Inject]
        internal void Construct(ILogger<AvatarListViewController> logger, PlayerAvatarManager avatarManager, MirrorViewController mirrorViewController, LevelCollectionViewController levelCollectionViewController, PlatformLeaderboardViewController leaderboardViewController, AssetLoader assetLoader)
        {
            _logger = logger;
            _avatarManager = avatarManager;
            _mirrorViewController = mirrorViewController;
            _levelCollectionViewController = levelCollectionViewController;
            _leaderboardViewController = leaderboardViewController;
            _assetLoader = assetLoader;
        }

        protected void Start()
        {
            SpriteAtlas atlas = _assetLoader.uiSpriteAtlas;
            _noAvatarSprite = atlas.GetSprite("NoAvatarIcon");
            _blankAvatarSprite = atlas.GetSprite("BlankAvatarIcon");
            _loadErrorSprite = atlas.GetSprite("LoadErrorIcon");
            reloadSprite = atlas.GetSprite("ReloadButtonIcon");
        }

        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                _tableCellPrefab = CreateTableCellPrefab();
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

                _logger.LogTrace($"Watching files in '{_fileSystemWatcher.Path}' ({_fileSystemWatcher.Filter})");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create FileSystemWatcher");
                _logger.LogError(ex);
            }

            await ReloadAvatars();
            UpdateSelectedRow();
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

        [UIAction("#post-parse")]
        protected void PostParse()
        {
            // TODO: this is a temporary workaround until BSML has a way to specify a data source or something
            _tableView = _tableData.TableView;
            Destroy(_tableData);
            _tableView.SetDataSource(this, true);
            _tableView.didSelectCellWithIdxEvent += OnAvatarClicked;
        }

        private AvatarListTableCell CreateTableCellPrefab()
        {
            GameObject gameObject = Instantiate(_levelCollectionViewController.transform.Find("LevelsTableView/TableView/Viewport/Content/LevelListTableCell").gameObject);
            gameObject.name = "AvatarListTableCell";

            LevelListTableCell originalTableCell = gameObject.GetComponent<LevelListTableCell>();
            AvatarListTableCell tableCell = gameObject.AddComponent<AvatarListTableCell>();
            tableCell.Init(originalTableCell, _leaderboardViewController);

            DestroyImmediate(originalTableCell);

            return tableCell;
        }

        private async void OnAvatarClicked(TableView table, int row)
        {
            await _avatarManager.SwitchToAvatarAsync(avatars[row].fileName, new Progress<float>(_mirrorViewController.UpdateProgress));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            UpdateSelectedRow();
        }

        private async void OnAvatarFileCreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            _logger.LogTrace($"File {e.ChangeType}: '{fileName}'");

            await UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                AvatarListItem item = avatars.Find(a => a.fileName == fileName);

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

                    avatars.Add(item);
                    ReloadData();
                }

                await GetAvatarInfoAsync(item, true);
            });
        }

        private async void OnAvatarFileDeleted(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            _logger.LogTrace($"File Deleted: '{fileName}'");

            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                avatars.RemoveAll(a => a.fileName == fileName);
                ReloadData();
            });
        }

        protected void OnRefreshButtonPressed()
        {
            ReloadAvatars(true).ContinueWith((task) => _logger.LogError($"Failed to reload avatars\n{task.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task ReloadAvatars(bool force = false)
        {
            avatars.Clear();
            avatars.Add(new AvatarListItem("No Avatar", _noAvatarSprite, null, true));

            List<string> fileNames = _avatarManager.GetAvatarFileNames();
            var avatarsToLoad = new List<AvatarListItem>();

            foreach (string fileName in fileNames)
            {
                if (_avatarManager.TryGetCachedAvatarInfo(fileName, out AvatarInfo avatarInfo))
                {
                    var item = new AvatarListItem(avatarInfo, !force, _blankAvatarSprite);
                    avatars.Add(item);

                    if (force)
                    {
                        avatarsToLoad.Add(item);
                    }
                }
                else
                {
                    var item = new AvatarListItem(Path.GetFileNameWithoutExtension(fileName), _blankAvatarSprite, fileName, false);
                    avatars.Add(item);
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
                AvatarInfo avatarInfo = await _avatarManager.GetAvatarInfo(avatar.fileName, avatar, forceReload);

                avatar.SetLoadedInfo(avatarInfo, _blankAvatarSprite);

                // in case the order is different with the actual name
                ReloadData();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load avatar '{avatar.fileName}'");
                _logger.LogError(ex);

                avatar.SetException(ex, _loadErrorSprite);
            }
        }

        private void ReloadData()
        {
            avatars.Sort((a, b) =>
            {
                if (string.IsNullOrEmpty(a.fileName)) return -1;
                if (string.IsNullOrEmpty(b.fileName)) return 1;

                return string.Compare(a.name, b.name, StringComparison.CurrentCulture);
            });

            _tableView.ReloadDataKeepingPosition();
        }

        private void UpdateSelectedRow(bool scroll = false)
        {
            int currentRow = _avatarManager.currentlySpawnedAvatar ? avatars.FindIndex(a => a.fileName == _avatarManager.currentAvatarFileName) : 0;

            if (scroll) _tableView.ScrollToCellWithIdx(currentRow, TableView.ScrollPositionType.Center, false);

            _tableView.SelectCellWithIdx(currentRow);
        }

        public float CellSize(int idx)
        {
            return 8.5f;
        }

        public int NumberOfCells()
        {
            return avatars.Count;
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
            var tableCell = _tableView.DequeueReusableCellForIdentifier(kTableCellReuseIdentifier) as AvatarListTableCell;

            if (!tableCell)
            {
                tableCell = Instantiate(_tableCellPrefab);
                tableCell.reuseIdentifier = kTableCellReuseIdentifier;
            }

            AvatarListItem avatar = avatars[idx];
            tableCell.listItem = avatar;

            return tableCell;
        }
    }
}

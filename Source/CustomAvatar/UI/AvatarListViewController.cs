//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Avatar;
using CustomAvatar.Utilities;
using HMUI;
using Polyglot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarListViewController : ViewController, TableView.IDataSource
    {
        private const string kTableCellReuseIdentifier = "CustomAvatarsTableCell";

        private PlayerAvatarManager _avatarManager;
        private DiContainer _container;

        private TableView _tableView;
        private GameObject _loadingIndicator;

        private readonly List<AvatarListItem> _avatars = new List<AvatarListItem>();
        private LevelListTableCell _tableCellTemplate;

        private Texture2D _blankAvatarIcon;
        private Texture2D _noAvatarIcon;

        [Inject]
        private void Inject(PlayerAvatarManager avatarManager, DiContainer container)
        {
            _avatarManager = avatarManager;
            _container = container;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                _tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");

                _blankAvatarIcon = LoadTextureFromResource("CustomAvatar.Resources.mystery-man.png");
                _noAvatarIcon = LoadTextureFromResource("CustomAvatar.Resources.ban.png");

                CreateTableView();

                rectTransform.sizeDelta = new Vector2(120, 0);
                rectTransform.offsetMin = new Vector2(-60, 0);
                rectTransform.offsetMax = new Vector2(60, 0);
            }

            if (addedToHierarchy)
            {
                _avatarManager.avatarChanged += OnAvatarChanged;

                _avatars.Clear();
                _avatars.Add(new AvatarListItem("No Avatar", _noAvatarIcon));

                SetLoading(true);
                _avatarManager.GetAvatarInfosAsync(avatar => _avatars.Add(new AvatarListItem(avatar)), null, ReloadData);
            }
        }

        // temporary while BSML doesn't support the new scroll buttons & indicator
        private void CreateTableView()
        {
            RectTransform tableViewContainer = new GameObject("AvatarsTableView", typeof(RectTransform)).transform as RectTransform;
            RectTransform tableView = new GameObject("AvatarsTableView", typeof(RectTransform), typeof(ScrollRect), typeof(Touchable), typeof(EventSystemListener)).transform as RectTransform;
            RectTransform viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).transform as RectTransform;

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

            tableView.GetComponent<ScrollRect>().viewport = viewport;

            Transform header = Instantiate(Resources.FindObjectsOfTypeAll<LeaderboardViewController>().First().transform.Find("HeaderPanel"), rectTransform, false);

            Destroy(header.GetComponentInChildren<LocalizedTextMeshProUGUI>());
            header.GetComponentInChildren<TextMeshProUGUI>().text = "Avatars";

            _loadingIndicator = Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First().transform.Find("LoadingContainer/LoadingIndicator").gameObject, rectTransform, false);

            // buttons and indicator have images so it's easier to just copy from an existing component
            Transform scrollBar = Instantiate(Resources.FindObjectsOfTypeAll<LevelCollectionTableView>().First().transform.Find("ScrollBar"), tableViewContainer, false);

            Button upButton = scrollBar.Find("UpButton").GetComponent<Button>();
            Button downButton = scrollBar.Find("DownButton").GetComponent<Button>();
            VerticalScrollIndicator verticalScrollIndicator = scrollBar.Find("VerticalScrollIndicator").GetComponent<VerticalScrollIndicator>();

            _tableView = _container.InstantiateComponent<TableView>(tableView.gameObject);

            _tableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
            _tableView.SetPrivateField("_isInitialized", false);
            _tableView.SetPrivateField("_pageUpButton", upButton);
            _tableView.SetPrivateField("_pageDownButton", downButton);
            _tableView.SetPrivateField("_verticalScrollIndicator", verticalScrollIndicator);
            _tableView.SetPrivateField("_hideScrollButtonsIfNotNeeded", false);
            _tableView.SetPrivateField("_hideScrollIndicatorIfNotNeeded", false);

            _tableView.SetDataSource(this, true);

            _tableView.didSelectCellWithIdxEvent += OnAvatarClicked;

            tableViewContainer.gameObject.SetActive(true);
        }

        private Texture2D LoadTextureFromResource(string resourceName)
        {
            Texture2D texture = new Texture2D(0, 0);

            using (Stream textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                byte[] textureBytes = new byte[textureStream.Length];
                textureStream.Read(textureBytes, 0, (int)textureStream.Length);
                texture.LoadImage(textureBytes);
            }

            return texture;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            if (removedFromHierarchy)
            {
                _avatarManager.avatarChanged -= OnAvatarChanged;
            }
        }

        private void OnAvatarClicked(TableView table, int row)
        {
            _avatarManager.SwitchToAvatarAsync(_avatars[row].fileName);
            _tableView.ScrollToCellWithIdx(row, TableViewScroller.ScrollPositionType.Center, true);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            UpdateSelectedRow();
        }

        private void ReloadData()
        {
            _avatars.Sort((a, b) =>
            {
                if (string.IsNullOrEmpty(a.fileName)) return -1;
                if (string.IsNullOrEmpty(b.fileName)) return 1;

                return string.Compare(a.name, b.name, StringComparison.CurrentCulture);
            });

            SetLoading(false);

            UpdateSelectedRow();
        }

        private void UpdateSelectedRow()
        {
            int currentRow = _avatarManager.currentlySpawnedAvatar ? _avatars.FindIndex(a => a.fileName == _avatarManager.currentlySpawnedAvatar.avatar.fileName) : 0;

            _tableView.ReloadData();
            _tableView.ScrollToCellWithIdx(currentRow, TableViewScroller.ScrollPositionType.Center, true);
            _tableView.SelectCellWithIdx(currentRow);
        }

        private void SetLoading(bool loading)
        {
            _loadingIndicator.SetActive(loading);
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
            LevelListTableCell tableCell = _tableView.DequeueReusableCellForIdentifier(kTableCellReuseIdentifier) as LevelListTableCell;

            if (!tableCell)
            {
                tableCell = Instantiate(_tableCellTemplate);

                tableCell.GetPrivateField<Image>("_backgroundImage").enabled = false;
                tableCell.GetPrivateField<Image>("_favoritesBadgeImage").enabled = false;

                tableCell.transform.Find("BpmIcon").gameObject.SetActive(false);

                tableCell.GetPrivateField<TextMeshProUGUI>("_songDurationText").enabled = false;
                tableCell.GetPrivateField<TextMeshProUGUI>("_songBpmText").enabled = false;

                tableCell.reuseIdentifier = kTableCellReuseIdentifier;
            }

            AvatarListItem avatar = _avatars[idx];

            tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = avatar.name;
            tableCell.GetPrivateField<TextMeshProUGUI>("_songAuthorText").text = avatar.author;

            Texture2D icon = avatar.icon ? avatar.icon : _blankAvatarIcon;

            tableCell.GetPrivateField<Image>("_coverImage").sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.zero);

            return tableCell;
        }
    }
}

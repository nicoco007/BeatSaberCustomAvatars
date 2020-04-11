using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Utilities;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class AvatarListViewController : BSMLResourceViewController, TableView.IDataSource
    {
        [Inject] private AvatarManager _avatarManager;

        private const string kTableCellReuseIdentifier = "CustomAvatarsTableCell";

        public override string ResourceName => "CustomAvatar.Views.AvatarList.bsml";


        [UIComponent("avatar-list")] public CustomListTableData avatarList;
        [UIComponent("up-button")] public Button upButton;
        [UIComponent("down-button")] public Button downButton;

        private readonly List<AvatarListItem> _avatars = new List<AvatarListItem>();
        private LevelListTableCell _tableCellTemplate;

        private Texture2D _blankAvatarIcon;
        private Texture2D _noAvatarIcon;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            _avatarManager.avatarChanged += OnAvatarChanged;
            
            _blankAvatarIcon = LoadTextureFromResource("CustomAvatar.Resources.mystery-man.png");
            _noAvatarIcon = LoadTextureFromResource("CustomAvatar.Resources.ban.png");

            if (firstActivation) FirstActivation();
        }

        private Texture2D LoadTextureFromResource(string resourceName)
        {
            Texture2D texture = new Texture2D(0, 0);

            using (Stream textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                byte[] textureBytes = new byte[textureStream.Length];
                textureStream.Read(textureBytes, 0, (int) textureStream.Length);
                texture.LoadImage(textureBytes);
            }

            return texture;
        }

        private void FirstActivation()
        {
            _tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");

            avatarList.tableView.SetPrivateField("_pageUpButton", upButton);
            avatarList.tableView.SetPrivateField("_pageDownButton", downButton);
            avatarList.tableView.SetPrivateField("_hideScrollButtonsIfNotNeeded", false);

            TableViewScroller scroller = avatarList.tableView.GetPrivateField<TableView, TableViewScroller>("_scroller");

            upButton.onClick.AddListener(() =>
            {
                scroller.PageScrollUp();
                avatarList.tableView.RefreshScrollButtons(false);
            });

            downButton.onClick.AddListener(() =>
            {
                scroller.PageScrollDown();
                avatarList.tableView.RefreshScrollButtons(false);
            });
            
            avatarList.tableView.dataSource = this;

            _avatars.Add(new AvatarListItem("No Avatar", _noAvatarIcon));
            
            _avatarManager.GetAvatarsAsync(avatar =>
            {
                _avatars.Add(new AvatarListItem(avatar));

                ReloadData();
            });
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            _avatarManager.avatarChanged -= OnAvatarChanged;
        }

        
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [UIAction("avatar-click")]
        private void OnAvatarClicked(TableView table, int row)
        {
            _avatarManager.SwitchToAvatar(_avatars[row].avatar);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            _avatars.Sort((a, b) =>
            {
                if (a.avatar == null) return -1;
                if (b.avatar == null) return 1;

                return string.Compare(a.name, b.name, StringComparison.CurrentCulture);
            });

            int currentRow = _avatars.FindIndex(a => a.avatar?.fullPath == _avatarManager.currentlySpawnedAvatar?.avatar.fullPath);
            
            avatarList.tableView.ReloadData();
            avatarList.tableView.ScrollToCellWithIdx(currentRow, TableViewScroller.ScrollPositionType.Center, true);
            avatarList.tableView.SelectCellWithIdx(currentRow);
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
            LevelListTableCell tableCell = avatarList.tableView.DequeueReusableCellForIdentifier(kTableCellReuseIdentifier) as LevelListTableCell;

            if (!tableCell)
            {
                tableCell = Instantiate(_tableCellTemplate);

                foreach (var image in tableCell.GetPrivateField<LevelListTableCell, UnityEngine.UI.Image[]>("_beatmapCharacteristicImages"))
                {
                    DestroyImmediate(image);
                }

                tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
                tableCell.GetPrivateField<LevelListTableCell, RawImage>("_favoritesBadgeImage").enabled = false;

                tableCell.reuseIdentifier = kTableCellReuseIdentifier;
            }

            AvatarListItem avatar = _avatars[idx];

            tableCell.GetPrivateField<LevelListTableCell, TextMeshProUGUI>("_songNameText").text = avatar.name;
            tableCell.GetPrivateField<LevelListTableCell, TextMeshProUGUI>("_authorText").text = avatar.author;
            tableCell.GetPrivateField<LevelListTableCell, RawImage>("_coverRawImage").texture = avatar.icon ?? _blankAvatarIcon;

            return tableCell;
        }
    }
}
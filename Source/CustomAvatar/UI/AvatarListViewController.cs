using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomAvatar.UI
{
    public class AvatarListViewController : BSMLResourceViewController, TableView.IDataSource
    {
        private const string kTableCellReuseIdentifier = "CustomAvatarsTableCell";

        public override string ResourceName => "CustomAvatar.Views.AvatarListViewController.bsml";

        [UIComponent("avatar-list")] public CustomListTableData AvatarList;
        [UIComponent("up-button")] public Button UpButton;
        [UIComponent("down-button")] public Button DownButton;

        private List<CustomAvatar> avatars = new List<CustomAvatar>();
        private LevelListTableCell tableCellTemplate;
        
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            AvatarManager.instance.avatarChanged += OnAvatarChanged;

            if (firstActivation) FirstActivation();
        }

        private void FirstActivation()
        {
            tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");
            AvatarManager.instance.GetAvatarsAsync(avatar =>
            {
                Plugin.logger.Info("Loaded avatar " + avatar.descriptor.name);

                avatars.Add(avatar);

                ReloadData();
            }, ex =>
            {
                Plugin.logger.Error("Failed to load avatar: " + ex.Message);
            });

            AvatarList.tableView.dataSource = this;
            AvatarList.tableView.SetPrivateField("_pageUpButton", UpButton);
            AvatarList.tableView.SetPrivateField("_pageDownButton", DownButton);

            TableViewScroller scroller = AvatarList.tableView.GetPrivateField<TableViewScroller>("_scroller");

            UpButton.onClick.AddListener(() =>
            {
                scroller.PageScrollUp();
                AvatarList.tableView.InvokePrivateMethod("RefreshScrollButtons", false);
            });

            DownButton.onClick.AddListener(() =>
            {
                scroller.PageScrollDown();
                AvatarList.tableView.InvokePrivateMethod("RefreshScrollButtons", false);
            });
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            AvatarManager.instance.avatarChanged -= OnAvatarChanged;
        }

        
        // ReSharper disable UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [UIAction("avatar-click")]
        private void OnAvatarClicked(TableView table, int row)
        {
            AvatarManager.instance.SwitchToAvatar(avatars[row]);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            avatars.Sort((a, b) => string.Compare(a.descriptor.name, b.descriptor.name, StringComparison.CurrentCulture));

            int currentRow = avatars.FindIndex(a => a.fullPath == AvatarManager.instance.currentlySpawnedAvatar?.customAvatar.fullPath);
            
            AvatarList.tableView.ReloadData();
            AvatarList.tableView.ScrollToCellWithIdx(currentRow, TableViewScroller.ScrollPositionType.Center, true);
            AvatarList.tableView.SelectCellWithIdx(currentRow);
        }

        public float CellSize()
        {
            return 8.5f;
        }

        public int NumberOfCells()
        {
            return avatars.Count;
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
            LevelListTableCell tableCell = AvatarList.tableView.DequeueReusableCellForIdentifier(kTableCellReuseIdentifier) as LevelListTableCell;

            if (!tableCell)
            {
                tableCell = Instantiate(tableCellTemplate);

                foreach (var image in tableCell.GetPrivateField<UnityEngine.UI.Image[]>("_beatmapCharacteristicImages"))
                {
                    DestroyImmediate(image);
                }

                tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
                tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
                tableCell.GetPrivateField<RawImage>("_favoritesBadgeImage").enabled = false;

                tableCell.reuseIdentifier = kTableCellReuseIdentifier;
            }

            CustomAvatar avatar = avatars[idx];

            tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = avatar.descriptor.name;
            tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = avatar.descriptor.author;
            tableCell.GetPrivateField<RawImage>("_coverRawImage").texture = avatar.descriptor.cover?.texture ?? Texture2D.blackTexture;

            return tableCell;
        }
    }
}
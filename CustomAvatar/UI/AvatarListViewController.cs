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
        public override string ResourceName => "CustomAvatar.Views.AvatarListViewController.bsml";
		
        [UIComponent("avatar-list")] public CustomListTableData AvatarList;
        [UIComponent("up-button")] public Button UpButton;
        [UIComponent("down-button")] public Button DownButton;

        private List<CustomAvatar> avatars = new List<CustomAvatar>();
        private LevelListTableCell tableCellTemplate;
        
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            AvatarManager.Instance.AvatarChanged += OnAvatarChanged;

            if (firstActivation) FirstActivation();
        }

        private void FirstActivation()
        {
	        tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");
	        AvatarManager.Instance.GetAvatarsAsync(avatar =>
	        {
		        Plugin.Logger.Info("Loaded avatar " + avatar.Descriptor.Name);

		        avatars.Add(avatar);

		        ReloadData();
	        }, ex =>
	        {
				Plugin.Logger.Error("Failed to load avatar: " + ex.Message);
	        });

	        AvatarList.tableView.dataSource = this;
	        AvatarList.tableView.SetPrivateField("_pageUpButton", UpButton);
	        AvatarList.tableView.SetPrivateField("_pageDownButton", DownButton);

	        TableViewScroller scroller = AvatarList.tableView.GetPrivateField<TableViewScroller>("_scroller");

			UpButton.onClick.AddListener(() =>
			{
				scroller.PageScrollUp();
				AvatarList.tableView.RefreshScrollButtons();
			});

			DownButton.onClick.AddListener(() =>
			{
				scroller.PageScrollDown();
				AvatarList.tableView.RefreshScrollButtons();
			});
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
	        base.DidDeactivate(deactivationType);

	        AvatarManager.Instance.AvatarChanged -= OnAvatarChanged;
        }

		
		// ReSharper disable UnusedMember.Local
		// ReSharper disable once UnusedParameter.Local
        [UIAction("avatar-click")]
        private void OnAvatarClicked(TableView table, int row)
		{
	        AvatarManager.Instance.SwitchToAvatar(avatars[row]);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
	        ReloadData();
        }

        private void ReloadData()
        {
	        avatars.Sort((a, b) => string.Compare(a.Descriptor.Name, b.Descriptor.Name, StringComparison.CurrentCulture));

	        int currentRow = avatars.FindIndex(a => a.FullPath == AvatarManager.Instance.CurrentlySpawnedAvatar?.CustomAvatar.FullPath);
			
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
	        LevelListTableCell tableCell = AvatarList.tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;

	        if (!tableCell)
	        {
		        tableCell = Instantiate(tableCellTemplate);

		        foreach (var image in tableCell.GetPrivateField<UnityEngine.UI.Image[]>("_beatmapCharacteristicImages"))
		        {
			        DestroyImmediate(image);
		        }

		        tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
		        tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);

		        tableCell.reuseIdentifier = "CustomAvatarsTableCell";
	        }

	        CustomAvatar avatar = avatars[idx];

	        tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = avatar.Descriptor.Name;
	        tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = avatar.Descriptor.Author;
	        tableCell.GetPrivateField<RawImage>("_coverRawImage").texture = avatar.Descriptor.Cover?.texture ?? Texture2D.blackTexture;

	        return tableCell;
        }
    }
}

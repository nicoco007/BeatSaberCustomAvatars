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

        [UIComponent("avatar-list")]
        public CustomListTableData avatarList;

        private List<CustomAvatar> avatars;
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
	        avatars = AvatarManager.Instance.Avatars;
	        avatarList.tableView.dataSource = this;
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
	        base.DidDeactivate(deactivationType);

	        AvatarManager.Instance.AvatarChanged -= OnAvatarChanged;
        }

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
	        int currentRow = avatars.IndexOf(AvatarManager.Instance.CurrentlySpawnedAvatar.CustomAvatar);

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
	        return avatars.Count;
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
	        LevelListTableCell tableCell = avatarList.tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;

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

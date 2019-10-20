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

        private List<CustomAvatar> loadedAvatars;
        private LevelListTableCell tableCellTemplate;
        
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            Plugin.Instance.PlayerAvatarManager.AvatarChanged += OnAvatarChanged;

            if (firstActivation) FirstActivation();
        }

        private void FirstActivation()
        {
	        tableCellTemplate = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => x.name == "LevelListTableCell");
            loadedAvatars = new List<CustomAvatar>();
	        avatarList.tableView.dataSource = this;

	        foreach (CustomAvatar avatar in Plugin.Instance.AvatarLoader.Avatars)
	        {
		        avatar.Load(OnAvatarLoaded);
	        }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
	        base.DidDeactivate(deactivationType);

	        Plugin.Instance.PlayerAvatarManager.AvatarChanged -= OnAvatarChanged;
        }

        [UIAction("avatar-click")]
        private void OnAvatarClicked(TableView table, int row)
        {
	        Plugin.Instance.PlayerAvatarManager.SwitchToAvatar(loadedAvatars[row]);
        }

        private void OnAvatarLoaded(CustomAvatar avatar, AvatarLoadResult loadResult)
        {
	        if (loadResult != AvatarLoadResult.Completed)
	        {
		        Plugin.Logger.Error("Avatar " + avatar.FullPath + " failed to load");
		        return;
	        }

	        loadedAvatars.Add(avatar);

	        ReloadData();
        }

        private void OnAvatarChanged(CustomAvatar avatar)
        {
	        ReloadData();
        }

        private void ReloadData()
        {
	        int currentRow = loadedAvatars.IndexOf(Plugin.Instance.PlayerAvatarManager.CurrentPlayerAvatar);

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
	        return loadedAvatars.Count;
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
	        LevelListTableCell tableCell = avatarList.tableView.DequeueReusableCellForIdentifier("AvatarListCell") as LevelListTableCell;

	        if (!tableCell)
	        {
		        tableCell = Instantiate(tableCellTemplate);

		        tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
		        tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);

		        foreach (Behaviour behaviour in tableCellTemplate.GetPrivateField<UnityEngine.UI.Image[]>("_beatmapCharacteristicImages"))
			        behaviour.enabled = false;

		        tableCell.reuseIdentifier = "CustomAvatarsTableCell";
	        }

	        CustomAvatar avatar = loadedAvatars[idx];

	        tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = avatar.Name;
	        tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = avatar.AuthorName;
	        tableCell.GetPrivateField<RawImage>("_coverRawImage").texture = avatar.CoverImage?.texture ?? Texture2D.blackTexture;

	        return tableCell;
        }
    }
}

# Beat Saber Custom Avatars Plugin
[![GitHub Actions Build Status](https://img.shields.io/github/workflow/status/nicoco007/BeatSaberCustomAvatars/build/develop?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/actions?query=workflow%3Abuild+branch%3Adevelop)
[![Latest Release](https://img.shields.io/github/v/release/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/releases/latest)
[![License](https://img.shields.io/github/license/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/blob/master/LICENSE)

## Getting Started
The easiest way to get Custom Avatars up and running is to use [ModAssistant](https://github.com/Assistant/ModAssistant). The latest version of Custom Avatars will always be posted in the [releases](https://github.com/nicoco007/BeatSaberCustomAvatars/releases) here, and then become available on ModAssistant a few days later. If you want to install it manually by using the releases available here or are looking for the files to create an avatar yourself, you can follow the instructions below.

### What to download
- RELEASE is what you should download if you just want to use the mod. If you are unsure what to download, this is the file you are looking for.
- DEBUG contains debug code and a PDB for debugging purposes. This is useful if you're developing a mod that relies on Custom Avatars.
- UnityProject contains a barebones Unity project to get started with creating avatars. You will need to copy `FinalIK.dll` and `DynamicBone.dll` from the game's installation folder into the `Assets\Libraries` folder **before opening the project in Unity** to get it working.
- Editor contains only the `CustomAvatar.dll` file for use in the editor. Use this if you want to update your current Unity project to the latest version of Custom Avatars.

### Installing manually
Install [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) and [DynamicOpenVR](https://github.com/nicoco007/DynamicOpenVR/releases) (the required versions are listed in Custom Avatars' release notes). Download [the latest release of Custom Avatars](https://github.com/nicoco007/BeatSaberCustomAvatars/releases/latest) and extract **the entire contents** of the ZIP file into your Beat Saber folder and **overwrite any existing files**. By default, your Beat Saber install folder should be `C:\Program Files (x86)\Steam\steamapps\common\Beat Saber` for Steam and `C:\Program Files\Oculus\Software\Software\hyperbolic-magnetism-beat-saber` for Oculus.

## Usage Notes
### Full-Body Tracking
The way full-body tracking works has changed as of version 5.0.0. You must now do the following for full-body tracking to work:
- Set up your trackers' roles in SteamVR. This makes it so you no longer need to turn on your trackers in a specific order.
- Calibrate your avatar in-game or use the "Bypass Calibration" option. Both are in the Avatars menu, under "Avatar Specific" in the settings (left-hand) pane.

## Contributing
To resolve references and automatically copy the compiled DLL into Beat Saber's installation directory, first create files called `CustomAvatar.csproj.user` and `CustomAvatar-Editor.csproj.user` next to `Source\CustomAvatar\CustomAvatar.csproj` and `Source\CustomAvatar-Editor\CustomAvatar-Editor.csproj` respectively. Then paste in the following contents:

### CustomAvatar.csproj.user
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <!-- Replace this with the path to your Beat Saber installation -->
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>

    <!-- To use the included Steam launch profiles and your Steam installation isn't in the default folder, change this -->
    <SteamExecutable>C:\Program Files (x86)\Steam\steam.exe</SteamExecutable>
  </PropertyGroup>
</Project>
```

### CustomAvatar-Editor.csproj.user
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <!-- Replace this with the path to your Beat Saber installation -->
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>

    <!-- Replace this with the path to your Custom Avatars Unity Project or leave it empty if you don't have one -->
    <UnityProjectDir>C:\Users\Me\Documents\AvatarsUnityProject</UnityProjectDir>
  </PropertyGroup>
</Project>
```

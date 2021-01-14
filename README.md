# Beat Saber Custom Avatars Plugin
[![GitHub Actions Build Status](https://img.shields.io/github/workflow/status/nicoco007/BeatSaberCustomAvatars/build/develop?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/actions?query=workflow%3Abuild+branch%3Adevelop)
[![Latest Release](https://img.shields.io/github/v/release/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/releases/latest)
[![License](https://img.shields.io/github/license/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/blob/master/LICENSE)

## Installing
Install [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) and [DynamicOpenVR](https://github.com/nicoco007/DynamicOpenVR/releases) using one of the many available mod installers for Beat Saber or download the latest version from [BeatMods](https://beatmods.com/).

Download [the latest release of Custom Avatars](https://github.com/nicoco007/BeatSaberCustomAvatars/releases) and extract **the entire contents** of the ZIP file into your Beat Saber folder (for Steam, this is usually `C:\Program Files (x86)\Steam\steamapps\common\Beat Saber`), **overwriting any existing files**.

## Usage
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

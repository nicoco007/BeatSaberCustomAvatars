# Beat Saber Custom Avatars Plugin
[![Jenkins Build Status](https://img.shields.io/jenkins/build/https/ci.gnyra.com/job/BeatSaberCustomAvatars/job/master?style=flat-square)](https://ci.gnyra.com/blue/organizations/jenkins/BeatSaberCustomAvatars/)
[![Latest Release](https://img.shields.io/github/v/release/nicoco007/BeatSaberCustomAvatars?include_prereleases&style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/releases/)
[![License](https://img.shields.io/github/license/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/blob/master/LICENSE)

## Installing
Install [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) using one of the many available mod installers for Beat Saber or download the latest version from [BeatMods](https://beatmods.com/).

Install [the latest release of DynamicOpenVR](https://github.com/nicoco007/DynamicOpenVR/releases) by [following the instructions in the repository's README](https://github.com/nicoco007/DynamicOpenVR/blob/master/README.md#Installing).

Download [the latest release of Custom Avatars](https://github.com/nicoco007/BeatSaberCustomAvatars/releases) and extract **the entire contents** of the ZIP file into your Beat Saber folder (for Steam, this is usually `C:\Program Files (x86)\Steam\steamapps\common\Beat Saber`), **overwriting any existing files**.

## Contributing
To resolve references and automatically copy the compiled DLL into Beat Saber's installation directory, first create files called `CustomAvatar.csproj.user` and `CustomAvatar-Editor.csproj.user` next to `Source\CustomAvatar\CustomAvatar.csproj` and `Source\CustomAvatar-Editor\CustomAvatar-Editor.csproj` respectively. Then paste in the following contents:

### CustomAvatar.csproj.user
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <!-- Replace this with the path to your Beat Saber installation -->
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>
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

    <!-- Replace this with the path to your Custom Avatars Unity Project (if you have one) -->
    <BeatSaberDir>C:\Users\Me\Documents\AvatarsUnityProject</BeatSaberDir>
  </PropertyGroup>
</Project>
```

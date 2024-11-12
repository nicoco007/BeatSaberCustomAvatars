# Beat Saber Custom Avatars Plugin
[![GitHub Actions Build Status](https://img.shields.io/github/actions/workflow/status/nicoco007/BeatSaberCustomAvatars/build.yml?branch=main&style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/actions?query=workflow%3Abuild+branch%3Amain)
[![Latest Release](https://img.shields.io/github/v/release/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/releases/latest)
[![License](https://img.shields.io/github/license/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/blob/master/LICENSE.txt)

## Getting Started
The easiest way to get Custom Avatars up and running is to use [ModAssistant](https://github.com/Assistant/ModAssistant). The latest version of Custom Avatars will always be posted in the [releases](https://github.com/nicoco007/BeatSaberCustomAvatars/releases) here, and then become available on ModAssistant a few days later. If you want to install it manually by using the releases available here or are looking for the files to create an avatar yourself, you can follow the instructions below.

### Full-Body Tracking
Full-body tracking requires [OpenXR Tracker Profiles](https://github.com/nicoco007/BeatSaber-ExtraOpenXRFeatures/tree/main/OpenXRTrackerProfiles) (which can be installed through mod managers or [downloaded and installed manually](https://github.com/nicoco007/BeatSaber-ExtraOpenXRFeatures/releases)) and setting up tracker roles in SteamVR. For instructions on how to do the latter, check out [OpenXR Tracker Profiles' instructions](https://github.com/nicoco007/BeatSaber-ExtraOpenXRFeatures/tree/main/OpenXRTrackerProfiles#assigning-steamvr-tracker-roles).

### Creating your own avatar
To get started, check out [the avatars guide on the BSMG wiki](https://bsmg.wiki/models/avatars-guide.html). If you have questions, join the [the BSMG Discord server](https://discord.gg/beatsabermods) and ask in the [#pc-3d-modeling](https://discord.com/channels/441805394323439646/468249466865057802) channel.

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

    <!-- Replace this with the path to your Custom Avatars Unity Project or leave it empty if you don't have one -->
    <UnityProjectDir>C:\Users\Me\Documents\AvatarsUnityProject</UnityProjectDir>
  </PropertyGroup>
</Project>
```

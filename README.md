# Beat Saber Custom Avatars Plugin
[![Jenkins](https://img.shields.io/jenkins/build/https/ci.gnyra.com/job/BeatSaberCustomAvatars/job/master?style=flat-square)](https://ci.gnyra.com/blue/organizations/jenkins/BeatSaberCustomAvatars/)
[![Release](https://img.shields.io/github/v/release/nicoco007/BeatSaberCustomAvatars?include_prereleases&style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/releases/)
[![GitHub](https://img.shields.io/github/license/nicoco007/BeatSaberCustomAvatars?style=flat-square)](https://github.com/nicoco007/BeatSaberCustomAvatars/blob/master/LICENSE)

## Contributing
Guidelines coming soon.

To automatically copy the compiled DLL into Beat Saber's installation directory, create a file called `CustomAvatar.csproj.user` next to `CustomAvatar\CustomAvatar.csproj` and paste in the following:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Replace this with the path to your Beat Saber installation -->
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```

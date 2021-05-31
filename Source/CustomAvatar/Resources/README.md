# Embedded Resources
This folder contains various resources used by Custom Avatars.

## icon.png
This is simply the mod icon used by BSIPA. It is referenced in the `manifest.json` file at the root of this project.

## shaders.assets
This file contains shaders used by Custom Avatars. Note that this does not contain shaders required to load avatars &ndash; shaders are included in avatar asset bundles and do not need to be loaded separately. This asset bundle is generated through this repo's Unity project.

## ui.dds
This file contains various UI elements. It is a DXT5 compressed DDS file that can be edited using [GIMP](https://www.gimp.org/) and should be exported with mip map generation enabled. Note that this image is flipped vertically because [Unity uses the DirectX convention while basically everything else (e.g. Windows, GIMP, Blender) uses the OpenGL convention](https://developer.blender.org/T59867). The coordinate system when looking at this flipped image has its origin in the top-left corner; for example, the mystery man icon starts at (256, 0) and has a size of (256, 256).
# VectorToCursor

Converts an SVG file into a Windows cursor (`.cur`) with images at 32, 48, 64, 96, 128 and 256 px. Each size gets its own scaled hotspot. It is a .NET 10 NativeAOT console app built with System.CommandLine, ImageSharp and Svg.Skia.

## Usage

```
VectorToCursor <input> -x <x> -y <y> [-o <file>]
```

| Argument / option | Meaning |
|---|---|
| `<input>` | The SVG file to convert. |
| `-x`, `--hotspot-x` | Hotspot X in SVG coordinates (required). |
| `-y`, `--hotspot-y` | Hotspot Y in SVG coordinates (required). |
| `-o`, `--output` | The cursor file to write. Defaults to the input path with a `.cur` extension. An existing file is overwritten. |

Example:

```
> VectorToCursor VectorToCursor.Tests/TestData/arrow.svg -x 3 -y 2
Created ...\arrow.cur
  Size  Hotspot
    32  3,2
    48  4,3
    64  6,4
    96  9,6
   128  12,8
   256  24,16
```

### Hotspot

- **Coordinates:** use the SVG's own coordinate system, the units of its `viewBox`, or pixels if it has none. You can read the point straight from your vector editor.
- **Number format:** `.` is the decimal separator. Negative values are valid when the viewBox starts below zero.
- **Scaling:** for each size the point is scaled like the artwork, then rounded down to the pixel that contains it.
- **Range:** a hotspot outside the cursor square is rejected.

### Rendering

- **Rendering:** every size is drawn directly from the vector data, never downscaled from a larger image.
- **Non-square artwork:** scaled to fit and centered on whole pixels; the margins stay transparent.
- **Storage:** sizes 32 to 128 px are stored as 32-bit BMP and 256 px as PNG.
- **Requirements:** the SVG needs a `viewBox` or an absolute `width`/`height`. A `transform` on the root `<svg>` element is not supported; wrap the content in a `<g>` instead.
- **External resources:** only files in the SVG's folder and `data:` URIs are loaded. Nothing is fetched from the network.

## Prerequisites

- **.NET 10 SDK.**
- **ImageSharp license key.** Apply at https://licensing.sixlabors.com; it is free for open-source projects and companies under USD 1M revenue.
  - Put `sixlabors.lic` in the repository root.
  - Without it, Debug builds only warn, but Release builds and `dotnet publish` fail.
  - The file is git-ignored; never commit it. In CI, pass the key with `-p:SixLaborsLicenseKey=...` instead.
- **NativeAOT publishing** needs Visual Studio or Build Tools 2022 or later with the *Desktop development with C++* workload.

## Build, test, publish

```
dotnet build
dotnet test
dotnet publish VectorToCursor -c Release -r win-x64
```

`libSkiaSharp.dll` and `libHarfBuzzSharp.dll` must stay next to `VectorToCursor.exe`. The `.pdb` files are only needed for debugging.

**Troubleshooting:** if publish fails with `"vswhere.exe" is not recognized`, the environment variable `NoDefaultCurrentDirectoryInExePath` is set. Add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to `PATH`.

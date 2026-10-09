# <img src="https://raw.githubusercontent.com/Der-Floh/VectorToCursor/main/Assets/icon.svg" alt="VectorToCursor icon" height="64"> VectorToCursor

[![GitHub Release](https://img.shields.io/github/v/release/Der-Floh/VectorToCursor)](https://github.com/Der-Floh/VectorToCursor/releases/latest)
[![GitHub Downloads](https://img.shields.io/github/downloads/Der-Floh/VectorToCursor/total)](https://github.com/Der-Floh/VectorToCursor/releases)
[![CI](https://github.com/Der-Floh/VectorToCursor/actions/workflows/ci.yml/badge.svg)](https://github.com/Der-Floh/VectorToCursor/actions/workflows/ci.yml)

Converts an SVG file into a Windows cursor (`.cur`), or an animated cursor (`.ani`) when the SVG is animated. The file holds one image per size, by default 32, 48, 64, 96, 128 and 256 px, and each size gets its own scaled hotspot. It is a .NET 10 NativeAOT console app built with System.CommandLine, ImageSharp and Svg.Skia, and installed with Velopack.

## Installation

Download the setup or the portable archive for your processor from the [latest release](https://github.com/Der-Floh/VectorToCursor/releases/latest): `win-x64` for most PCs, `win-arm64` for Windows on Arm, `win-x86` for 32-bit Windows.

- **Setup** (`VectorToCursor-win-x64-Setup.exe`): installs into `%LocalAppData%\VectorToCursor` without administrator rights and adds its `current` folder to your user `PATH`, so `VectorToCursor` runs in every terminal you open afterwards. When Setup is done, it opens a window that tells you how to start. Uninstalling under *Settings > Apps > Installed apps* removes the `PATH` entry again.
- **Portable** (`VectorToCursor-win-x64-Portable.zip`): extract it anywhere. It holds a `VectorToCursor` folder with `VectorToCursor.exe` and the two DLLs it needs, which must stay together. Nothing is installed and `PATH` stays as it is.

## Usage

```txt
VectorToCursor <input> -x <x> -y <y> [-o <file>] [--sizes <list>] [--image-format <bmp|png>] [--bleed <percent>] [--fps <rate>]
```

| Argument / option | Meaning |
|---|---|
| `<input>` | The SVG file to convert. CSS or SMIL animations make it an animated cursor; see [Animated cursors](#animated-cursors). |
| `-x`, `--hotspot-x` | Hotspot X in SVG coordinates (required). |
| `-y`, `--hotspot-y` | Hotspot Y in SVG coordinates (required). |
| `-o`, `--output` | The cursor file to write: `.cur` for a static SVG, `.ani` for an animated one. Defaults to the input path with that extension. An existing file is overwritten. |
| `--sizes` | The image sizes in px, from 1 to 256, separated by commas (default `32,48,64,96,128,256`). |
| `--image-format` | How every image in the file is stored: `bmp` or `png`. Defaults to `bmp` for `.cur`; for `.ani` to `bmp` when the frames fit, otherwise `png`. See [Storage](#storage). |
| `--bleed` | Width of the color band around the artwork, in percent of each cursor size (default 5, `0` turns it off). See [Transparent pixels](#transparent-pixels). |
| `--fps` | Frames per second of an animated cursor; must divide 60 (default 30). Ignored for static SVGs. |

Examples:

```txt
> VectorToCursor VectorToCursor.Tests/TestData/arrow.svg -x 3 -y 2
Created ...\arrow.cur with BMP images
  Size  Hotspot
    32  3,2
    48  4,3
    64  6,4
    96  9,6
   128  12,8
   256  24,16

> VectorToCursor VectorToCursor.Tests/TestData/css-blink.svg -x 3 -y 2 --sizes 32,48,64
Created ...\css-blink.ani with BMP images: 6 frames at 30 fps (0.2 s)
  Size  Hotspot
    32  3,2
    48  4,3
    64  6,4
```

### Hotspot

- **Coordinates:** use the SVG's own coordinate system, the units of its `viewBox`, or pixels if it has none. You can read the point straight from your vector editor.
- **Number format:** `.` is the decimal separator. Negative values are valid when the viewBox starts below zero.
- **Scaling:** for each size the point is scaled like the artwork, then rounded down to the pixel that contains it.
- **Range:** a hotspot outside the cursor square is rejected.

### Rendering

- **Rendering:** every size is drawn directly from the vector data, never downscaled from a larger image.
- **Non-square artwork:** scaled to fit and centered on whole pixels; the margins stay transparent.
- **Requirements:** the SVG needs a `viewBox` or an absolute `width`/`height`. A `transform` on the root `<svg>` element is not supported; wrap the content in a `<g>` instead.
- **External resources:** only files in the SVG's folder and `data:` URIs are loaded. Nothing is fetched from the network.

### Animated cursors

- **Detection:** CSS animations (`@keyframes` with the `animation` properties) and SMIL elements such as `<animate>` and `<animateTransform>` make the output an `.ani` file.
- **Loop:** one loop lasts until every animation repeats exactly, the least common multiple of their lengths. Loops longer than 60 s are rejected.
- **Frames:** the loop is sampled evenly at `--fps` frames per second. The rate must divide 60, because animated cursors count time in sixtieths of a second, and a loop may have at most 1,800 frames.
- **Hotspot:** every frame shares the same hotspot.
- **CSS support:** opacity, fill and stroke with their opacities, stroke width, dash array and dash offset, and the transforms rotate, translate, scale and skew, eased with linear, the `ease` keywords, `cubic-bezier()` without overshoot or single steps. Anything that can't be played back exactly is reported as an error instead of producing a different animation.

### Storage

- **One format per file:** every image of a cursor is stored the same way. Windows ignores the PNG images of a cursor that also holds BMP images and scales up a smaller BMP instead.
- **`bmp`:** 32-bit BMP, which every program that reads cursor files can load. The default for `.cur` files.
- **`png`:** a fraction of the size, and Windows draws it exactly like BMP at every size. Some programs that load cursor files themselves, such as WinForms, reject it.
- **64 KB limit of animated cursors:** every frame of an `.ani` file is a complete cursor file, and Windows refuses the whole animation when an image starts 64 KB (65,536 bytes) or more into its frame, however large or small the file is. Images are stored smallest first. As BMP they take:

  | Size | 32 px | 48 px | 64 px | 96 px | 128 px | 256 px |
  |---|---:|---:|---:|---:|---:|---:|
  | BMP image | 4.3 KB | 9.6 KB | 16.9 KB | 38.1 KB | 67.6 KB | 270.4 KB |

  So with the default sizes the 128 px image already starts at 69 KB. BMP frames fit with, for example, `--sizes 32,48,64,96`, `48,64,96,128` or `32,48,64,256`, but never with 128 and 256 px together. PNG frames are far smaller and only reach the limit with very noisy artwork.
- **Default for `.ani` files:** `bmp` when its frames fit, which the first frame decides because BMP frames all have the same length; otherwise `png`. With the default sizes that means `png`.
- **Check:** every frame is checked as soon as it is rendered. A frame that doesn't fit stops the conversion with an error that names the frame, the image and the byte it starts at, and no file is written.

### Transparent pixels

- **Why they get a color:** Windows scales a cursor whenever no image matches the requested size (for example 80 or 144 px), and it does so without premultiplying alpha. The color of fully transparent pixels then bleeds into the edges; left black, it shows up as a dark fringe.
- **Band:** within `--bleed` percent of the cursor size (default 5%, rounded up: 2 px at 32 px up to 13 px at 256 px), transparent pixels take the color of the nearest edge pixel.
- **Beyond the band:** all remaining transparent pixels share one color per image, the average of the band's outer edge. Windows never samples that far from an edge, and a flat area keeps PNG images small.
- **Noise filter:** pixels below alpha 16 don't pass on their color, because after un-premultiplying it is mostly rounding noise; they take the nearest reliable edge color instead. If the whole artwork is that faint, every visible pixel counts.
- **Unchanged look:** alpha is never changed, so at native sizes the cursor looks exactly as rendered.

## 📜 License

Licensed under the [MIT License](./LICENSE).

<br>
<br>

[!["Buy me a coffee"](https://raw.githubusercontent.com/Der-Floh/Der-Floh/refs/heads/main/_meta/BuyMeACoffee/Buttons%20%26%20Icons/orange-button-x180.png)](https://www.buymeacoffee.com/der_floh)

# Noto Sans Fonts for Multilingual Support

This folder contains Noto Sans fonts for the multilingual Flowery.WinUI Gallery.

## Active Variable Fonts

`GalleryResources.xaml` uses these variable fonts. Each file contains weights 100 through 900, including Regular (400), SemiBold (600), and Bold (700).

| File | Font family | Script coverage |
| --- | --- | --- |
| `NotoSans-Variable.ttf` | Noto Sans | Latin, Cyrillic, Greek |
| `NotoSansSC-Variable.ttf` | Noto Sans SC | Simplified Chinese |
| `NotoSansJP-Variable.ttf` | Noto Sans JP | Japanese |
| `NotoSansKR-Variable.ttf` | Noto Sans KR | Korean |
| `NotoSansArabic-Variable.ttf` | Noto Sans Arabic | Arabic |
| `NotoSansHebrew-Variable.ttf` | Noto Sans Hebrew | Hebrew |

WinUI selects the weight within each file through `FontWeight`. Separate static files do not provide automatic weight selection for a file-based `FontFamily`.

These files are unmodified downloads from [Google Fonts revision 45e3d5269e4a17d823f6d84470bc084dc5ae46f6](https://github.com/google/fonts/tree/45e3d5269e4a17d823f6d84470bc084dc5ae46f6/ofl).
The source folders are `notosans`, `notosanssc`, `notosansjp`, `notosanskr`, `notosansarabic`, and `notosanshebrew`.
Upstream filenames use `[wght]` or `[wdth,wght]` instead of `-Variable`.
Each family has an accompanying `*-OFL.txt` file with its copyright notice and license.

`Flowery.WinUI.GalleryAssets.props` copies these files and licenses to the Gallery and Kiosk build and publish output.

## Existing Static Fonts

`PhotosResources.xaml` uses these static fonts.
The Gallery app excludes them and `StaticFonts-OFL.txt` from build and publish output.

| File | Purpose | Size |
| --- | --- | --- |
| `NotoSans-Regular.ttf` | Latin, Cyrillic, Greek | ~560KB |
| `NotoSansSC-Regular.otf` | Simplified Chinese (Standalone) | ~16MB |
| `NotoSansJP-Regular.otf` | Japanese (Standalone) | ~16MB |
| `NotoSansKR-Regular.otf` | Korean (Standalone) | ~16MB |
| `NotoSansArabic-Regular.ttf` | Arabic script | ~240KB |

**Total Size:** ~48MB (Note: Builds may be large, but this ensures full CJK support)

These files are unmodified upstream files:

- `NotoSans-Regular.ttf` and `NotoSansArabic-Regular.ttf` come from `hinted/ttf` in [notofonts/noto-fonts](https://github.com/notofonts/noto-fonts).
- The CJK files are `NotoSansCJKsc-Regular.otf`, `NotoSansCJKjp-Regular.otf`, and `NotoSansCJKkr-Regular.otf` from `Sans/OTF` in [notofonts/noto-cjk](https://github.com/notofonts/noto-cjk), renamed.

`StaticFonts-OFL.txt` contains their copyright notices and the license.
The notices match the font metadata and the upstream license files.
The Photos project copies these files and `StaticFonts-OFL.txt` to build and publish output.

## License

These fonts are from the [Google Noto Fonts](https://fonts.google.com/noto) project and are licensed under the **SIL Open Font License, Version 1.1 (OFL-1.1)**.
The repository's MIT license does not apply to these fonts.

### What This Means

✅ **Free to use** - in personal and commercial projects  
✅ **Free to distribute** - can be bundled with applications  
✅ **Free to modify** - derivative works are allowed  
The copyright notice and license must accompany redistributed font files.
Keep each `*-OFL.txt` file in the same `Assets/Fonts` folder as the fonts it covers, including in build, publish, and installer output.
The build and publish fail if a required font or license file is missing from `Assets/Fonts`.

The OFL is one of the most permissive font licenses available. The full license text is available at:
https://scripts.sil.org/OFL

For a practical guide on using OFL fonts, see:
https://openfontlicense.org/how-to-use-ofl-fonts/

> *"The OFL allows the licensed fonts to be used, studied, modified and redistributed freely as long as they are not sold by themselves."*

## Why These Are Committed

The apps ship these fonts as separate files in `Assets/Fonts`, not as embedded assembly resources.
The Gallery uses them to render its language dropdown and localized UI text for all supported languages on Windows.

## Configuration

`GalleryResources.xaml` defines the font family and fallback chain:

```xml
<FontFamily x:Key="NotoSansFamily">
    ms-appx:///Assets/Fonts/NotoSans-Variable.ttf#Noto Sans,
    ms-appx:///Assets/Fonts/NotoSansSC-Variable.ttf#Noto Sans SC,
    ms-appx:///Assets/Fonts/NotoSansJP-Variable.ttf#Noto Sans JP,
    ms-appx:///Assets/Fonts/NotoSansKR-Variable.ttf#Noto Sans KR,
    ms-appx:///Assets/Fonts/NotoSansArabic-Variable.ttf#Noto Sans Arabic,
    ms-appx:///Assets/Fonts/NotoSansHebrew-Variable.ttf#Noto Sans Hebrew
</FontFamily>
```

This font family is then applied globally via styles for `TextBlock`, `TextBox`, `Button`, `ComboBox`, `ComboBoxItem`, and `ContentControl`.

> **Note:** If CJK characters don't render correctly, verify the font family names (after `#`) match the internal font names. You can check fonts by opening them in a font viewer.

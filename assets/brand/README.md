# Brand assets

`meridian-logo-source.jpeg` is the supplied artwork: 1600×682, light-on-black,
no transparency.

Everything the application uses is derived from it into
`src/DentalSurgery.Web/wwwroot/brand/`. Nothing here is served; this folder is
the source of record, so the derived assets can be rebuilt if the artwork
changes.

| Derived | Used by | Why it exists |
|---|---|---|
| `logo.png` | PDF letterhead | The black background is keyed out. Dropped straight in, the JPEG shows as a black box on the white page. |
| `logo-on-dark.png` | Sign-in panel | "MERIDIAN" is `#002A52`, all but invisible on the `#16202e` sidebar. Each pixel is scaled to a common peak, which lifts the wordmark while keeping the blue. |
| `mark.png` | Sidebar, sign-in header | The wordmark is unreadable below about 160px, so compact placements get the tooth and orbit alone. |
| `favicon.png` | Browser tab | The mark at 180×180. |

The background is keyed on **colourfulness as well as brightness**. The artwork
is blue and the background is black, but the tooth's dark interior and the
JPEG's compression noise are dark *neutral* pixels: a brightness-only key keeps
them as grey haze, invisible against the original black and dirty against white.

Each derived file is sized for where it is actually drawn, at 2× for
high-density screens. The full-resolution art was several hundred kilobytes on
every page load to fill a 32-pixel square.

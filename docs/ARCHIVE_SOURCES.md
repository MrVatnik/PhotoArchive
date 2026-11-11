# Archive sources (input for the folder importer)

The photographer's archive (`Storage:OriginalsRoot`, e.g. `D:\img\Films`) predates the app and holds more than the database: **107 roll folders, of which 22 are in the DB** (Nov 2026). This document records how that archive is organised so an importer can read it. It was checked against the 22 films already in the DB.

## 1. Roll folders

Name: `yy-MM-dd_{col|bw|BW}_{Camera}`, e.g. `21-05-30_bw_FED2`, `23-10-19_bw_Kiev-19`. This is the same format as `Film.ToString()`.

- The date is when the roll was developed or scanned, not necessarily when it was shot.
- Suffix `-RESCAN` (2 folders) marks a second scan of the same roll; the importer should offer it as an alternative folder, not as a new film.
- Not rolls: `папа маленький` (sub-folders `1`…`10`), and the `.docx`/`.bak` files at the root.

### Where the images are

| Layout | Count | What the importer should use |
|--------|-------|------------------------------|
| JPEGs directly in the roll folder | most | the roll folder |
| TIFF scans in the roll folder + JPEG exports in a sub-folder | ~25 | the sub-folder with JPEGs. Usually it has the roll's own name (`21-11-07_bw_F-1/21-11-07_bw_F-1`), sometimes `Export`, `эдитед` or `22-04-04 цв F-1` |
| `CaptureOne/` sub-folder | 4 | ignore (editor sidecars: `.cop/.cof/.cot/.cos`) |
| `squares/` (21-12-17) | 1 | ignore (derived crops) |

`Film.FolderName` may therefore contain a sub-path (film 1017 does). Browsers can't show TIFF and SkiaSharp can't decode it, so TIFF-only folders need a JPEG export first.

## 2. `Пленка+Аппарат.txt` ("film + camera")

104 of 107 folders have one (2 are named `Пленка + аппарат.txt`). Missing: `16-03-01_col_Smena8m`, `23-07-13_col_Vera`, `папа маленький`. UTF-8 or ASCII.

```
<film stock> [ISO] [as|как <EI>] [+ <developer>] + <camera> [+ <lens>]
[стр <N>]                        ← second line, from 2023-03; the paper album page
```

Examples: `Foma pan 100 как 50 + Зенит 3м` · `aviphot as 800 + ФГ31П + Зоркий 1 + Юпитер 12` · `Kodak Vision 3 250d as 320 + canon f-1`

Parsing rules:
- Split on `+` and trim; drop empty parts (`FOMADON R09 + + Zeiss…` occurs).
- The **camera is the part that matches a known camera** (case-insensitive, alias list below). It isn't always last: Zorki rolls end with the lens `Юпитер 12`.
- Part 1 = film stock: optional box ISO, then `as N`, `как N`, `as N EI` or a range `as 50-100` for the EI. With no EI, EI = box ISO. **The ЛК-А (LC-A) can only be set to 25, 50, 100, 200 or 400** (no 125), so any other EI snaps to the nearest setting, e.g. Aerocolor 125 is shot at 100.
- A middle part = developer. Colour rolls usually have none, which means lab C-41; some say `с41`, `e6`/`E6`.
- Free-text notes appear in brackets: `(~2007)`, `(китайская копия Cinestill 800T …)`.

### Aliases (confirmed by the owner)

| In the files | Means |
|--------------|-------|
| Rollei Superpan 200 | **Agfa Aviphot 200** (same emulsion) |
| Silberra color | **Kodak Aerocolor 125** (re-spooled) |
| кн2, КН-2 | **КН-2**, an old Soviet B&W negative stock; its box speed isn't recorded, so the ISO defaults to the EI from the notes |
| aviphot, Aviphot, Agfa Aviphot | Agfa Aviphot 200 |
| FOMADON R09, R09 | **Rodinal** |
| AF pyro350, PYRO350, Pyro 350, pyro350, пиро 350 | Pyro 350 (one developer) |
| ФГ31П, ФГ31п | ФГ-31П |
| fujicolor c200 | Fujifilm C 200 |
| foma 400, Fomapan 400, Foma pan 400 | Foma pan 400 (same for 100/200) |
| canon f-1, Canon F-1 | Canon F-1 |
| Зенит 3м | Зенит 3М |
| ЛК-А, LCA | ЛК-А (LOMO LC-A) |
| Zeiss Ikon, Zeiss Ikon Maximar 207/1 | Zeiss Ikon Maximar 207/1 |

New cameras not yet in the DB: Zeiss Ikon Maximar 207/1, Minoltaflex II, Зоркий 1, Киев-19, Чайка 3, ФЭД Микрон-2, Vera.

### Checked against the 22 films in the DB

All 21 files agree with the DB once the aliases are applied:
- Superpan/R09 on 21-11-07 and 21-12-17 = Aviphot/Rodinal ✔
- Aerocolor 125 on ЛК-А at EI 100 ✔ (the LC-A has no 125 setting; its other rolls in the DB are at box speed: 200 and 400)
- 21-10-14 Silberra Ultima 100: the file says `D76`, the DB says "BW in lab". The owner confirms it was lab-developed (probably D76, not certain), so the DB is kept as is.

## 3. `Плёнки описание.docx`, the paper negative album

The index of the physical negative album, up to mid-2022. Sleeves hold **7 strips per page with up to 6 frames each**. The app's `Page` / `Line` / `Place_In_Line` fields model this album.

- **Table 1, roll index:** for each of 41 rolls, the start and end address `page – line` (e.g. `11 – 1 … 11 – 7`), the film stock, the camera and a date (sometimes "shot (developed)", e.g. `7.11.21 (28.11)`). It covers album pages 1–36; a roll may start mid-page and run onto the next.
- **Tables 2+, captions:** one row per strip (`p-l`), followed by a short description of each frame. There are 271 strips on pages 1–40 and about 1 230 captions. They are personal notes; treat them as private data (good candidates for `Photo.Notes`).

### Checked against the DB

- Photos placed on pages 1–16 sit on exactly the album pages the docx gives for their roll.
- **Page 0 means "not placed"**: 186 photos in 15 films (frames that were scanned but aren't in the album index), including all of `16-03-01_col_Smena8m` and `21-10-05_bw_Zenit-3m`, which aren't in the docx.
- Matching docx rolls to folders by date alone is ambiguous. Two rolls share 05.10.21, and the docx dates Vision3 250D 24.11.21 while its folder says 21-12-01. Match on **date ± 2 weeks + film stock + camera** instead.
- From 2023 the `стр N` line in the `.txt` files continues the album numbering (pages 61–94); the docx stops at about page 40.

## Importer rules (implemented in `Services/Import`, see ARCHITECTURE.md)

1. List folders not in `Films.FolderName`; skip non-roll folders; treat `-RESCAN` as an alternative scan.
2. Pick the image folder: top level if it has JPEGs, otherwise the JPEG sub-folder; ignore `CaptureOne`/`squares`.
3. Parse the folder name → date, colour/BW, camera hint. Parse `.txt` → stock, EI, developer, camera, lens, album page. Normalise everything through the alias table and create missing cameras, stocks, developers and recipes on confirmation.
4. Album placement: use `стр N` or the docx roll address when available, otherwise put the photos on page 0 (unplaced). Frames per strip follow the format: the docx shows 6x9 rolls (8 frames) taking 4 strips, i.e. 2 frames per strip; 6x6 takes 3 per strip.
5. Show everything for review before writing; never move or rename files.

### Dry run against the 2023 backup (Nov 2026)

84 unregistered roll folders were found (plus `папа маленький`, skipped). 79 were pre-selected and imported cleanly into a test copy: 2 192 photos, 4 new cameras (Minoltaflex II, Зоркий 1, Киев-19, Чайка 3), 1 new format (Средний 6x6), 3 developers (ФГ-31П, E-6, Moersch Tanol), 15 film types and 33 recipes. Five rows were left unchecked by that run; the owner's answers:

| Folder | Why it is unchecked |
|--------|---------------------|
| `22-06-01_col_F-1` | Silberra color = Kodak Aerocolor 125 → now pre-filled and checked |
| `22-07-29_bw_LCA` | `кн2 как 25` = Soviet КН-2 at EI 25 (ISO 25 assumed) → now pre-filled and checked |
| `23-07-13_col_Vera` | no `.txt`; **not to be imported yet** (stays unchecked, and can't be imported without a stock and ISO) |
| `23-06-25_BW_MinoltaflexII-RESCAN`, `23-06-26_BW_MinoltaflexII-RESCAN` | second scans of rolls that are imported from their non-RESCAN folder |

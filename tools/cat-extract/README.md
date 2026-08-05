# tools/cat-extract

`verify_content.py` is a stdlib-only Python 3 checker for the gameplay catalog transcription under
`content/`. It parses every `.json` file in the tree and fails loudly on any parse error; asserts the
per-directory entry counts from the design docs (8 resources, 6 mechs, 10 enemies plus 1 elite
modifier profile, 4 bosses, 15 weapons, 45 branches, 12 utilities plus the radar, 10 relics, 13
PowerUps with 58 rank rows in total, 6 unlocks, 4 mining-site classes, the 35-minute encounter
schedule with 4 Hyper Gold beacon responses and 7 formations, and the map contract plus 2 world
props) from an expectations table at the top of the file where every row cites the source doc and
line; checks that every file carries a well-formed `_provenance` block (and every nested `_source`
block) whose `doc` path exists on disk; recomputes the two doc-stated grand totals from the JSON
(PowerUp rank prices must sum to 9,450 Hyper Gold, option-unlock costs to 2,150); and checks
referential integrity for `weaponId` in `content/branches/` → `content/weapons/`, `EN-xx` references
in `content/encounters/` → `content/enemies/`, and signature-weapon references in `content/mechs/` →
`content/weapons/`. Entries whose `id` is `null` (the known cases where no design document assigns a
stable ID) are printed as warnings, not failures. It performs no JSON Schema validation — the
canonical schemas under `content/schemas/` do not exist yet.

Run it from anywhere; it locates the repository root relative to its own path:

```sh
python3 tools/cat-extract/verify_content.py
```

It prints a per-check summary table either way and exits non-zero if any check fails. Warnings never
affect the exit code. When an expected count changes because a design document changed, edit the
`EXPECTATIONS`/`PROBES` tables and update the `source` citation on that row in the same commit.

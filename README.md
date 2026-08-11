# Mecha Miner Survivor

A survivor-like action game in which science-fiction mechs fight alien monsters. It removes experience points, replacing experience-gem and treasure-chest progression with map exploration, resource mining, positional commitment, and upgrade crafting.

Godot 4.7.1 (.NET / C#), authoritative 60 Hz simulation, Windows and Steam Deck targets.

## Where the project is

Measured against `master` at `e17b8b6`, 11 Aug 2026. This block is refreshed after major merges rather than continuously; `git log --oneline e17b8b6..origin/master` shows exactly what has landed since, which is exactly what this block has not accounted for.

**Playable today: a mech you can drive around an empty test arena.** No enemies, no weapons, no mining, no HUD. What it does do, verified by execution at the sha below: the mech moves at 3.0 m/s under the authoritative 60 Hz simulation, turns to face the direction it is travelling, stops with its collision circle tangent to the wall of a 40 m square arena, and is shown by a non-rotating orthographic north-up camera whose vertical extent is 24 gameplay metres.

The slice has merged — but into another branch, not into `master`. See [Try it out](#try-it-out) for the branch to check out.

| | Area | State |
| --- | --- | --- |
| ✅ | Design + technical specification | Complete, on `master` (`docs/`) |
| ✅ | Content catalog | 138 JSON definitions on `master` — 15 weapons, 45 branches, 6 mechs, 10 enemies, 4 bosses, 10 relics, 13 utilities, 13 power-ups (116), and 22 more: 8 resources, 6 unlocks, 4 mining sites, 1 map contract, 1 encounter schedule, and the shared weapon-price and elite-modifier rule files. This is the number the content gate asserts — `python3 src/MechaMiner.Tools/ContentImport/verify_content.py` reports 138 definition `*.json` files under `content/`, excluding `localization/` and `schemas/`, which is why the tree also holds a 139th JSON file (`content/localization/en.json`) that is not a definition. |
| 🚧 | Build + toolchain | Works on a branch. `master` carries two provisioning scripts — `build/bootstrap-linux.sh` and `build/bootstrap-macos.sh`, the latter with its static `build/verify-bootstrap-macos.sh` gate — but **no `./build.sh`** and no CI. |
| 🚧 | Simulation core | Written, 252 tests passing in `MechaMiner.Simulation.Tests`, not on `master` — [#11](https://github.com/pwestling/mecha-miner-survivor/pull/11) was open and marked ready for review when this block was last checked on 6 Aug 2026, with head `claude/hearth-thread-3aamx2` and base `claude/hearth-thread-2vmaro-fnd-002` |
| 🚧 | Playable slice | Movement only. [#19](https://github.com/pwestling/mecha-miner-survivor/pull/19) **merged** on 6 Aug 2026 — into `claude/hearth-thread-3aamx2` (merge commit `5f9e28c`), **not** into `master`. It now rides that branch instead of one of its own, and three merges still separate it from `master`: [#11](https://github.com/pwestling/mecha-miner-survivor/pull/11), [#3](https://github.com/pwestling/mecha-miner-survivor/pull/3), and a pull request nobody has opened yet. |
| ⬜ | Everything else | Not started |

Because #19 merged into #11's head branch, #11's own diff now carries the playable slice as well as the simulation core.

### Milestones

Full definitions: `docs/technical/110-implementation-plan-for-ai-agents.md` § Milestone gates.

| | | |
| --- | --- | --- |
| **M0** | Reproducible foundation — a clean checkout builds, tests, and imports the Godot project | 🚧 in progress |
| **M1** | Headless simulation skeleton — clocks, entities, commands, events, RNG, snapshots | 🚧 built, on a branch, not on `master` |
| **M2** | Combat graybox — one mech, pursuing enemies, one weapon, hull/HUD, 60 FPS | 🚧 movement only, on a branch, not on `master` |
| **M3** | Core differentiator slice — mining seams, resources, fabrication, radar, map | ⬜ |
| **M4** | Internal gameplay demo — a 14-minute scenario, two mechs, four weapons, six enemies, two bosses | ⬜ |
| **M5** | Full standard run — all 15 weapons and 45 branches, six mechs, the 35-minute schedule | ⬜ |
| **M6** | Content and performance production readiness | ⬜ |
| **M7** | Release candidate — exports, Steam staging, release checklist | ⬜ |

Everything executable is stacked in a chain of unmerged branches, each one based on the next rather than on `master`:

`claude/hearth-thread-3aamx2` ([#11](https://github.com/pwestling/mecha-miner-survivor/pull/11)) → `claude/hearth-thread-2vmaro-fnd-002` ([#3](https://github.com/pwestling/mecha-miner-survivor/pull/3)) → `claude/hearth-thread-2vmaro` → `master`

The playable slice ([#19](https://github.com/pwestling/mecha-miner-survivor/pull/19)) has already merged into `claude/hearth-thread-3aamx2`, so it rides that branch now instead of one of its own; that moved it one link along and did not put it on `master`. Of the two remaining pull requests, #3 was still a draft and #11 had been marked ready for review when this page was last checked on 6 Aug 2026. The `./build.sh` command surface and CI both live on `claude/hearth-thread-2vmaro-fnd-002` and arrive on `master` in the same reconcile, which is why `master` currently has neither — it holds the specification, the content catalog, and an empty Godot shell. The last hop is not pending work: `claude/hearth-thread-2vmaro` already merged to `master` once ([#1](https://github.com/pwestling/mecha-miner-survivor/pull/1)) and is an ancestor of it today — 0 commits ahead of `master`, however far `master` has since moved past it — so it needs a fresh pull request only once #3 lands on it.

## Try it out

**Linux x86-64 and macOS on Apple Silicon have both been run.** The Linux steps were executed from a fresh clone on 6 Aug 2026 and the game launched. The macOS steps were executed on Apple Silicon under macOS 26.6 and the game launched with the mech drivable from the keyboard. The two runs establish different things — see [What has and hasn't been verified](#what-has-and-hasnt-been-verified) for what each one did and did not cover. Windows has never been run.

You will need `git`, `curl` and `unzip`, and then two pinned tools:

- **.NET SDK 10.0.302** — exactly this patch, not "latest .NET 10"
- **Godot 4.7.1-stable, mono flavour** — the .NET build; the standard build cannot run this project

### Linux (x86-64, Debian/Ubuntu) — verified

```bash
sudo apt-get update && sudo apt-get install -y git curl unzip
git clone https://github.com/pwestling/mecha-miner-survivor.git
cd mecha-miner-survivor
git checkout claude/hearth-thread-3aamx2   # exists only until the scene reaches master
```

If that checkout fails with `did not match any file(s) known to git`, the branch has been deleted or renamed — do **not** just stay where you are and carry on. `master` carries neither `./build.sh` nor `game/scenes/Run.tscn`, so every command in the next block fails there: the `./build.sh` lines with exit 127, command not found, and the launch with a missing scene. Run `git fetch origin` and try the checkout again; if it still fails, `git branch -r | grep hearth-thread` shows where the playable scene lives now. If the work has genuinely landed on `master`, then `game/scenes/Run.tscn` is there — check that it is before you continue. `claude/hearth-thread-3aamx2` exists for exactly as long as the merge chain above is unfinished. To pin the precise tree the verification below describes, `git checkout 5f9e28cc` instead — or `git checkout dccc9588`, the sha the measurements were taken at, whose tree is identical to `5f9e28cc`'s. A sha never disappears, but it leaves you on a detached HEAD. (The earlier branch this page named, `claude/ui-002-first-playable`, is the merged side of #19; it is no longer the branch to check out and may be deleted at any time.)

```bash
sudo build/bootstrap-linux.sh   # once per machine, about a minute
./build.sh doctor               # expect exit 0: "pinned toolchain verified; 10 probes, 0 mismatches"
./build.sh build
./build.sh godot-import

godot --path game res://scenes/Run.tscn
```

If that last line reports it cannot load `res://scenes/Run.tscn`, the game is not broken — you are on the wrong ref. `game/scenes/Run.tscn` is on `claude/hearth-thread-3aamx2` and is not on `master`. Do not work around it by dropping the scene argument; that runs `Boot.tscn`, which *is* on `master` and renders nothing, so you get a blank window and a worse diagnosis. Go back to the checkout above.

WASD or the arrow keys drive the mech at 3 m/s; a gamepad left stick works too. The camera is orthographic and north-up. There is no HUD, no pause and no quit button — close the window.

`build/bootstrap-linux.sh` installs the .NET SDK to `/usr/share/dotnet`, Godot to `/opt/godot` with a `/usr/local/bin/godot` symlink, and the `mesa-vulkan-drivers` package. It must run as root and does not call `sudo` itself, so invoke it with `sudo`. `/usr/share/dotnet` is not a free choice: it is hostfxr's default probe path, and Godot's .NET host finds the runtime there without `DOTNET_ROOT`. Re-running the script is safe — it revalidates and skips what is already correct. You do not need to re-run it per clone.

On a machine with no display, also `sudo apt-get install -y xvfb` (the bootstrap script does not install it) and prefix the launch with `xvfb-run -a`.

### Running the pinned tools directly — verified on Linux

You can skip `./build.sh` entirely and drive the two pinned tools yourself. This is the only route on this page that invokes no gate script, which is why the macOS section below is built on it: it is that section's primary path and the only one a Mac has run, and because no gate script is involved it needs none of the GNU userland that section opens with. This three-command sequence was executed on Linux from a fresh clone at `5f9e28cc` with **no `./build.sh` verb invoked at all**, and reached a running scene:

```bash
dotnet build game/MechaMiner.Game.csproj   # exit 0, "Build succeeded", 0 warnings
godot --headless --path game --import      # exit 0
godot --path game res://scenes/Run.tscn    # the game window
```

Verified exit codes for that run: the build printed `Build succeeded` and exited 0; the import exited 0; the launch stayed up until killed and printed `MechaMiner: run scene ready`, and the same command with `--quit-after 120` exited 0. Two of these three commands are confirmed on a Mac as well — `dotnet build game/MechaMiner.Game.csproj` and the `--path game res://scenes/Run.tscn` launch, on Apple Silicon under macOS 26.6, because they are exactly what step 4 of the macOS section runs, with Godot invoked there by its full bundle path rather than as a bare `godot`. The middle command, the import, is the one line no Mac has established.

What you give up is `doctor`, so check the two pins by hand: `dotnet --version` must print exactly `10.0.302` and `godot --version` must print `4.7.1.stable.mono.official.a13da4feb`. You also give up `test-fast` and the evidence bundles, so use this to look at the game, not to validate a change.

### macOS (Apple Silicon) — run on hardware

There are two macOS routes now, and they differ in exactly one way: one has been run on a Mac and the other has not.

**A macOS provisioning script exists — on `master`, and not on the branch this guide tells you to check out.** `build/bootstrap-macos.sh` merged to `master` on 11 Aug 2026. It pins every artifact it downloads by digest, and `build/verify-bootstrap-macos.sh` gates it statically on Linux — asserting the pins agree with `global.json`, and that the script keeps the properties its own header claims. It has **never been executed on macOS**; its header says so, and asks that an unexpected failure be treated as a defect in the script rather than in your machine. It is also **not in the tree you get by following step 4 below**: `claude/hearth-thread-3aamx2` carries no macOS or `osx` path at all, so a reader who runs that `git checkout` will find only `build/bootstrap-linux.sh`, which is Linux-only by construction, and the `bootstrap` verb on that branch still reports "there is no platform installer for osx-arm64 yet". Getting the script means getting it from `master` — a separate clone or checkout from the one this page walks you through. One thing to know before you do: it installs Godot to `~/Applications/Godot_mono.app`, creates a `~/.local/bin/godot` symlink, and prints guidance to put that directory on `PATH`. That is the arrangement the measured failure further down this section says breaks the mono build.

The steps below install the same pins the Linux script uses, by hand. They were carried out on Apple Silicon under macOS 26.6 and ended with the game running and the mech drivable from the keyboard; [What has and hasn't been verified](#what-has-and-hasnt-been-verified) records the one step in them that a Mac has not established. So: these steps have provisioned a Mac and are written from that run; the script is hash-pinned, gated and merged, and its first run will be someone's first run. Neither statement makes one of them the right choice — pick on which of those two things you would rather rely on.

**1. Install the GNU tools the gate scripts assume.**

```bash
brew install bash coreutils gnu-sed
export PATH="$(brew --prefix)/bin:$(brew --prefix)/opt/gnu-sed/libexec/gnubin:$(brew --prefix)/opt/coreutils/libexec/gnubin:$PATH"
```

**These are needed only for the `./build.sh` verbs, and `build.sh` and the gate scripts are on `claude/hearth-thread-3aamx2` — none of them are on `master`. Playing the game does not require any of them** — if you only want to drive the mech, skip this step; nothing in steps 2 to 4 invokes a gate script. Put the export in your shell profile if you want it to survive a new terminal.

The gate scripts on that branch use `mapfile` (bash 4+) in 32 places (on `master`, which carries none of them, the count is 0), and stock macOS ships bash 3.2.57. They also invoke `bash`, `sed`, `sha256sum` and `readlink` by unprefixed name from `PATH`, so Homebrew's `bin` and the relevant `libexec/gnubin` directories have to precede the system ones: otherwise `sed` and `readlink` resolve to the BSD builds, whose flags differ from the ones these scripts pass, and `sha256sum` and `timeout` do not resolve at all — macOS ships neither. The export writes `$(brew --prefix)/opt/<formula>` rather than `brew --prefix <formula>` deliberately: the latter errors when the formula is absent, and an erroring command substitution in a shell profile breaks the whole login rather than the one path it was meant to add. The command host invokes `bash` by name from `PATH` rather than through each script's shebang, so a Homebrew bash ahead of `/bin` is enough; no file in the repo needs editing.

**2. Install .NET SDK exactly 10.0.302.**

- Apple Silicon: <https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-osx-arm64.pkg>
- Intel: <https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-osx-x64.pkg>

Do not use `brew install dotnet` and do not accept a later 10.0.3xx patch. `./build.sh doctor` — again, on `claude/hearth-thread-3aamx2`, not on `master` — compares the SDK against the pin by exact patch, so a same-band newer patch passes the launcher's own probe and then fails `doctor` with exit class 3 — and the remedy it prints on macOS is a Linux script. The `.pkg` installs to `/usr/local/share/dotnet`, which is where you want it. If Godot later cannot load the runtime, `export DOTNET_ROOT=/usr/local/share/dotnet`.

**3. Install Godot 4.7.1-stable mono.** One universal binary covers both architectures. Note the separator in the filename is a dot, not an underscore.

```bash
curl -LO https://github.com/godotengine/godot-builds/releases/download/4.7.1-stable/Godot_v4.7.1-stable_mono_macos.universal.zip
unzip Godot_v4.7.1-stable_mono_macos.universal.zip -d /Applications
xattr -dr com.apple.quarantine /Applications/Godot_mono.app   # only if Gatekeeper objects
/Applications/Godot_mono.app/Contents/MacOS/Godot --version   # expect 4.7.1.stable.mono.official.a13da4feb
```

The bundle is named `Godot_mono.app`, not `Godot.app`. There is a pinned macOS digest for this exact archive, but not in the tree you are standing in: it is in `build/bootstrap-macos.sh` on `master`, as `GODOT_ARCHIVE_SHA256`. That script carries four macOS digests in all — `GODOT_ARCHIVE_SHA256` for this universal zip, `GODOT_EXECUTABLE_SHA256` for the `Contents/MacOS/Godot` extracted from it, and `DOTNET_SHA512_ARM64` and `DOTNET_SHA512_X64` for the two SDK downloads — and its provenance note records each one as downloaded-and-hashed on Linux and cross-checked against a hash the vendor publishes. Comparing `shasum -a 256` of your download against `GODOT_ARCHIVE_SHA256` is therefore a check you can run by hand; like everything else in that script, no Mac has run it. The two .NET constants do **not** cover step 2 above: the script installs the SDK from the `dotnet-sdk-10.0.302-osx-<rid>.tar.gz` tarball, and step 2 downloads the `.pkg`, which is a different artifact with a different hash.

`build/toolchain.json`, the machine-readable pin file, holds none of that: it records SHA-256 for `linux-x64` only, and its own `unpinned_platform_policy` states the macOS hash is unrecorded and that recording it is FND-002 follow-on work. It also does not exist on `master` at all — it lives only on the playable branch — so the two records have never been in the same tree. That split is known rather than overlooked: section 10 of `build/verify-bootstrap-macos.sh` reports SKIP while the pin file is absent and turns into a hard assertion that the two agree the moment they meet. What made the reconciliation non-trivial to begin with is that the sums published with the 4.7.1-stable release, read over the network on 6 Aug 2026, were SHA512 while `toolchain.json` records SHA256; whether the release server offers a SHA256 elsewhere was not established.

**Always invoke Godot by its full bundle path. Do not put it on `PATH` — not by symlink, not by wrapper.** The long path is what this page documents on purpose, because both shortcuts have been tried on a Mac and both broke:

- `sudo ln -s /Applications/Godot_mono.app/Contents/MacOS/Godot /usr/local/bin/godot` breaks the mono build. Godot locates its `GodotSharp` assemblies relative to the path it was invoked by, so invoked through the symlink it searches next to that name in `/usr/local/bin` and dies with, verbatim:

  ```
  Unable to find .NET assemblies directories, Make sure /usr/local/bin/GodotSharp/Api/Debug exists and contains the .NET assemblies
  ```

- A shell wrapper is worse, because writing one to the wrong path is unrecoverable in place. A wrapper written over `/Applications/Godot_mono.app/Contents/MacOS/Godot` itself replaced Godot's own 348 MB binary with a 70-byte script that `exec`'d itself in an infinite loop. It hangs with no `godot` process visible anywhere, because the process is `sh`. Recovery meant re-extracting the app from the zip.

Why the Linux section's `/usr/local/bin/godot` symlink is fine and this one is not — the coherent reading of the failure above, not something measured — is that the macOS build derives its assembly search root from the path it was invoked by, where the Linux build resolves the link through to its target first.

One consequence, read from the scripts rather than run on the Mac: `build/verify-godot.sh` calls bare `godot`, and `doctor`'s Godot probes resolve `godot` from `PATH` unless `MECHAMINER_GODOT` names the executable. With no `godot` on `PATH` — which on macOS you must not create — `./build.sh godot-import` has nothing to find. Step 4 therefore drives the two pinned tools directly, which is the route that was actually run.

**4. Clone the playable branch and run it.** There is no bootstrap step here; steps 2 and 3 already installed both pinned tools.

```bash
git clone https://github.com/pwestling/mecha-miner-survivor.git
cd mecha-miner-survivor
git checkout claude/hearth-thread-3aamx2   # if this fails, read the note below before going further
dotnet build game/MechaMiner.Game.csproj
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path game --import
/Applications/Godot_mono.app/Contents/MacOS/Godot --path game res://scenes/Run.tscn
```

If that checkout fails, the branch has been deleted or renamed; do not simply continue on the default branch, because `master` has no `game/scenes/Run.tscn` and the last line above will fail on it. `git fetch origin` and try again, or `git branch -r | grep hearth-thread` to find where the playable scene lives now. If the work has genuinely landed on `master`, `game/scenes/Run.tscn` is there — verify that before continuing, and note that dropping the scene argument to get past a missing-scene error only gets you `Boot.tscn`, which renders nothing.

WASD or the arrow keys drive the mech at 3 m/s. There is no HUD, no pause and no quit button — close the window.

The import line is needed only the first time and is a no-op once `game/.godot` is warm. Run it explicitly anyway: it separates "importing, which takes a while" from "stuck behind a dialog you cannot see", which on macOS are otherwise indistinguishable. Which brings us to the thing to know before anything goes wrong.

**On macOS, a silent hang is this program's normal way of reporting a mistake.** Godot reports errors through a modal dialog even under `--headless`, and a process launched from a terminal never brings its alert to the front. So *every* Godot error on a Mac — a .NET runtime it cannot find, a mistyped scene path — presents identically: the command sits there at 0% CPU, printing nothing, with no error text anywhere. Before you conclude it is wedged, look in the Dock and in Mission Control for a Godot window waiting on a click, or re-run the same command headed so the message has somewhere to go. To settle it from the terminal, `sample $(pgrep -i godot) 5` — a stack containing `-[NSAlert runModal]` is the confirmation. Dismissing the alert can then produce a null-dereference crash inside `-[NSApplication _postDidFinishNotification]`; that is the same root cause one step further along, not a second fault to chase.

**The `verify-gate-wiring` gate.** It is `build/verify-gate-wiring.sh`, run as `bash build/verify-gate-wiring.sh` from the checkout root, and it is on the feature branches — `claude/hearth-thread-3aamx2` among them — and not on `master`. A real Mac has reached and passed that gate's real checks — sections 1 to 4, the ones that analyse the checkout it is run from — with zero findings about the repository. It still reports FAIL, and the FAIL is section 5: the negative controls the script runs against copies of the tree to prove its own checks can go red, one of which rewrites a call site with `sed -i` and, under BSD sed, dies with `sed: 1: ... invalid command code` instead. That is the script's self-test failing, not a finding about this repository — the defect is `sed -i` portability; the FAIL is the gate being honest.

If you would rather not install the GNU tools, skip step 1 — step 4 above already avoids `./build.sh` entirely, and the Linux write-up of the same route is at [Running the pinned tools directly](#running-the-pinned-tools-directly--verified-on-linux).

### Windows — not supported

`build.ps1` exists as a launcher counterpart to `build.sh` — both on `claude/hearth-thread-3aamx2` only, neither on `master` — but it has never been executed on any host and there is no Windows provisioning script.

## What has and hasn't been verified

Run on Linux x86-64 from a fresh HTTPS clone on 6 Aug 2026, at `master` `3016fbc` and on the playable branch at `dccc9588` and `5f9e28cc`. `5f9e28cc` is the merge commit of #19 and the tip of `claude/hearth-thread-3aamx2`; `dccc9588` is its second parent, the sha the measurements were taken at. Every file this guide touches — `build/bootstrap-linux.sh`, `build.sh`, `game/project.godot`, `game/scenes/Run.tscn`, and the whole of `game/` and `src/MechaMiner.Tools/` — is the same blob or tree object at both commits; in fact the two commits share one root tree (`fbe1966`), so a checkout of either gives a byte-identical working tree and the results below hold at both.

- ✅ `sudo build/bootstrap-linux.sh` — exit 0, reporting .NET SDK 10.0.302 and Godot 4.7.1.stable.mono.official.a13da4feb present and both pins re-verified. This file is byte-identical to the copy verified before the merge (blob `f091537`), and it is *not* the same file as `build/bootstrap-linux.sh` on `master` (blob `ee6b470`), so run it after the checkout, not before. The 63 s figure for a from-scratch install was measured in a Linux container against that same byte-identical script, not on a workstation, and says nothing about how long it takes on your hardware or link.
- ✅ `./build.sh doctor` — exit 0, 10 probes, 0 mismatches
- ✅ `./build.sh build` — exit 0, 0 warnings; `./build.sh godot-import` — exit 0
- ✅ `./build.sh test-fast` — exit 0, total 260, passed 260, failed 0, skipped 0 (252 in `MechaMiner.Simulation.Tests`, 5 in `MechaMiner.Content.Tests`, 3 in `MechaMiner.Persistence.Tests`). The mech stopping at the arena wall is asserted here, not by the harnesses below: `MovementCommandPathTests.TheBodyCannotLeaveTheGrayboxArena` drives east for 20 seconds and requires the body to come to rest at 19.5 m, its collision circle tangent to the wall of the 40 m square graybox arena.
- ✅ `godot --path game res://scenes/Run.tscn` — launched and stayed up under Xvfb (killed by a 25 s timeout, exit 124), rendering on `Vulkan 1.4.318 - Forward Mobile` with no scene, script or render errors, and exited 0 after `--quit-after 120` frames.
- ✅ `MECHAMINER_RUN_SLICE_OUTPUT=<dir> godot --headless --path game res://tests/RunSliceEvidenceHarness.tscn` — exit 0, **28 assertions run, 0 failed**, writing a `transcript.tsv` that records 60 ticks per second, a 3.0 m/s base speed, 0.05 m of displacement per tick, 1.5 m travelled per 30-tick leg in each of the four cardinals, facing turning to match each leg, an exact stop on release, and an orthographic camera with `KeepAspect` `Height` and a 24 m vertical extent whose up vector is world −Z.
- ✅ `MECHAMINER_RUN_SLICE_CAPTURE=<dir> xvfb-run -a godot --path game res://tests/RunSliceCaptureHarness.tscn` — exit 0, writing five PNGs and a `captures.tsv`. It instantiates `res://scenes/Run.tscn` as shipped; the screenshots show the mech rendered, displaced and rotated against the arena floor. Its one gate assertion passed: all four movement actions carry bound events (3 each) and all eight physical keys resolve to the action they drive. That check is now an exit code — the harness exits 4 and captures nothing if it fails — where an earlier revision only logged it.

Neither harness is wired to a `./build.sh` verb; both are run by hand, so neither can turn a gate red.

Not verified, and not claimed:

- ⚠️ **A real keyboard, on the Linux evidence only.** Every ✅ above was produced in a container, and a container cannot press keys: the input map — WASD, arrows and stick, bound by physical keycode — and the movement path driven from action state are verified there, but no key was pressed in any of those runs. This caveat does **not** extend to macOS, where a human drove the mech with a keyboard in a real window; it stands unchanged for the Linux and container evidence, which never pressed one.
- ⚠️ **A real GPU, on the Linux evidence only.** Every ✅ above rendered through `llvmpipe`, a software Vulkan implementation. Audio fell back to the dummy driver, and every one of those launches was under Xvfb rather than a window on a real desktop. The macOS run described above was headed on Apple Silicon hardware; it is not covered by this caveat, and it produced none of the ✅ measurements here.
- ⚠️ Exports and packaging. The `export`, `package-demo`, `release-validate` and `run` verbs are unimplemented, and the 1.2 GB Godot export templates are not fetched.
- ⚠️ **Any Linux that is not Debian or Ubuntu.** The block opens with `apt-get`, and `build/bootstrap-linux.sh` installs `mesa-vulkan-drivers` by that name — on a non-apt distro it logs `WARNING: no apt-get` and skips the Vulkan driver rather than failing. Install `git`, `curl`, `unzip` and a Vulkan ICD with your own package manager; the rest of the block is unchanged.
- ⚠️ **One step of the macOS path, and every Windows instruction on this page.** macOS is confirmed as far as the game running with the mech drivable — on Apple Silicon, macOS 26.6, launched from the full bundle path. What is *not* established on Mac hardware is whether a fresh clone needs the explicit `--headless --path game --import` before its first launch: the single report that it was unnecessary came from an already-warm `game/.godot`, which proves nothing about a cold one. Windows has never been run at all.

## Things that will trip you up

- `./build.sh run` reads like the launch command and is not implemented — on `claude/hearth-thread-3aamx2` it exits 2 and names FND-006 as its owner. On `master` there is no `build.sh` at all, so the shell answers 127, command not found, which is the same problem wearing a different symptom. Use the `--path game res://scenes/Run.tscn` form instead — as bare `godot` on Linux, and as `/Applications/Godot_mono.app/Contents/MacOS/Godot` on macOS, where you must not put Godot on `PATH` at all.
- If `./build.sh` is missing after a clone, you are on `master`, which does not have it yet. Check out the branch above.
- If Godot cannot load `res://scenes/Run.tscn`, that is the same problem, not a broken game: `game/scenes/Run.tscn` is on `claude/hearth-thread-3aamx2` and is not on `master` either. Check out the branch; do not drop the scene argument to get past it, for the reason in the next bullet.
- `godot --path game` with no scene argument runs `Boot.tscn`, which prints one line and renders nothing. The scene has to be passed explicitly; `project.godot`'s main scene is deliberately still `Boot.tscn`.
- No automated check launches `Run.tscn`. `./build.sh godot-import` and `build/verify-godot.sh` exercise `Boot.tscn` only, and no verb invokes either run-slice harness in `game/tests/`, so a break in the playable scene would not turn a gate red.

## Documentation

| Path | What it is |
| --- | --- |
| `docs/README.md` | Gameplay specification index — player-visible behaviour |
| `docs/technical/README.md` | Technical design specification |
| `docs/technical/110-implementation-plan-for-ai-agents.md` | Milestones, work packages, task queue |
| `docs/technical/100-*.md` | Toolchain pins and the standard command surface |
| `AGENTS.md` | The rules every contributor works under, human or agent |

## Keeping this page honest

The status block and the setup steps are re-verified by re-running them after major merges, not edited from memory. Last run: 6 Aug 2026. If a step here fails for you, treat that as a bug in this page and say so — these commands are meant to have been executed, not just written.

# Mecha Miner Survivor

A survivor-like action game in which science-fiction mechs fight alien monsters. It removes experience points, replacing experience-gem and treasure-chest progression with map exploration, resource mining, positional commitment, and upgrade crafting.

Godot 4.7.1 (.NET / C#), authoritative 60 Hz simulation, Windows and Steam Deck targets.

## Where the project is

Measured against `master` at `3016fbc`, 6 Aug 2026. This block is refreshed after major merges rather than continuously; `git log --oneline 3016fbc..origin/master` shows exactly what has landed since, which is exactly what this block has not accounted for.

**Playable today: a mech you can drive around an empty test arena.** No enemies, no weapons, no mining, no HUD. What it does do, verified by execution at the sha below: the mech moves at 3.0 m/s under the authoritative 60 Hz simulation, turns to face the direction it is travelling, stops with its collision circle tangent to the wall of a 40 m square arena, and is shown by a non-rotating orthographic north-up camera whose vertical extent is 24 gameplay metres.

The slice has merged — but into another branch, not into `master`. See [Try it out](#try-it-out) for the branch to check out.

| | Area | State |
| --- | --- | --- |
| ✅ | Design + technical specification | Complete, on `master` (`docs/`) |
| ✅ | Content catalog | 138 JSON definitions on `master` — 15 weapons, 45 branches, 6 mechs, 10 enemies, 4 bosses, 10 relics, 13 utilities, 13 power-ups (116), and 22 more: 8 resources, 6 unlocks, 4 mining sites, 1 map contract, 1 encounter schedule, and the shared weapon-price and elite-modifier rule files. This is the number the content gate asserts — `python3 src/MechaMiner.Tools/ContentImport/verify_content.py` reports 138 definition `*.json` files under `content/`, excluding `localization/` and `schemas/`, which is why the tree also holds a 139th JSON file (`content/localization/en.json`) that is not a definition. |
| 🚧 | Build + toolchain | Works on a branch. `master` carries the provisioning script but **no `./build.sh`** and no CI. |
| 🚧 | Simulation core | Written, 252 tests passing in `MechaMiner.Simulation.Tests`, not on `master` — [#11](https://github.com/pwestling/mecha-miner-survivor/pull/11) is open and in draft, with head `claude/hearth-thread-3aamx2` and base `claude/hearth-thread-2vmaro-fnd-002` |
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

The playable slice ([#19](https://github.com/pwestling/mecha-miner-survivor/pull/19)) has already merged into `claude/hearth-thread-3aamx2`, so it rides that branch now instead of one of its own; that moved it one link along and did not put it on `master`. Both remaining pull requests are still drafts. The `./build.sh` command surface and CI both live on `claude/hearth-thread-2vmaro-fnd-002` and arrive on `master` in the same reconcile, which is why `master` currently has neither — it holds the specification, the content catalog, and an empty Godot shell. The last hop is not pending work: `claude/hearth-thread-2vmaro` already merged to `master` once ([#1](https://github.com/pwestling/mecha-miner-survivor/pull/1)) and is an ancestor of it today — 0 commits ahead of `master`, however far `master` has since moved past it — so it needs a fresh pull request only once #3 lands on it.

## Try it out

**Linux x86-64 is the only path that has actually been run.** The steps below were executed from a fresh clone on 6 Aug 2026 and the game launched; see [What has and hasn't been verified](#what-has-and-hasnt-been-verified). The macOS steps are derived from the same version pins and have **never been run on a Mac**.

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

If that checkout fails with `did not match any file(s) known to git`, the branch is gone because the work landed — stay on the default branch and carry on with the next block. `claude/hearth-thread-3aamx2` exists for exactly as long as the merge chain above is unfinished, and disappears when it completes. To pin the precise tree the verification below describes, `git checkout 5f9e28cc` instead — or `git checkout dccc9588`, the sha the measurements were taken at, whose tree is identical to `5f9e28cc`'s. A sha never disappears, but it leaves you on a detached HEAD. (The earlier branch this page named, `claude/ui-002-first-playable`, is the merged side of #19; it is no longer the branch to check out and may be deleted at any time.)

```bash
sudo build/bootstrap-linux.sh   # once per machine, about a minute
./build.sh doctor               # expect exit 0: "pinned toolchain verified; 10 probes, 0 mismatches"
./build.sh build
./build.sh godot-import

godot --path game res://scenes/Run.tscn
```

WASD or the arrow keys drive the mech at 3 m/s; a gamepad left stick works too. The camera is orthographic and north-up. There is no HUD, no pause and no quit button — close the window.

`build/bootstrap-linux.sh` installs the .NET SDK to `/usr/share/dotnet`, Godot to `/opt/godot` with a `/usr/local/bin/godot` symlink, and the `mesa-vulkan-drivers` package. It must run as root and does not call `sudo` itself, so invoke it with `sudo`. `/usr/share/dotnet` is not a free choice: it is hostfxr's default probe path, and Godot's .NET host finds the runtime there without `DOTNET_ROOT`. Re-running the script is safe — it revalidates and skips what is already correct. You do not need to re-run it per clone.

On a machine with no display, also `sudo apt-get install -y xvfb` (the bootstrap script does not install it) and prefix the launch with `xvfb-run -a`.

### Running the pinned tools directly — verified on Linux

You can skip `./build.sh` entirely and drive the two pinned tools yourself. This is the only route on this page that invokes no gate script, which is what makes it the way out of the GNU-userland requirement the macOS section below opens with. This three-command sequence was executed on Linux from a fresh clone at `5f9e28cc` with **no `./build.sh` verb invoked at all**, and reached a running scene:

```bash
dotnet build game/MechaMiner.Game.csproj   # exit 0, "Build succeeded", 0 warnings
godot --headless --path game --import      # exit 0
godot --path game res://scenes/Run.tscn    # the game window
```

Verified exit codes for that run: the build printed `Build succeeded` and exited 0; the import exited 0; the launch stayed up until killed and printed `MechaMiner: run scene ready`, and the same command with `--quit-after 120` exited 0. Only the Linux behaviour of these three commands is verified; running them on a Mac is not.

What you give up is `doctor`, so check the two pins by hand: `dotnet --version` must print exactly `10.0.302` and `godot --version` must print `4.7.1.stable.mono.official.a13da4feb`. You also give up `test-fast` and the evidence bundles, so use this to look at the game, not to validate a change.

### macOS — not run on hardware

There is no macOS provisioning script in this repository. `build/bootstrap-linux.sh` is Linux-only by construction, and the `bootstrap` verb reports "there is no platform installer for osx-arm64 yet". The steps below follow the same pins the Linux script uses; every one of them is unverified on a Mac.

**1. Replace the BSD userland tools the gate scripts assume.** This is required, not optional: the gate scripts use `mapfile` (bash 4+) in 32 places and GNU `sha256sum` and `timeout`, and stock macOS ships bash 3.2.57 and neither command.

```bash
brew install bash coreutils
echo 'export PATH="$(brew --prefix)/bin:$(brew --prefix coreutils)/libexec/gnubin:$PATH"' >> ~/.zshrc
exec zsh
bash --version   # expect 5.x, not 3.2.57
```

The command host invokes `bash` by name from `PATH` rather than through each script's shebang, so a Homebrew bash ahead of `/bin` is enough; no file in the repo needs editing.

**2. Install .NET SDK exactly 10.0.302.**

- Apple Silicon: <https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-osx-arm64.pkg>
- Intel: <https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-osx-x64.pkg>

Do not use `brew install dotnet` and do not accept a later 10.0.3xx patch. `./build.sh doctor` compares the SDK against the pin by exact patch, so a same-band newer patch passes the launcher's own probe and then fails `doctor` with exit class 3 — and the remedy it prints on macOS is a Linux script. The `.pkg` installs to `/usr/local/share/dotnet`, which is where you want it. If Godot later cannot load the runtime, `export DOTNET_ROOT=/usr/local/share/dotnet`.

**3. Install Godot 4.7.1-stable mono.** One universal binary covers both architectures. Note the separator in the filename is a dot, not an underscore.

```bash
curl -LO https://github.com/godotengine/godot-builds/releases/download/4.7.1-stable/Godot_v4.7.1-stable_mono_macos.universal.zip
unzip Godot_v4.7.1-stable_mono_macos.universal.zip -d /Applications
xattr -dr com.apple.quarantine /Applications/Godot_mono.app   # only if Gatekeeper objects
sudo ln -s /Applications/Godot_mono.app/Contents/MacOS/Godot /usr/local/bin/godot
godot --version   # expect 4.7.1.stable.mono.official.a13da4feb
```

The bundle is named `Godot_mono.app`, not `Godot.app`. There is nothing to check this download against: the sums published with the 4.7.1-stable release, read over the network on 6 Aug 2026, were SHA512, while `build/toolchain.json` records SHA256 and only for `linux-x64`. Whether the release server offers a SHA256 elsewhere was not established, and no macOS hash is pinned in this repository at all.

**4. Clone the playable branch and run the verbs.** There is no bootstrap step here; steps 2 and 3 already installed both pinned tools.

```bash
git clone https://github.com/pwestling/mecha-miner-survivor.git
cd mecha-miner-survivor
git checkout claude/hearth-thread-3aamx2   # if this fails, the work landed; stay on the default branch
./build.sh doctor
./build.sh build
./build.sh godot-import
godot --path game res://scenes/Run.tscn
```

`doctor` should still exit 0. It will report the Godot pin as a **warning** rather than a mismatch, because `build/toolchain.json` has no macOS entry; that is deliberate and non-blocking.

If you would rather not install GNU bash and coreutils, skip step 1 and use [Running the pinned tools directly](#running-the-pinned-tools-directly--verified-on-linux) above instead of `./build.sh` — that path invokes no gate script, so it needs neither `mapfile` nor `sha256sum` nor `timeout`; its exit codes are verified on Linux only, and running it on a Mac is as unverified as everything else here.

### Windows — not supported

`build.ps1` exists as a launcher counterpart to `build.sh`, but it has never been executed on any host and there is no Windows provisioning script.

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

- ⚠️ **A real keyboard.** A container cannot press keys. The input map — WASD, arrows and stick, bound by physical keycode — and the movement path driven from action state are both verified; that pressing a physical W moves the mech is not.
- ⚠️ A real GPU. Everything above rendered through `llvmpipe`, a software Vulkan implementation. Audio fell back to the dummy driver, and every launch was under Xvfb rather than a window on a real desktop.
- ⚠️ Exports and packaging. The `export`, `package-demo`, `release-validate` and `run` verbs are unimplemented, and the 1.2 GB Godot export templates are not fetched.
- ⚠️ **Any Linux that is not Debian or Ubuntu.** The block opens with `apt-get`, and `build/bootstrap-linux.sh` installs `mesa-vulkan-drivers` by that name — on a non-apt distro it logs `WARNING: no apt-get` and skips the Vulkan driver rather than failing. Install `git`, `curl`, `unzip` and a Vulkan ICD with your own package manager; the rest of the block is unchanged.
- ⚠️ **Every macOS and Windows instruction on this page.**

## Things that will trip you up

- `./build.sh run` reads like the launch command and is not implemented — it exits 2 and names FND-006 as its owner. Use the `godot --path game res://scenes/Run.tscn` form.
- If `./build.sh` is missing after a clone, you are on `master`, which does not have it yet. Check out the branch above.
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

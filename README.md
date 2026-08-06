# Mecha Miner Survivor

A survivor-like action game in which science-fiction mechs fight alien monsters. It removes experience points, replacing experience-gem and treasure-chest progression with map exploration, resource mining, positional commitment, and upgrade crafting.

Godot 4.7.1 (.NET / C#), authoritative 60 Hz simulation, Windows and Steam Deck targets.

## Where the project is

Measured against `master` at `3016fbc`, 6 Aug 2026. This block is refreshed after major merges rather than continuously; `git log --oneline 3016fbc..origin/master` shows exactly what has landed since, which is exactly what this block has not accounted for.

**Playable today: a mech you can drive around an empty test arena.** No enemies, no weapons, no mining, no HUD. It is also not on `master` yet — see [Try it out](#try-it-out) for the branch to check out.

| | Area | State |
| --- | --- | --- |
| ✅ | Design + technical specification | Complete, on `master` (`docs/`) |
| ✅ | Content catalog | 139 JSON definitions on `master` — 15 weapons, 45 branches, 6 mechs, 10 enemies, 4 bosses, 10 relics, 13 utilities, 13 power-ups |
| 🚧 | Build + toolchain | Works on a branch. `master` carries the provisioning script but **no `./build.sh`** and no CI. |
| 🚧 | Simulation core | Written, 157 tests passing, not merged — [#11](https://github.com/pwestling/mecha-miner-survivor/pull/11) |
| 🚧 | Playable slice | Movement only, not merged — [#19](https://github.com/pwestling/mecha-miner-survivor/pull/19) |
| ⬜ | Everything else | Not started |

### Milestones

Full definitions: `docs/technical/110-implementation-plan-for-ai-agents.md` § Milestone gates.

| | | |
| --- | --- | --- |
| **M0** | Reproducible foundation — a clean checkout builds, tests, and imports the Godot project | 🚧 in progress |
| **M1** | Headless simulation skeleton — clocks, entities, commands, events, RNG, snapshots | 🚧 built, not merged |
| **M2** | Combat graybox — one mech, pursuing enemies, one weapon, hull/HUD, 60 FPS | 🚧 movement only |
| **M3** | Core differentiator slice — mining seams, resources, fabrication, radar, map | ⬜ |
| **M4** | Internal gameplay demo — a 14-minute scenario, two mechs, four weapons, eight enemies, two bosses | ⬜ |
| **M5** | Full standard run — all 15 weapons and 45 branches, six mechs, the 35-minute schedule | ⬜ |
| **M6** | Content and performance production readiness | ⬜ |
| **M7** | Release candidate — exports, Steam staging, release checklist | ⬜ |

Everything executable is stacked in a chain of unmerged branches, each one based on the next rather than on `master`:

`claude/ui-002-first-playable` ([#19](https://github.com/pwestling/mecha-miner-survivor/pull/19)) → `claude/hearth-thread-3aamx2` ([#11](https://github.com/pwestling/mecha-miner-survivor/pull/11)) → `claude/hearth-thread-2vmaro-fnd-002` ([#3](https://github.com/pwestling/mecha-miner-survivor/pull/3)) → `claude/hearth-thread-2vmaro` → `master`

So #19 merging does not put the playable scene on `master`; it moves it one link along. The `./build.sh` command surface and CI both live on `claude/hearth-thread-2vmaro-fnd-002` and arrive on `master` in the same reconcile, which is why `master` currently has neither — it holds the specification, the content catalog, and an empty Godot shell. The last hop, `claude/hearth-thread-2vmaro` → `master`, has no open pull request yet.

## Try it out

**Linux x86-64 is the only path that has actually been run.** The steps below were executed from a fresh clone on 6 Aug 2026 and the game launched; see [What has and hasn't been verified](#what-has-and-hasnt-been-verified). The macOS steps are derived from the same version pins and have **never been run on a Mac**.

You will need `git`, `curl` and `unzip`, and then two pinned tools:

- **.NET SDK 10.0.302** — exactly this patch, not "latest .NET 10"
- **Godot 4.7.1-stable, mono flavour** — the .NET build; the standard build cannot run this project

### Linux (x86-64) — verified

```bash
sudo apt-get update && sudo apt-get install -y git curl unzip
git clone https://github.com/pwestling/mecha-miner-survivor.git
cd mecha-miner-survivor
git checkout claude/ui-002-first-playable   # exists only until the scene reaches master
```

If that checkout fails with `did not match any file(s) known to git`, the branch is gone because the work landed — stay on the default branch and carry on with the next block. This branch exists for exactly as long as the merge chain above is unfinished, and disappears when it completes. To pin the precise tree the verification below describes, `git checkout dccc9588` instead — a sha never disappears, but it leaves you on a detached HEAD.

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

The bundle is named `Godot_mono.app`, not `Godot.app`. Godot publishes SHA512 sums for this release and no SHA256, so there is nothing to compare against `build/toolchain.json`, which records Linux hashes only.

**4. Clone the playable branch and run the verbs.** There is no bootstrap step here; steps 2 and 3 already installed both pinned tools.

```bash
git clone https://github.com/pwestling/mecha-miner-survivor.git
cd mecha-miner-survivor
git checkout claude/ui-002-first-playable   # if this fails, the work landed; stay on the default branch
./build.sh doctor
./build.sh build
./build.sh godot-import
godot --path game res://scenes/Run.tscn
```

`doctor` should still exit 0. It will report the Godot pin as a **warning** rather than a mismatch, because `build/toolchain.json` has no macOS entry; that is deliberate and non-blocking.

#### If you would rather not install GNU bash and coreutils

Step 1 exists only to satisfy the gate scripts. You can skip it and skip `./build.sh` entirely, driving the two pinned tools yourself. This three-command sequence was executed on Linux from a fresh clone with **no `./build.sh` verb invoked at all**, and reached a running scene:

```bash
dotnet build game/MechaMiner.Game.csproj   # exit 0, "Build succeeded", 0 warnings
godot --headless --path game --import      # exit 0
godot --path game res://scenes/Run.tscn    # the game window
```

Verified exit codes for that run: the build printed `Build succeeded` and exited 0; the import exited 0; the launch stayed up until killed and printed `MechaMiner: run scene ready`, and the same command with `--quit-after 120` exited 0. Only the Linux behaviour of these three commands is verified — like everything else in this section, running them on a Mac is not.

What you give up is `doctor`, so check the two pins by hand: `dotnet --version` must print exactly `10.0.302` and `godot --version` must print `4.7.1.stable.mono.official.a13da4feb`. You also give up `test-fast` and the evidence bundles, so use this to look at the game, not to validate a change.

### Windows — not supported

`build.ps1` exists as a launcher counterpart to `build.sh`, but it has never been executed on any host and there is no Windows provisioning script.

## What has and hasn't been verified

Run on Linux x86-64 from a fresh HTTPS clone on 6 Aug 2026, at `master` `3016fbc` and `claude/ui-002-first-playable` `dccc9588`:

- ✅ `build/bootstrap-linux.sh` — exit 0 in 63 s, installing .NET SDK 10.0.302 and Godot 4.7.1.stable.mono.official.a13da4feb. Run once, at the earlier tip `91714b0`; the script is byte-identical at both commits, and `doctor` re-confirmed the toolchain it produced. Note it is *not* the same file as `build/bootstrap-linux.sh` on `master`, so run it after the checkout, not before.
- ✅ `./build.sh doctor` — exit 0, 10 probes, 0 mismatches
- ✅ `./build.sh build` — exit 0; `./build.sh test-fast` — exit 0, 260 of 260 tests passing; `./build.sh godot-import` — exit 0
- ✅ `godot --path game res://scenes/Run.tscn` — launched and stayed up under Xvfb, rendering on Vulkan Forward Mobile with no scene, script or render errors, and exited 0 after `--quit-after 120` frames. The capture harness in `game/tests/` produced screenshots showing the mech rendered, displaced and rotated, and asserted that all four movement actions carry bound events and all eight physical keys resolve to the action they drive.

Not verified, and not claimed:

- ⚠️ **A real keyboard.** A container cannot press keys. The input map — WASD, arrows and stick, bound by physical keycode — and the movement path driven from action state are both verified; that pressing a physical W moves the mech is not.
- ⚠️ A real GPU (software Vulkan only), audio (dummy driver), and a window on a real desktop rather than Xvfb.
- ⚠️ Exports and packaging. The `export`, `package-demo`, `release-validate` and `run` verbs are unimplemented, and the 1.2 GB Godot export templates are not fetched.
- ⚠️ **Every macOS and Windows instruction on this page.**

## Things that will trip you up

- `./build.sh run` reads like the launch command and is not implemented — it exits 2 and names FND-006 as its owner. Use the `godot --path game res://scenes/Run.tscn` form.
- If `./build.sh` is missing after a clone, you are on `master`, which does not have it yet. Check out the branch above.
- `godot --path game` with no scene argument runs `Boot.tscn`, which prints one line and renders nothing. The scene has to be passed explicitly; `project.godot`'s main scene is deliberately still `Boot.tscn`.
- No automated check launches `Run.tscn`. `./build.sh godot-import` and `build/verify-godot.sh` exercise `Boot.tscn` only, so a break in the playable scene would not turn a gate red.

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

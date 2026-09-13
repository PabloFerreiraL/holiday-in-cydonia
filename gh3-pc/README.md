# Guitar Hero III / Windows PC

Download **GH3-HoldToHit.exe**. No Python installation is needed to run it.

Supported game executable:

- 32-bit **GH3.exe**, file version **1.0.6.57108**.
- SHA-256: `801f8a522651a479d63a9ff5fbde5be949ba0b7ad2ec0e5fb1d92ab1a8ef2154`.
- The helper itself requires 64-bit Windows with .NET Framework 4.x.

The helper checks both the executable hash and the live routines before patching. Other builds are refused. It does not configure controller mappings: use a setup where normal color presses already act as strums.

## Start and play

1. Start GH3, enter a song and pause for your first test.
2. Open `GH3-HoldToHit.exe` from any writable folder. If opened first, it waits for GH3.
3. The helper attaches and starts **OFF**.
4. Return to GH3, press **F6**, and resume. You can also use the Enable/Disable button.
5. Close the helper to turn it off and restore the original game instructions.

It supports player 1. Hold the exact matching buttons for automatic hits; release to stop them. Ordinary presses still receive native judgment. V3 adds one-time confirmation for a tap on the note the helper just scored, within the native timing window, to avoid an accidental extra-press penalty. It does not add another hit or score.

If access is denied when attaching to an elevated game, run the helper at the same permission level. If it is forcibly terminated, restart GH3 to clear the runtime patch.

## Open both from one shortcut

Create a shortcut to the helper and append `--launch-game` to its target:

```text
"C:\YourTools\GH3-HoldToHit.exe" --launch-game
```

This optional launcher expects GH3 at `C:\Program Files (x86)\Aspyr\Guitar Hero III\GH3.exe`. For a custom installation directory, launch GH3 manually and then open the helper. Manual attachment identifies the running executable independently of its folder.

The helper waits for the game's window, attaches and starts OFF. Keep it at the shortcut's target location.

## Build from source

Keep `HoldToHit.cs`, `Payload.cs`, `hold_hook.bin` and `Build.ps1` together, then run:

```powershell
.\Build.ps1
```

The script uses the Windows .NET Framework C# compiler and embeds `hold_hook.bin` into the EXE. `hold_hook.asm` and `hook_manifest.json` document the payload and its relocations.

`session.log` records attachment and toggle results. Do not commit personal session logs.

## Status

Experimental v3, confirmed working in gameplay by the original tester on their controller setup on September 13, 2026, following the tapping correction. It also passed 107 instruction-level scenarios, with scoring callbacks simulated. This does not establish compatibility with other executable builds or every controller. See [testing](../docs/TESTING.md).

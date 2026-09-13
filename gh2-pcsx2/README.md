# Guitar Hero II / PCSX2

Download `SLUS-21447_2A6C845B.pnach` from this folder.

**Supported:** USA retail GH2, serial **SLUS-21447**, CRC **2A6C845B**. Other regions/revisions require a different patch.

1. Place the PNACH in the **cheats folder configured in PCSX2**.
2. Open Guitar Hero II's game properties, then Cheats.
3. Enable **Hold matching frets to hit notes (experimental v3)**. Disable older copies.
4. Boot the game fresh. Do not load an old emulator save state for the first test. An ordinary memory-card save is fine.
5. Use normal DualShock 2/gamepad mode and your existing button bindings.

Press the first note and keep its buttons held through repeats. Hold the exact combination for chords. Release to stop scoring. Native new presses retain normal judgment, including empty-press penalties; starting a hold far before a note can therefore cause an initial penalty.

To remove it, uncheck the cheat and fully restart the game. Removing the PNACH and restarting also works. The ISO is unchanged.

The patch uses EE memory at `000F0000`; combining it with another patch using that area can conflict. Multiplayer and other controller modes need separate testing.

# Validation and release testing

## Evidence so far

GH2's core held-note behavior was confirmed by the original tester. Its latest patch restores native button-down judgment. The instruction harness passed 33 scenarios; a simulated replay also used chart data read from the tester's game. These are not universal compatibility results.

GH3 v1 successfully attached, toggled and restored its original hook in a live process. The tester subsequently confirmed v2's held singles and chords worked. A tap-after-auto overlap bug was reproduced against native judgment instructions and corrected in v3. The v3 suite has 107 passing scenarios, including both input modes, all two-color chords, release, partial chords, normal presses, one-time tap confirmation, wrong-color/out-of-window penalties, toggling, register preservation and relocation. Scoring callbacks were mocked.

On September 13, 2026, the original tester subsequently confirmed GH3 v3 was working in gameplay on their controller setup. That confirmation covers the latest build after the tapping correction; it is separate from the automated instruction tests above.

## Further regression testing

The overall GH3 v3 gameplay test is confirmed. The following detailed cases remain useful for broader testing; the tester has not individually reported each one.

- [ ] Verify GH3 v3 attachment, F6 toggling and clean restoration.
- [ ] Tap every note normally with the helper ON.
- [ ] Switch between tapping and holding within a song.
- [ ] Hold repeated singles and each two-color chord combination.
- [ ] Release all buttons, or only half a chord, and check misses.
- [ ] Test wrong colors and empty presses, including repeated extra presses.
- [ ] Test sustains, pause/resume, song restart and returning to menus.
- [ ] Test GH2 v3's restored empty-press penalty in gameplay.
- [ ] Record real gameplay footage, after confirming the tested behavior.

The GIF in the README is an original animated illustration of the intended behavior. It is not a gameplay capture and does not establish that any unchecked item passed.

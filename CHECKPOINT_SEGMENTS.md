# Checkpoint Segments Feature

## Overview

This feature extends the demo system to support checkpoint-based segmentation, allowing demos to:
- Start from specific checkpoints instead of only from level reload
- Reset to checkpoints during playback
- Store level and checkpoint metadata in demo files

## Implementation Details

### Changes to Demo Data Structure

#### DemoData.cs
- Added `LevelId` (string): Stores the scene name where the demo was recorded
- Added `CheckpointId` (int): Stores the checkpoint ID (-1 = level start, 0+ = specific checkpoint)
- Added `CheckpointResets` (List&lt;bool&gt;): Per-frame flags indicating when to reset to checkpoint

#### CSV Format Extensions

The CSV format now includes metadata rows before the column headers:

```csv
Level: LevelName
Checkpoint: 0
Move Horizontal,Move Vertical,Look Horizontal,Look Vertical,Jump,Grab,Rotate,Reset Checkpoint,Speed
0,1,180,0,0,0,0,0,
0,-1,180,0,0,0,0,0,
```

**New Elements:**
1. **Metadata rows** (optional, at the beginning):
   - `Level: <scene_name>` - The Unity scene name
   - `Checkpoint: <id>` - The checkpoint ID (0+), omit if starting from level start

2. **Reset Checkpoint column**: Boolean column (0/1) indicating frames where checkpoint reset should occur

#### SLT Format Extensions

The binary SLT format has been versioned:

**V1 Format (SUPERLIMINALTAS1):**
- Original format without metadata (backward compatible)

**V2 Format (SUPERLIMINALTAS2):**
```
Magic Header: "SUPERLIMINALTAS2" (16 bytes, ASCII)
Frame Count: Int32 (4 bytes)
Level ID Length: Int32 (4 bytes)
Level ID: UTF-8 string (variable bytes)
Checkpoint ID: Int32 (4 bytes)
Axes Data: float32 arrays
Buttons Data: bool arrays
Checkpoint Resets: bool array
```

The deserializer automatically detects and handles both formats.

### Checkpoint Management

#### Recording
When starting a recording (F7):
- Reloads the level to ensure consistent starting state
- Automatically captures current scene name as `LevelId`
- Captures checkpoint ID (will be -1 since level was just reloaded)
- Note: Currently, recording always starts from level beginning. To create a demo starting from a checkpoint, you would need to:
  1. Record a full run from level start
  2. Export to CSV
  3. Manually edit to add checkpoint metadata and trim frames before the checkpoint
  4. Or, load an existing demo file that starts from a checkpoint

#### Playback
When playing back a demo (F5):
- Reloads the level
- If demo has a checkpoint ID >= 0, teleports to that checkpoint using `TeleportToCheckpointIndexDebug()`
- Displays level and checkpoint info in debug log
- Warns if current level doesn't match demo level
- Checks for checkpoint reset flags each frame
- Triggers checkpoint reset when flag is set

#### Checkpoint Reset Process
1. Detects reset flag at current frame
2. Pauses playback and blocks input
3. Calls `SaveAndCheckpointManager.ResetToLastCheckpoint()` (runs synchronously)
4. Resumes playback from same frame (no frame number adjustment needed)

## Testing Checkpoint Consistency

### Test Procedure

To test if checkpoint resets provide consistent/deterministic results:

1. **Record a baseline demo:**
   - Load a level and reach a checkpoint
   - Press F7 to start recording from that checkpoint
   - Perform some actions
   - Press F6 to stop recording
   - Press F12 to save as `baseline.slt`

2. **Create a checkpoint-segmented demo:**
   - Open `baseline.slt` with F11
   - Export to CSV with F10 as `test.csv`
   - Edit `test.csv` in a spreadsheet program:
     - Insert a row at frame 60 (or any frame)
     - Set all input columns to 0
     - Set "Reset Checkpoint" column to 1
   - Save the CSV

3. **Test consistency:**
   - Load the edited `test.csv` with F11
   - Play it back with F5
   - Observe if the checkpoint reset at frame 60 produces consistent results
   - Re-run playback multiple times to verify determinism

4. **Compare results:**
   - Record player position/rotation values before and after checkpoint reset
   - Verify they match across multiple playback attempts
   - If values drift or differ, checkpoint resets may not be deterministic

### Expected Behavior

**If checkpoint resets ARE deterministic:**
- Playback will be perfectly consistent across multiple runs
- Player position/state will be identical after each reset
- Demo will complete with same final position/state

**If checkpoint resets are NOT deterministic:**
- Playback may diverge after checkpoint reset
- Player position/state may vary slightly between runs
- Demo may fail or produce different results

### Troubleshooting

**Issue:** Checkpoint reset doesn't work
- **Solution:** Check debug log for errors. The `SaveAndCheckpointManager` may use different method/field names than expected. Update reflection code in `ResetToCheckpoint()` accordingly.

**Issue:** Wrong checkpoint is loaded
- **Solution:** Verify checkpoint ID is correct. Use reflection or game debugging tools to inspect `SaveAndCheckpointManager.checkpointNum`.

**Issue:** Demo desync after checkpoint reset
- **Solution:** This indicates checkpoint resets are not deterministic. May need additional state capture/restore beyond what the game's checkpoint system provides.

## Manual Checkpoint Reset in CSV

To manually add a checkpoint reset to a CSV demo:

1. Open the CSV file in a spreadsheet program
2. Find the frame where you want the reset to occur
3. Set the "Reset Checkpoint" column to `1` for that frame
4. All other frames should have `0` in this column
5. Save the CSV

## API Reference

### DemoData Methods

```csharp
// Get if checkpoint reset should occur at frame
public bool GetCheckpointReset(int frame)

// Set checkpoint reset flag for a frame
public void SetCheckpointReset(int frame, bool value)

// Level and checkpoint metadata
public string LevelId { get; set; }
public int CheckpointId { get; set; }
```

### DemoRecorder Methods (Private)

```csharp
// Get current checkpoint ID from game (uses reflection)
private int GetCurrentCheckpointId()

// Reset to last checkpoint (synchronous, uses ResetToLastCheckpoint())
private void ResetToCheckpoint()

// Teleport to specific checkpoint index (uses TeleportToCheckpointIndexDebug())
private void TeleportToCheckpoint(int checkpointId)

// Reset to checkpoint with callback (coroutine for async handling)
private IEnumerator ResetToCheckpointThen(Action afterLoaded)
```

## Limitations

1. **Checkpoint determinism is untested:** This implementation assumes the game's checkpoint system provides deterministic state restoration. This needs to be verified through testing.

2. **Checkpoint reset uses "last checkpoint":** The `ResetToLastCheckpoint()` method resets to whatever checkpoint the player last reached, not to a specific checkpoint ID. This means:
   - The demo must naturally progress through checkpoints in order
   - You cannot arbitrarily reset to any checkpoint during playback
   - Checkpoint resets will go back to the most recently passed checkpoint

3. **Recording starts from level beginning:** Currently, pressing F7 to record always resets the level first. There's no way to start recording from the middle of a level or from a specific checkpoint without manually editing the demo file afterward.

4. **Reflection-based checkpoint access:** Uses reflection to access `SaveAndCheckpointManager.checkpointNum`. May break if game updates change field/property names.

5. **No automatic level loading:** When loading a demo from a different level, user must manually load the correct level first.

6. **Single starting checkpoint per demo:** Each demo stores only one starting checkpoint ID (used when playback begins). Individual checkpoint reset events during playback reset to "last checkpoint" reached in that playback session.

## Future Enhancements

Potential improvements if checkpoint segments prove viable:

1. **Auto-level loading:** Automatically load the correct level when opening a demo
2. **Per-reset checkpoint IDs:** Allow different checkpoint IDs for each reset
3. **Checkpoint position validation:** Verify player is at expected position after reset
4. **UI for adding checkpoints:** Add hotkey to insert checkpoint reset at current frame during playback
5. **Checkpoint segments editor:** Visual tool for splitting demos into segments

## Migration

**Old demos (V1 SLT format) remain compatible:**
- Will load without level/checkpoint metadata
- `LevelId` will be empty string
- `CheckpointId` will be -1
- No checkpoint resets

**Old CSV demos remain compatible:**
- Will load without metadata rows
- "Reset Checkpoint" column is optional
- If missing, no checkpoint resets will occur

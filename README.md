# SuperliminalTAS

A Tool-Assisted Speedrun (TAS) tool for Superliminal that enables deterministic recording, playback, and editing of game inputs.

## Features

### Core TAS Functionality
- **Frame-perfect input recording** - Records all player inputs with perfect accuracy
- **Deterministic playback** - Replays inputs with guaranteed consistency through:
  - Fixed timestep synchronization (TimeManager memory patching)
  - RNG determinism patches
  - Voice line timing control
  - Rewired input system integration
- **Binary recording format** - Compact `.slt` files for efficient storage

### CSV Editing Support (NEW)
- **Export to CSV** - Convert recorded inputs to CSV format for editing in Excel or similar programs
- **Import from CSV** - Load edited inputs back into the game
- **Frame-by-frame editing** - Modify individual inputs at any frame
- **CSV Format**:
  ```csv
  Frame,Move Horizontal,Move Vertical,Look Horizontal,Look Vertical,Jump,Grab,Rotate,JumpDown,GrabDown,RotateDown,JumpUp,GrabUp,RotateUp
  0,0.000000,0.000000,0.000000,0.000000,0,0,0,0,0,0,0,0,0
  1,0.500000,0.000000,0.100000,0.000000,0,0,0,0,0,0,0,0,0
  ```

### Playback Controls (NEW)
- **Pause/Resume** - Freeze and resume playback at any point
- **Speed Control** - Fast forward playback at 1x, 2x, 4x, or 8x speed
- **Frame-accurate control** - Maintains perfect sync even when paused or fast-forwarded

## Hotkeys

### Recording & Playback
- **F5** - Start playback (requires loaded demo)
- **F6** - Stop recording/playback
- **F7** - Start recording

### File Operations
- **F8** - Reload current file (for quick iteration when editing CSV)
- **F10** - Export to CSV
- **F11** - Open file (supports both `.slt` and `.csv`)
- **F12** - Save binary `.slt` file

### Playback Controls (During Playback Only)
- **SPACE** - Pause/Resume playback
- **[ or -** - Decrease playback speed (8x → 4x → 2x → 1x)
- **] or +** - Increase playback speed (1x → 2x → 4x → 8x)

## Workflow

### Creating a TAS

1. **Start Recording**
   - Launch Superliminal with the mod installed
   - Press **F7** to start recording
   - Play through the section you want to record
   - Press **F6** to stop recording

2. **Save Your Recording**
   - Press **F12** to save as binary `.slt` file (recommended for replay)
   - Press **F10** to export as CSV (for editing)

3. **Edit in Excel** (Optional)
   - Open the CSV file in Excel or any spreadsheet program
   - Edit individual frame inputs:
     - **Axes**: Float values from -1.0 to 1.0
     - **Buttons**: 0 (not pressed) or 1 (pressed)
     - **ButtonsDown**: 1 on the frame the button is first pressed, 0 otherwise
     - **ButtonsUp**: 1 on the frame the button is released, 0 otherwise
   - Save the CSV file

4. **Load Edited Inputs**
   - Press **F11** to open your CSV file
   - Verify the frame count in the UI

5. **Test Playback**
   - Press **F5** to start playback
   - Use **SPACE** to pause and examine specific moments
   - Use **[** / **]** to speed up or slow down playback
   - Press **F6** to stop

6. **Refine and Iterate**
   - Edit the CSV file in Excel
   - Press **F8** to quickly reload without navigating the file dialog
   - Test again with **F5**
   - Repeat until perfect

### Input Types

#### Axes (Range: -1.0 to 1.0)
- **Move Horizontal** - Strafe left (-1) / right (+1)
- **Move Vertical** - Move backward (-1) / forward (+1)
- **Look Horizontal** - Camera pan left (-1) / right (+1)
- **Look Vertical** - Camera pitch down (-1) / up (+1)

#### Buttons (Values: 0 or 1)
- **Jump** - Current state of jump button
- **Grab** - Current state of grab button
- **Rotate** - Current state of rotate button

#### Button Events (Values: 0 or 1)
- **JumpDown/GrabDown/RotateDown** - Set to 1 on the frame the button is first pressed
- **JumpUp/GrabUp/RotateUp** - Set to 1 on the frame the button is released

## Technical Details

### Frame-Perfect Synchronization
The tool uses x86 memory patching to force Unity's TimeManager to report a fixed `deltaTime` on every frame, ensuring:
- `Update()` and `FixedUpdate()` run in perfect lockstep
- No frame skips or doubles
- Playback is deterministic regardless of system performance

### Determinism Patches
- **RNG**: All random number generation returns deterministic values
- **Voice Lines**: Audio duration is controlled by frame count
- **Input Timing**: Rewired delta time is locked to fixed timestep
- **Focus**: Game continues running when alt-tabbed

### File Formats

#### Binary (.slt)
- Compact binary format for efficient storage
- Header: "SUPERLIMINALTAS1" (16 bytes)
- Frame count: int32 (4 bytes)
- Input data: Packed floats and bools

#### CSV (.csv)
- Human-readable format for editing
- Header row with column names
- One row per frame
- Comma-separated values

## Building

This is a BepInEx mod for Superliminal. To build:

1. Install BepInEx for Superliminal
2. Build the project with your C# compiler
3. Copy the DLL to `BepInEx/plugins/`

## Credits

Built with:
- **BepInEx** - Mod framework
- **Harmony** - Runtime patching
- **Rewired** - Input system integration
- **StandaloneFileBrowser** - Native file dialogs

## License

See LICENSE file for details.

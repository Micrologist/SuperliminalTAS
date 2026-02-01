# Code Review: SuperliminalTools

## Bugs

### 1. `OnDisable` typo in `ColliderVisualizer.cs:222` — visualization won't hide when disabled

The method is named `OnDiable` instead of `OnDisable`. Unity will never call it, so disabling a `ColliderVisualizer` component won't hide its visual object.

```csharp
// Line 222: "OnDiable" should be "OnDisable"
private void OnDiable()  // <-- typo, never called by Unity
```

### 2. Missing `Instance` assignment in `PracticeModController.Awake` (`PracticeModController.cs:19-27`)

The singleton guard checks for duplicates but never assigns `Instance = this`, so `Instance` will always be `null`. Any code calling `PracticeModController.Instance` will get a null reference.

```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(this);
        Debug.LogError("Duplicate PracticeModController");
        return;
    }
    // Missing: Instance = this;
}
```

### 3. Missing `Instance` assignment in `TASModController.Awake` (`TASModController.cs:27-34`)

Same issue as above. `TASModController.Instance` is never set.

### 4. `TeleportToStoredPosition` uses `ScalePlayer` (multiply) instead of `SetPlayerScale` (absolute) (`TeleportAndScaleController.cs:56`)

`_storedScale` stores an absolute scale value (e.g. `1.0`), but `ScalePlayer(factor)` *multiplies* the current scale by that factor. After teleporting, the player scale will be wrong unless they happen to already be at scale `1.0`. Should use `SetPlayerScale(_storedScale)`.

### 5. `DemoRecorder.FixedUpdate` dead-code condition (`DemoRecorder.cs:92`)

```csharp
if (_lastUpdateWasFixed && _recording && _playingBack)
```

`_recording && _playingBack` can never both be true simultaneously — every state transition sets one false when setting the other true. This warning will never fire. Likely should be `_recording || _playingBack`.

### 6. Flashlight leak on scene reload (`FlashlightController.cs:47-60`)

`OnSceneLoaded` creates a new `_flashlight` GameObject every time a scene loads without destroying the previous one. The old flashlight is orphaned in the scene. Should destroy the existing flashlight before creating a new one.

### 7. `WithUnlockedCursor` doesn't restore `lockState` (`DemoRecorder.cs:178-187`)

The cursor lock state is set to `CursorLockMode.None` but never restored in the `finally` block — only `Cursor.visible` is reset. After opening/saving a demo file, the cursor remains unlocked.

### 8. Suspicious background alpha value (`RenderDistanceController.cs:57`)

```csharp
playerCamera.backgroundColor = new Color(.15f, .15f, .15f, 15f);
//                                                          ^^^
```

Alpha of `15f` is unusual. Unity color alpha is typically `0-1`. This likely should be `0.15f` or `1f`.

---

## Typos / Naming

### 9. Filename `UtlityPatches.cs` — should be `UtilityPatches.cs`.

### 10. `HotCofeeErrorPatch` (`UtlityPatches.cs:147`) — should be `HotCoffeeErrorPatch`.

### 11. Parameter `scnee` in `FadeController.OnSceneLoaded` (`FadeController.cs:36`) — should be `scene`.

---

## Code Quality

### 12. Magic numbers for layer masks

- `PathProjector.cs:84`: `(LayerMask)1807218687` — undocumented. Should use named constants or `LayerMask.GetMask(...)`.
- `GizmoVisibilityController.cs:51`: culling mask `-32969` — same issue.

### 13. Inconsistent singleton patterns

Some controllers use `{ get; private set; }` auto-properties (`NoClipController`, `PracticeModController`), while others use plain `public static` fields (`HUDController`, `FadeController`, `TeleportAndScaleController`). This makes the mutability contract unclear.

### 14. Inconsistent access modifiers in `TASInput`

- `GetButtonUp` is `public` while `GetButton`, `GetButtonDown`, `GetAxis` are `internal`.
- Fields `blockAllInput`, `passthrough`, `disablePause` are all `public` on an `internal` class — `internal` would suffice.

### 15. Empty catch block (`DemoRecorder.cs:408-411`)

```csharp
catch (Exception e)
{
    // Silently ignore errors (file might be temporarily locked during writing)
}
```

The exception variable `e` is captured but never used. Even for expected file-locking scenarios, a `Debug.LogWarning` on the first occurrence would help debugging.

### 16. Material allocation on every `ToggleGizmosVisible` call (`GizmoVisibilityController.cs:72-93`)

`SetDefaultTriggerBoxMaterial` creates a new `Material` and iterates all `_Interactive` tagged objects every time gizmos are toggled or a scene loads. The material could be cached.

### 17. `VoiceOverProxy.VoicelineEndFrames` is a public mutable static dictionary (`VoiceOverProxy.cs:25`)

Should be `internal` at minimum to prevent external mutation.

### 18. Redundant stop calls in desync handler (`DemoRecorder.cs:82-83`)

```csharp
StopPlayback();
StopRecording();
```

Only one of `_recording` or `_playingBack` can be true, so one of these calls is always a no-op. Not harmful, but could be clearer with a conditional.

---

## Summary

The most impactful issues are **#1-#5** (actual bugs that affect runtime behavior). Items #1 (typo preventing `OnDisable`), #2-#3 (null `Instance` singletons), and #4 (wrong scale on teleport) are likely causing visible problems for users. The rest are maintainability and code hygiene items.

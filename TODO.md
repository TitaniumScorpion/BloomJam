# Devil Engine — Backlog

Outstanding work, tracked against the GDD in [README.md](README.md).
Every item below was verified against the code on **2026-09-02**, after the six-step refactor pass.

Priority key: **P1** = contradicts a stated design pillar · **P2** = named in the GDD, not built · **P3** = polish / tech debt

---

## 1. Design pillars the code currently contradicts

These are places where the shipped behaviour disagrees with the GDD. Cheap to fix, high impact.

### P1 — The hidden quota is not hidden
- **GDD:** *"The lack of a UI counter creates psychological pressure, forcing the player to stay aggressive rather than hiding."*
- **Code:** `GameManager.cs:263` — `quotaText.text = $"KILLS: {currentKills} / {targetQuota}"`
- **Fix:** stop populating `quotaText`, or replace the count with a non-numeric intensity cue (screen edge, audio bed) that conveys "close" without a number.
- **Note:** decide deliberately. Playtesting may have shown the counter feels better — if so, update the GDD instead, so doc and build agree either way.

### P1 — Death screen shows time, not completion percentage
- **GDD:** *"A death screen displays exactly how far they made it as a percentage (e.g., 'Run Failed: 42% Complete')."*
- **Code:** `GameManager.cs:285` — `deathTimeText.text = $"TIME ALIVE: ..."`
- **Fix:** everything needed already exists. Roughly:
  `(zonesCleared + currentZoneProgress) / totalZones` from `QuotaManager.currentZoneIndex`, `currentKills`, and `targetQuotas`.
- Keep the timer too — it serves the speedrun framing. This is an addition, not a replacement.

### P2 — No hit-stop
- **GDD:** *"Combat features simplistic enemy animations, dramatic action lines, and intense hit-stop effects."*
- **Code:** camera shake and material hit-flash exist; hit-stop does not.
- **Fix:** brief `Time.timeScale` dip on kill, or a per-enemy freeze frame. Must not fight bullet time, which deliberately avoids `timeScale` (see §4).

### P3 — Bunnyhop is structurally impossible
- **GDD:** *"Extremely fast... prioritizing momentum and spatial awareness. (Bunnyhop maybe?)"*
- **Code:** `PlayerController.SpeedControl()` (line ~262) hard-clamps flat velocity to `moveSpeed` every frame, so speed can never be built or preserved.
- **Fix:** only clamp while grounded, or raise the ceiling while airborne. The GDD marks this "maybe" — treat as a design decision, not a bug.

---

## 2. GDD features not yet built

Ordered roughly by milestone.

| Feature | GDD milestone | Status |
|---|---|---|
| **Level 5 boss** | M3 | Not started. `AdvancedEnemy` is an elite artillery unit, not the climax boss. |
| **Escalating per-zone enemy rosters** | M3 | All zones share one `EnemySpawner` shape; escalation would come from per-zone Inspector values. |
| **Weapon 3 ("Bilmemne Gun")** | M2/M4 | Not started, including its first-clear unlock. |
| **Difficulty tiers ("Hell and Hell")** | M4 | Not started. No run modifiers. |
| **Ink / hatching shaders, neon bloom, action lines** | M4 | Not started. |
| **Leaderboards + score persistence** | M5 | Not started. Nothing is saved between sessions at all. |

### Upgrade system is 4 tiers, not 5
- **GDD:** *"Upgrading increases its Tier (1 to 5)."*
- **Code:** `UpgradeManager.PistolDescriptions` / `SwordDescriptions` each hold 4 entries.
- The sword changes model per tier (`swordLevelVisuals`); **the pistol has no per-tier visual change**, which the GDD calls for.
- Adding a 5th is now cheap: append to the description array and add a `case 4:` in `ApplyPistolUpgrade` / `ApplySwordUpgrade`. The debug overlay picks it up automatically.

### One upgrade per clear is shared across both weapons
`HasPendingUpgrade` is a single bool, so taking the pistol upgrade consumes the sword's chance too. This matches "choose one" — recorded only so it reads as intentional rather than a bug.

---

## 3. Built but never wired up

Serialized fields with clips/values assignable in the Inspector that **no code reads**. Each is a feature someone started.

### Enemy spawn telegraph — directly serves a GDD pillar
- `AudioManager.enemySpawnTelegraphSound` / `enemySpawnTelegraphVolume` — **0 usages** outside their declaration.
- **GDD:** *"Players must be able to pinpoint spawn locations and approaching swarmers purely by sound cues."*
- **Fix:** play it in `SpawnerDrone.SpawnPack()` at the drone's position before the pack drops.

### Elevator ascend sound
- `AudioManager.elevatorAscendSound` / `elevatorAscendVolume` — **0 usages**.
- **Fix:** play from `GameManager.StartCurrentZone()` or `ElevatorHub.OnStartPressed()`.

### Muzzle flash
- `AutomaticPistol.muzzleFlashEnabled` — added during the refactor, **defaults to `false`** (preserving the previous disabled-for-testing behaviour).
- **Fix:** needs a `MuzzleFlash` pool on the `ObjectPooler`, then tick the box. Feeds the GDD's "visceral feedback".

### Elite enemies are off by default
- `EnemySpawner.maxAdvancedEnemiesToSpawn = 0` (line 27) — so `AdvancedEnemy` **never spawns** unless raised per zone.
- Intentional or forgotten? Worth confirming.

---

## 4. Architecture notes for whoever works here next

Not bugs — constraints that are easy to violate accidentally.

### Bullet time does not use `Time.timeScale`
The player keeps moving at full speed while everything else stops, so **every moving object freezes itself** by polling `KatanaWeapon.IsBulletTimeActive`.

**Any new enemy or projectile type must handle this**, or it will keep moving during bullet time. Use the `BulletTimeFreeze` struct — one field plus one `Tick(rb)` call. `FlyingChaserEnemy` is the exception: it recomputes velocity from scratch each frame, so it just zeroes velocity with nothing to restore.

This also constrains hit-stop (§1) — a `timeScale` dip would interact with bullet time and needs designing around.

### Adding an enemy type
1. Implement `IDamageable` (or inherit `FlyingChaserEnemy`, which already does).
2. Call `EnemyEvents.ReportDeath()` on death, or the zone quota won't count it.
3. Handle bullet time (above).
4. Add a pool entry on `ObjectPooler` — `DeactivateAll()` then covers it at zone transitions automatically.

### Player-facing scripts need three gates
`Time.timeScale == 0` (countdown), `!GameManager.HasGameStarted` (pre-run), `ElevatorHub.IsActive` (hub browsing). Weapons get this free via `HandheldWeapon.CanAct()`.

### Serialization rules that bit us
- Unity serializes by **public field name**, walking the inheritance chain — moving a field to a base class is safe, **renaming it silently wipes the value**.
- Move `.cs` and `.cs.meta` **together**, always.
- `Quaternion.operator==` compares by dot product, so `default(Quaternion) == default(Quaternion)` is **`false`**. Never use `== default` on a Quaternion. (This bit us in `HandheldWeapon.ApplySway`; it uses `Quaternion?` now.)

---

## 5. Deferred tech debt

### Skipped deliberately — `AutomaticPistol` fire-mode split
475 lines, the largest file. Was step 7 of the refactor pass; **skipped on purpose** — the charge-shot state machine is the most intricate logic in the project and splitting it is file-size cosmetics for real regression risk. Revisit only if the pistol gains more fire modes.

### Delete before shipping
- `DebugUpgradeKeys.cs` — marked TEMPORARY in its own header. Keys 1–8 toggle every upgrade. **Must not ship.**
- `MissingScriptFinder.cs` — editor-only utility; belongs in an `Editor/` folder so it's excluded from builds.

### Smaller items
- **`ObjectPooler` pools never grow.** `SpawnFromPool` cycles a fixed queue, so exceeding a pool's size silently recycles a live instance (a bullet vanishing mid-flight). Size pools for worst-case density, or grow on exhaustion like `AudioManager` already does.
- **`AdvancedEnemy.OnEnable` calls `GameObject.Find("TargetZone")`** (line 54) on every spawn — a name-based scene lookup in a hot path, and it silently warns if the active zone has no such object.
- **`EnemySpawner.GetAdvancedSpawnPosition`** runs `FindObjectsByType<AdvancedEnemy>` during gameplay, not just at transitions.
- **Script folder organisation.** `Assets/Scripts/` is flat at 33 files. Subfolders (`Enemies/`, `Weapons/`, `Core/`, `UI/`) would help — move `.cs` + `.meta` together.
- **Player is 1-hit fragile** (`PlayerHealth.maxHealth = 1`), matching the GDD. Recorded because it makes every combat change high-stakes to test.

---

## Completed — refactor pass, 2026-09-02

| Step | Change |
|---|---|
| 1 | `IDamageable` — collapsed a 5-branch type chain repeated at 4 call sites |
| 2 | Dead code removed; all 11 compiler warnings cleared; `ObjectPooler.DeactivateAll()` replaced 6 scene scans per zone transition and fixed incomplete cleanup |
| 3 | `FlyingChaserEnemy` base — `StandardSwarmer` 263→11 lines, `TrailEnemy` 245→70; `EnemyEvents` decoupled the kill event from `StandardSwarmer` |
| 4 | `HandheldWeapon` base — shared view-model sway, show/hide, and input gating for both weapons |
| 5 | `BulletTimeFreeze` — one freeze helper replacing three drifting copies |
| 6 | `DebugUpgradeKeys` now calls the real `UpgradeManager` upgrades instead of hardcoded copies |

Net: **332 deletions / 183 insertions**, 5 new focused files, 0 compiler warnings, every serialized field name verified unchanged.

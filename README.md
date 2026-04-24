# BloomJam - Game Design Document

## Game Overview
Devil Engine is a fast-paced, skill-based, first-person arena shooter heavily inspired by *Devil Daggers*. Players are dropped into a dark, confined arena with no cover and must battle their way through 3 distinct zones filled with otherworldly enemies. The primary focus is on intense, split-second decision-making, fluid movement, and aiming precision. The ultimate goal is to clear all 3 zones as quickly as possible, maximizing your score to climb the leaderboards.

## Core Gameplay Loop
1. **Spawn:** The player drops into the first of 3 distinct zones.
2. **Eliminate & Advance:** Swarms of enemies spawn aggressively. The player must kill a specific quota of enemies to complete the current zone and unlock the next.
3. **Collect Currency:** Defeating specific elite enemies drops currency (e.g., gems/souls). This collected currency persists between runs.
4. **Die, Unlock & Repeat:** The player is extremely fragile. Upon death or victory, the clear time is logged. Before starting a new run, a pre-run menu appears where the player can spend retained currency to unlock the next weapon (starting with the second weapon) before jumping back in.

## Player Mechanics & Movement
*   **Base Movement:** Extremely fast, highly responsive first-person controls (WASD) prioritizing momentum and situational awareness.
*   **Normal Jumping:** A standard jump mechanic allowing the player to leap over smaller ground enemies or evade sweeping, low-level attacks.
*   **Dash:** A quick, directional burst of speed (forward, backward, or strafing) used to instantly reposition, dodge incoming projectiles, or escape from being swarmed. The dash operates on a short cooldown (or stamina system) to encourage tactical use rather than continuous spamming.

## Weapons & Combat System

*   **Weapon 1 (Default): Automatic Pistol**
    *   **Description:** The starting weapon. A rapid-fire pistol that shoots a continuous stream of energy projectiles.
    *   **Function:** Designed for precision aiming and taking down individual or lined-up enemies quickly. It has no ammo limit.
*   **Weapon 2 (Unlockable): Impact Bomb**
    *   **Description:** A throwable explosive that detonates 2 seconds after being launched.
    *   **Function:** Creates a large area-of-effect (AoE) blast, perfect for clearing dense clusters of weaker enemies. The explosion also launches the player into the air if they are caught in the blast radius, allowing for a high-skill "bomb jump" to reach greater heights and evade ground-based threats.

## Enemy Types & Behaviors

*   **Standard Swarmer (e.g., "Skull" or "Skitter")**
    *   **Description:** A small, fragile, and aggressive otherworldly entity.
    *   **Behavior:** Spawns in massive numbers. Relentlessly chases the player in a direct path.
    *   **Threat Level:** Low individually, but highly lethal when they form dense clusters that block movement and dash routes. Can be destroyed with a single pistol shot.
*   **Elite Boss (e.g., "The Hive" or "Leviathan")**
    *   **Description:** A towering, imposing monstrosity that acts as the primary anchor for a wave.
    *   **Behavior:** Moves slowly but acts as a mobile spawner, periodically expelling groups of Standard Swarmers into the arena. Requires sustained fire from the pistol or strategic use of Impact Bombs to take down.
    *   **Threat Level:** High priority target. If left alive, it will quickly flood the arena and make survival impossible.
    *   **Reward:** Defeating this boss causes it to shatter and drop the currency (gems/souls) used to unlock new weapons in the pre-run menu.

## Wave Progression & Spawning System
*   **Zone Progression:** The game is divided into 3 distinct zones/levels. To progress to the next zone, the player must kill a stated amount of mobs.
*   **Spawn Telegraphing:** Enemies spawn rapidly from the darkness or dimensional rifts. A brief visual and spatial audio cue warns the player just before a spawn occurs, allowing for split-second tactical positioning.
*   **Milestone Events:** Reaching certain kill count thresholds triggers "Milestone Events," guaranteeing the spawn of an Elite Boss (or multiple), abruptly shifting the flow of combat and forcing the player to prioritize targets to get currency.
*   **Swarm Density:** The system constantly pressures the player. Failing to clear enemies fast enough will result in the arena filling up, boxing the player in and making it harder to survive and achieve a fast clear time.

## Arena Design & Environment

*   **Zone 1: The Threshold**
    *   **Layout:** A large, perfectly flat, circular platform suspended in a void. There is no cover and no verticality.
    *   **Purpose:** To test the player's raw aim, movement, and ability to handle the initial swarms. This is a pure test of fundamental skills.
*   **Zone 2: The Pillars of Ruin**
    *   **Layout:** A multi-tiered arena with several large, indestructible pillars. The area is larger than Zone 1, but sightlines are broken up. Ramps or stairs connect the different levels.
    *   **Purpose:** To challenge the player's spatial awareness. Pillars can be used for cover but also create ambush points for enemies. Players must master bomb jumping and dashing to navigate the verticality effectively.
*   **Zone 3: The Collapsing Core**
    *   **Layout:** A chaotic and dynamic arena. The outer sections of the floor periodically crumble and fall away, shrinking the playable space. Environmental hazards, like energy beams that sweep across the arena, activate at set intervals.
    *   **Purpose:** The ultimate test of skill. Players must not only manage the most intense enemy waves but also constantly adapt to a shrinking and hostile environment. One wrong move can lead to an environmental death.

## Upgrades & Power-ups

## Scoring & Leaderboards
*   **Time-Based Scoring:** The score is directly tied to how quickly the player clears zones. A faster completion time yields a significantly higher score.
*   **Leaderboards:** Players will be ranked on a global leaderboard based on their final score (shortest time taken to complete all 3 zones).

## Art Style & Visual Aesthetics
*   **Graphic Novel / Ink Aesthetic:** The game features a striking, highly stylized visual style heavily inspired by *Mortal Sin*. It utilizes thick black outlines, dramatic crosshatching, and a gritty, hand-drawn-in-ink look.
*   **High-Contrast Monochromatic Palette:** The world and standard enemies are primarily rendered in deep, oppressive blacks and stark whites (or a very restricted color palette), creating a grim and intense atmosphere.
*   **Striking Accent Colors & "Bloom":** To ensure gameplay readability and tie into the "BloomJam" name, vital elements like player projectiles, enemy weak points, and currency are rendered in blinding, hyper-saturated neon colors with heavy bloom effects. This creates a spectacular contrast against the dark, sketchy backgrounds.
*   **Visceral Combat Feedback:** Combat feels incredibly impactful with aggressive, exaggerated enemy animations, dramatic action lines, and intense hit-stop effects that fit the frantic graphic-novel style.

## Audio Design & Music
*   **Visceral Sound Effects:** Combat audio is punchy, crunchy, and heavily stylized. Gunshots are loud and impactful, and enemy deaths feature aggressive, shattered, or tearing sounds that complement the grim graphic novel aesthetic.
*   **Crucial Spatial Audio:** High-fidelity 3D audio is mandatory for survival. Players must be able to pinpoint spawn locations, approaching swarmers, and incoming projectiles purely by sound cues before they even see them.
*   **Aggressive Soundtrack:** The music features a high-BPM, adrenaline-pumping blend of industrial metal, breakcore, or dark synthwave. It is designed to induce a flow state while feeling oppressive and frantic.
*   **Dynamic Escalation:** The audio landscape is dynamic; the music adds layers or increases in intensity as the player's kill count approaches the zone's milestone, or when an Elite Boss spawns, elevating the tension.

## Technical Architecture & Milestones

### Technical Architecture
*   **Engine:** Unity 3D (using Universal Render Pipeline for optimal performance, custom shader graphs, and advanced bloom post-processing).
*   **Object Pooling:** A mandatory, highly optimized system for managing all projectiles, standard enemies, and particle effects to prevent garbage collection stutters and maintain high frame rates during massive swarms.
*   **Event-Driven Architecture:** Decoupled systems utilizing C# Actions or ScriptableObject events. For example, an enemy dying broadcasts an event that the Score Manager, Quota Manager, and Audio Manager all listen to independently.
*   **State Machine (FSM):** Used for cleanly managing overall game flow (Pre-Run Menu, Zone Active, Death Screen, Victory) as well as structured enemy AI behaviors.
*   **Custom First-Person Controller:** A highly responsive, momentum-based custom character controller (Rigidbody or Kinematic) tailored to support snappy inputs, dashing, and the physics-based bomb-jumping.

### Development Milestones
*   **Milestone 1: First Playable (Graybox)**
    *   Implement core player movement (WASD, Jump, Dash) in a flat test arena.
    *   Implement Weapon 1 (Pistol) with basic raycast or projectile hit detection.
    *   Create the Standard Swarmer enemy with basic pathfinding/chasing logic.
*   **Milestone 2: The Swarm & Core Loop**
    *   Build the Wave Spawner, Zone Quota Manager, and robust Object Pooling system.
    *   Implement the Elite Boss enemy behavior and currency dropping.
*   **Milestone 3: Arsenal & Meta-Progression**
    *   Develop Weapon 2 (Impact Bomb) including AoE damage and physics-based blast jumping.
    *   Create the pre-run unlock menu, persistent currency saving, and basic score tracking.
*   **Milestone 4: Visuals & Sound (The "Bloom" & "Sin" Pass)**
    *   Develop and apply the custom graphic novel/ink shaders and high-contrast bloom effects.
    *   Integrate spatial 3D audio, dynamic music switching, and visceral combat SFX.
*   **Milestone 5: Polish, Optimization & Leaderboards**
    *   Finalize blockouts for Zones 2 and 3, including environmental hazards.
    *   Aggressively balance enemy spawn rates, weapon damage, and quota thresholds.
    *   Integrate a global leaderboard system and perform deep profiling to ensure performance.
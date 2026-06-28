# Devil Engine Game Design Doc

## Game Overview

A fast-paced, skill-based, first-person roguelike arena shooter prioritizing fluid movement and speed. Players are dropped into a dark, sprawling, brutalist megastructure and must battle through 5 escalating arenas. The goal is to clear all 5 zones as quickly as possible to achieve high rating in global speedrun leaderboards. Death is permanent, sending players back to the beginning, but victory unlocks new difficulty modifiers and expanded weapon choice.

## Core Gameplay Loop

1. **Spawn:** The player drops into the first of 5 arenas.
2. **Slaughter:** Enemies spawn aggressively in packs. The player must survive and kill a hidden quota of enemies to trigger the arena's completion.
3. **Upgrade:** Upon clearing an arena, the player is presented with a choice: upgrade their Pistol or upgrade their Sword for the next level.
4. **Advance or Die:**
    - **Death:** The player is fragile. Upon death, the run ends entirely. A death screen displays exactly how far they made it as a percentage (e.g., "Run Failed: 42% Complete"). All weapon upgrades are lost.
    - **Advance:** Proceed to the next arena with escalating enemy types and density.
5. **Ascend (Win State):** Clearing Level 5 unlocks the next Difficulty Tier (Hell and Hell) and, on the first full clear, a brand-new 3rd weapon for future runs.

## Player Mechanics & Movement

*   **Base Movement:** Extremely fast, highly responsive first-person controls (WASD) prioritizing momentum and spatial awareness.
*   **Normal Jumping:** A standard jump to clear small gaps, vault low hazards, or evade ground sweeps. (Bunnyhop maybe?)
*   **Dash:** A quick, directional burst of speed to instantly reposition or dodge. Operates on a short cooldown to encourage a reactionary use rather than spamming.
*   **Dual-Wielding:** The player constantly wields two weapons simultaneously: A gun in one hand and a blade in the other, allowing for seamless transitions between ranged and close-quarters battle.

## Weapons & Combat System

*   **Weapon 1: Automatic Pistol (Ranged)**
    *   **Function:** Shoots a continuous stream of energy projectiles.
    *   **Upgrades:** Upgrading increases its Tier (1 to 5). Upgrades could introduce ricochets, explosive rounds, or piercing beams, accompanied by subtle visual changes to the gun model and projectile FX.
*   **Weapon 2: Sword (Melee)**
    *   **Function:** Wide, sweeping melee attacks that deal heavy damage up close and can potentially parry or deflect specific incoming threats.
    *   **Upgrades:** Upgrading increases its Tier (1 to 5). Upgrades could increase swing speed, add a lunge effect, or introduce energy waves on consecutive hits. The blade's visual aura and geometry shift with each tier.
*   **Weapon 3: Bilmemne Gun**
    *   **Function:** Unlocked only after defeating Level 5 for the first time. Adds a new starting option or mechanic to drastically alter the playstyle for higher difficulty runs.

## Enemy Types & Behaviors

*Enemy variety increases as the player progresses deeper into the 5 arenas.*

*   **Standard Swarmer:** Small, fragile, and aggressive. Spawns in massive numbers and relentlessly chases the player. (Zone 1 Addition: a secondary basic enemy (e.g. a ranged attacker that fires projectiles or a heavily armored charger that requires dodging)).
*   **Escalating Roster:** Each subsequent arena introduces new enemy archetypes into the spawn pools, forcing players to constantly re-evaluate target prioritization as the swarms become more diverse.
*   **Boss:** There are no bosses in Levels 1-4. The climax of the run features a massive monstrosity at the end of Level 5, testing everything the player has built.

## Wave Progression & Arena Design

*   **The Hidden Quota:** To progress, the player must kill a specific amount of enemies. The lack of a UI counter creates psychological pressure, forcing the player to stay aggressive rather than hiding.
*   **Pack Spawning:** Enemies do not trickle in; they spawn in coordinated bursts or packs via dimensional rifts or dark corners.
*   **Unified Arena Logic:** All 5 arenas share a similar aesthetic and core layout logic. While the architecture might grow more chaotic or visually corrupted in later levels, the fundamental strategy of movement and line-of-sight management remains consistent.

## Scoring, Leaderboards & Ascensions

*   **Speedrun Focus:** The primary metric of success is the run's finish time.
*   **Additional Difficulties:** Beating Level 5 unlocks "Difficulty 2," which introduces permanent run modifiers (e.g., faster enemies, environmental hazards, tighter quotas).
*   **Segmented Leaderboards:** Global leaderboards rank players by completion time, with separate boards for each difficulty tier, ensuring a highly competitive endgame.

## Art Style & Visual Aesthetics

*   **Tsutomu Nihei meets Mortal Sin:** A striking, high-contrast visual style heavily utilizing thick black outlines and a gritty, hand-drawn ink aesthetic layered over colossal, brutalist megastructures.
*   **Colour Palette:** The environment and enemies are rendered in deep blacks and stark whites. Vital elements; player projectiles, enemy weak points, and sword trails are rendered in hyper-saturated neon colours (CRT/Cyberpunk accents) with heavy bloom.
*   **Visceral Feedback:** Combat features simplistic enemy animations, dramatic action lines, and intense hit-stop effects.

## Audio Design & Music

*   **Spatial Audio:** High-fidelity 3D audio. Players must be able to pinpoint spawn locations and approaching swarmers purely by sound cues.
*   **Aggressive Soundtrack:** High-BPM blend of breakcore, or dark synthwave designed to induce a flow state.

## Technical Architecture & Milestones

### Technical Architecture

*   **Engine:** Unity 6 (leveraging the Universal Render Pipeline for optimal performance, custom shader graphs for the ink/hatching effects, and advanced bloom post-processing).
*   **Object Pooling:** Mandatory for managing projectiles, swarmers, and particle effects to prevent garbage collection spikes during high-density combat.
*   **Event-Driven Architecture:** Decoupled systems (C# Actions) to manage the hidden quota, audio cues, and state transitions seamlessly.
*   **Custom FPS Controller:** A highly responsive, momentum-based Rigidbody controller tailored for snappy dual-wielding combat and dashing.

### Development Milestones

*   **Milestone 1: The Core Feel (Graybox)**
    *   Implement WASD, Jump, Dash.
    *   Implement dual-wielding basics (Pistol fire and Sword swing hitboxes).
    *   Create the Standard Swarmer and the pack-spawning logic.
*   **Milestone 2: The Roguelike Loop**
    *   Build the 5-level progression system with the hidden kill quota.
    *   Implement the post-arena upgrade choice (Tier 1 to 2 transitions).
    *   Build the percentage-based Death Screen and run reset logic.
*   **Milestone 3: Escalation & The Boss**
    *   Design and integrate the remaining enemy types for Levels 2-5.
    *   Develop the Level 5 Boss fight and the Victory state.
*   **Milestone 4: Art, Audio & Ascensions**
    *   Apply the custom graphic novel/ink shaders and neon bloom effects.
    *   Implement the Slay the Spire-style difficulty modifiers for subsequent runs.
*   **Milestone 5: Polish & Leaderboards**
    *   Integrate the speedrun timer and global leaderboards per difficulty.
    *   Aggressive optimization and profiling in Unity 6 for massive enemy counts.

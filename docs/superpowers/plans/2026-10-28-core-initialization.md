# Core Initialization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Separate Core readiness from game-data readiness and allow explicit empty test startup without hiding requested-load failures.

**Architecture:** Extend Managers and LoadScene. Keep existing service APIs; add an observable preparation coroutine and a safe resource preload path. No new manager or runtime assembly layout.

**Tech Stack:** Unity 6000.5.0f1, Addressables 2.9.1, Newtonsoft.Json, Unity Test Framework.

**Spec:** docs/superpowers/specs/2026-10-28-core-initialization-design.md

## Global Constraints

- Existing scenes, prefabs, package versions and user changes remain intact.
- Empty preloadLabel and requireSkillData=false allow TestReady, not GameDataReady.
- Requested loading or data validation failure blocks scene entry.
- MP terminology and undecided gameplay rules remain as specified by the user.
- Work in the current feature/poolmanager checkout; do not move the user's active Unity workspace or commit their scene changes.

## Review Focus

- Repeated Init must not duplicate sound roots or sources.
- Missing required data must report failure and allow retry.
- Empty configured label must finish as failure instead of hanging.
- Existing sound root must reconnect sources.
- Destroying the loading owner must not publish a stale success or leak operation handles.

### Task 1: Core initialization

**Files:** Managers.cs, Core/SoundManager.cs; Assets/Tests/PlayMode/CoreInitializationTests.cs and a test asmdef.

**Interfaces:** Managers.Init(), Initialized; SoundManager.Init().

- [x] Write runtime behavior tests through reflection to Assembly-CSharp, preserving existing assembly boundaries.
- [x] Run Unity PlayMode tests and observe sound readiness failures before modifying production code.
- [x] Initialize pool and sound once, publish Core readiness only after success, and own sound root lifetime.
- [x] Run tests for idempotence, existing sound roots, and destruction.

### Task 2: Resource/data preparation

**Files:** Core/ResourceManager.cs, Core/DataManager.cs, Managers.cs; CoreInitializationTests.cs.

**Interfaces:** ResourceManager.PreloadAsync(string, Action<bool,string>); DataManager.Init(), IsInitialized; Managers.PrepareGame(string,bool), PreparationState, PreparationError, GameDataReady.

- [x] Add failing tests for empty optional preparation, missing required data, invalid labels, malformed/duplicate data and retry.
- [x] Implement safe resource preload and transactional data validation.
- [x] Implement preparation states, matching concurrent-request waiting and explicit failure reporting.
- [x] Run PlayMode tests and inspect errors/logs.

### Task 3: Loading scene integration and handoff

**Files:** Framework/Scenes/LoadScene.cs; CoreInitializationTests.cs; plan progress.

**Interfaces:** LoadScene consumes Managers.PrepareGame and preparation result; uses existing MainTestScene target.

- [x] Add a failing test that preparation failure does not load MainTestScene.
- [x] Add optional serialized settings with safe defaults; await preparation and gate scene entry.
- [x] Run the entire available PlayMode suite and Unity compilation; inspect diff and unchanged user-scene hashes.
- [x] Request an independent review, resolve substantive findings, and document verified behavior and remaining setup.

## Execution notes

The user approved the concrete Core/TestReady separation and requested execution in priority order. Continue in this session without another approval round. Leave changes reviewable and uncommitted; no branch integration is requested.

# Resource Loading Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement task-by-task.

**Goal:** Unify resource loading so concurrent requests, failures and clearing have one ownership contract.
**Architecture:** Keep ResourceManager, share each pending asset by key/location, and track each label batch's own handle. Adapt the existing coroutine and callbacks to the same engine.
**Tech Stack:** Unity 6000.5.0f1, Addressables 2.9.1, existing PlayMode test assembly.
**Spec:** docs/superpowers/specs/2026-10-28-resource-loading-design.md

## Global Constraints

- Preserve previous phase, user scene edits, existing branch and uncommitted changes.
- Keep the old LoadAllAsync two-argument API; add explicit completion overload.
- Do not change scene assets, settings, packages, gameplay rules, or MP terminology.
- Use a separate validation copy while the user's Unity Editor is open.

## Review Focus

- Clear during a shared pending load releases one acquired asset reference and each query reference.
- Failure is not cached and retries produce a usable asset.
- A callback may throw, clear resources or start another request.
- Empty/invalid labels finish exactly once and never indicate success.
- Cached type/location collisions must not silently return the wrong object.

### Task 1: Reproduce resource defects

**Files:** Assets/Tests/PlayMode/ResourceLoadingTests.cs; .utmp/ResourceLoadingValidation.
**Interfaces:** existing PreloadAsync and LoadAllAsync; new completion overload exercised by reflection.

- [x] Add tests for legacy duplicate requests and mixed API requests, clear-during-load, empty results, cache reuse and failure/retry.
- [x] Run the whole PlayMode suite in the validation copy; record expected failures.

### Task 2: Shared loading and ownership

**Files:** Assets/0.Scripts/Framework/Managers/Core/ResourceManager.cs.
**Interfaces:** LoadAllAsync<T>(string,Action<string,int,int>,Action<bool,string>), existing APIs unchanged.

- [x] Replace separate loading paths with one batch engine and per-key pending requests.
- [x] Complete failures and cancellation, transfer successful handles to cache, and isolate callbacks.
- [x] Run tests until all existing/new behaviors pass.

### Task 3: Review and verification

**Files:** docs/AI/CoreInitialization.md; resource loading design/plan; tests if review exposes gaps.

- [x] Request one independent read-only review, reproduce and resolve material findings.
- [x] Run full PlayMode verification, check scoped diff and unchanged user assets, document results.
- [x] Leave reviewable changes uncommitted, with no branch integration.

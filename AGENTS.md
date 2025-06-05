# AI Contribution Guide

## Purpose
This document outlines the expectations for AI agents contributing to the Data-Driven Script Engine. It describes the project layout and the conventions that must be followed when creating or modifying code and text scripts.

---

## Repository Overview
```
/Assets
  /Scripts
    /RuntimeScripts
      Program files here

README.md
AGENTS.md  ← this file
```
- **RuntimeScripts** contain the core C# logic.
- **ScriptFiles** store the text DSL files, each typically defining a single `[OnEvent]` block.
- **README.md** explains project setup and examples.
- **AGENTS.md** (this document) details how AI agents should interact with the repository.

---

## Key Agent Guidelines
1. **Do not rename existing files.** New script files belong in `/Assets/ScriptFiles/` and should follow `On<EventName>_<Description>.txt`.
2. **Use PascalCase event names**: `[OnEventName]`. Lowercase or other styles are not allowed.
3. **Follow the DSL syntax**:
   - Conditions such as `<Function> <Comparator> <Number>` and boolean expressions with `&&`, `||`, `!`.
   - Actions like `ActionName(arg1, arg2) [canExecute=...] [interval=...|intervalFunc=...] [period=...]`.
   - Options use `key=value` with spaces between them.
4. **Avoid large refactors** of `TextScriptParser`, `ConditionEvaluator`, and `ScheduledAction`. Add new methods instead of restructuring.
5. **Keep the folder layout and `namespace RuntimeScripting` intact.**
6. **Consider performance**: minimize expensive operations in per-frame code paths.
7. **Handle errors gracefully** using `Debug.LogWarning` or `Debug.LogError`, skipping invalid lines rather than crashing.
8. **Follow Microsoft C# style** (PascalCase classes/methods, camelCase variables, 4-space indentation) and include XML doc comments on public APIs.

---

## DSL Rules
### Event Blocks
A script begins with `[OnEventName]`. While multiple blocks can appear in one file, prefer one per file for clarity.

### Conditions and Commands
```
<Condition> : <ActionCall> [options]
```
- Conditions may combine function calls with comparisons and logical operators.
- `:` separates the condition from the action.
- Actions use `ActionName(args...)` and optional parameters (`canExecute`, `interval`, `intervalFunc`, `period`).
- All functions map to static methods in `GameLogic` and must return `int` or `bool`.

### Execution Timing
- Without an `interval` or `intervalFunc`, an action runs once.
- `interval=<number>` repeats at a fixed rate.
- `intervalFunc=<Function>` queries the next wait time each cycle.
- Use `period=<number>` to stop after that many seconds of cumulative time.

---

## Coding and Documentation Notes
- **Namespace**: all runtime scripts are in `RuntimeScripting`.
- **Comments**: XML documentation on public members; inline comments for complex logic.
- **DSL files** should be UTF-8 with LF line endings. Lines starting with `#` are comments.
- **Testing**: maintain a `TEST_CASES.md` for scenarios. Unit tests should cover the parser, evaluator, and scheduler.

---

## Debugging and Future Improvements
- Add debug logs for skipped lines or evaluation issues.
- Provide a method to hot reload scripts at runtime.
- Potential enhancements include editor tools, validation utilities, visualization of event flows, and support for additional script formats.

---

## Appendix: File Descriptions
- **ParsedAction.cs** – stores data parsed from one line of script.
- **ParsedEvent.cs** – maps an event name to its actions.
- **TextScriptParser.cs** – reads script files and produces parsed events and actions.
- **ConditionEvaluator.cs** – evaluates boolean expressions in conditions.
- **ScheduledAction.cs** – handles repeating actions and execution timing.
- **RuntimeTextScriptController.cs** – MonoBehaviour that loads scripts, triggers events, and schedules actions.
- **GameLogic.cs** – contains the gameplay methods used by actions and conditions.

Adhering to these guidelines ensures consistency and maintainability across all contributions to the project.

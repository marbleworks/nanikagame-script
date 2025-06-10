# AI Contribution Guide

## Purpose
This document outlines the expectations for AI agents contributing to the Data-Driven Script Engine. It describes the project layout, conventions, and the DSL syntax (including new modifiers) that must be followed when creating or modifying code or text scripts.

---

## Repository Overview
```
/Assets
  /Resources
    /ScriptFiles
  /Scripts
    /RuntimeScripts
      Program files here

README.md
AGENTS.md  ← this file
```
- **RuntimeScripts** contain the core C# logic (namespace `RuntimeScripting`).
- **ScriptFiles** under `/Assets/Resources/ScriptFiles` store the text DSL files, each typically defining a single `[OnEventName]` block.
- **README.md** explains project setup and examples.
- **AGENTS.md** (this document) details how AI agents should interact with the repository.

---

## Key Agent Guidelines

1. **Event Names**  
   - Use **PascalCase** for event names. Always declare an event as `[OnEventName]`.  
   - Lowercase or other styles (e.g., `[onevent]`, `[On_event]`) are **not allowed**.

2. **DSL Syntax**  
   - The DSL is structured as:
     ```
     <EventBlock>
     [OnEventName]
     if (<Condition>) {
         act { <ActionList> } mod { <ModifierList> };
         ...
     } else if (<Condition>) {
         ...
     } else {
         ...
     }
     ```
   - **Conditions** may combine function calls with comparisons and logical operators:
     - Comparators: `<=, >=, <, >, ==, !=`
     - Boolean operators: `&&, ||, !`
     - Example: `HpMin() <= 100 && ResourceCount("sun") >= 5`
   - **Actions** use the form `ActionName(arg1, arg2, ...)`. Multiple actions can be comma-separated inside an `act { … }` block.
   - **Modifiers** appear immediately after an `act` block as `mod { key1=value1, key2=value2, … }`.  
     - Supported keys now include:
       - `interval` (number): fixed interval in seconds between executions.
       - `period` (number): stop after cumulative execution time reaches this many seconds.
       - `canExecute` (Expr): run only if this boolean expression is true; if false, skip but retry next interval.
       - **`maxCount` (integer)**: run at most this many successful executions; then stop permanently.
       - **`while` (Expr)**: continue only while this boolean condition is true; once false, cancel the task immediately.
     - All `key=value` pairs allow spaces around `=` (e.g., `interval = 2`, `while=HpMin() <= 100`).
     - At least one of `interval` or `period` is required if any repeating behavior is intended. If neither is specified and no modifiers are given, the action runs exactly once.
   - **Invocation mapping**: Every function used in `<Condition>` or `<ActionList>` must have a corresponding static method in `GameLogic` that returns `int` or `bool`.

3. **Avoid Large Refactors**  
   - Do **not** restructure core parsers (`TextScriptParser`, `ConditionEvaluator`, `ScheduledAction`).  
   - Instead, **add** new methods or extend existing code paths to support new DSL features.  
   - Keep the folder layout and `namespace RuntimeScripting` intact.

4. **Performance Considerations**  
   - Minimize expensive operations in per-frame or timer-driven code paths (e.g., avoid heavy allocations inside `Update()` loops).  
   - Use caching where appropriate (e.g., parse each DSL file only once and reuse the AST).

5. **Error Handling**  
   - Handle errors gracefully. Use `Debug.LogWarning` for non-critical skips (e.g., an invalid line in a DSL file) and `Debug.LogError` for serious failures (e.g., parser exceptions).  
   - Skipped or invalid lines should not crash the game—log a warning and continue parsing the next line.

6. **Coding Style**  
   - Follow Microsoft C# conventions:
     - Classes and methods in **PascalCase**.
     - Local variables in **camelCase**.
     - Four-space indentation (no tabs).
   - Add XML doc comments (`/// <summary>…</summary>`) to all public APIs.
   - Keep complex logic commented inline for maintainability.

---

## DSL Rules

### 1. Event Blocks
- A script file may contain one or more event blocks. Prefer one block per file:
  ```
  [OnEventName]
  if (<Condition1>) {
      act { … } mod { … };
      …
  } else if (<Condition2>) {
      …
  } else {
      …
  }
  ```
- Always start a block with `[OnEventName]`, where `OnEventName` is **PascalCase** (e.g., `[OnSpawned]`, `[OnUpdate]`, `[OnPlayerEnter]`).

### 2. Conditions
- Conditions appear inside `if (…)`, `else if (…)` clauses.
- `<Condition>` may use:
  - Function calls: `HpMin()`, `ResourceCount("sun")`, `EnemyExists("boss")`, etc.
  - Comparison operators: `<=, >=, <, >, ==, !=`.
  - Logical operators: `&&, ||, !`.
  - Parentheses for grouping: `( … )`.
- Example: `if (HpMin() <= 100 && ResourceCount("sun") >= 5)`

### 3. Action Lists
- Use `act { <Action1>, <Action2>, … }` to group one or more actions:
  ```
  act {
      Attack(1),
      AddPlayerEffect("strength", 1)
  }
  ```
- Each `<Action>` is a function call following C#-style syntax: `FunctionName(arg1, arg2, …)`.
- Example actions:
  - `Attack(1)`
  - `AddHp("@l", 5)`
  - `AddPlayerEffect("shield", 3)`
  - `TakeDamage(10)`
- If only one action is required, you may still use the same syntax:
  ```
  act { TakeDamage(10) }
  ```

### 4. Modifiers (`mod { … }`)
- The `mod` block follows immediately after `act { … }` and ends with a semicolon `;`.
- Example:
  ```
  act {
      Attack(1)
  } mod {
      interval=2,
      while=HpMin() <= 100,
      canExecute=UseResource("sun", 1),
      maxCount=5
  };
  ```

#### 4.1. Supported Keys

1. **`interval` (number)**  
   - Fixed time in seconds between each execution cycle.
   - Example: `interval=2` → run every 2 seconds.

2. **`period` (number)**  
   - Stop repeating after this many seconds of cumulative execution time.  
   - Mutually exclusive with `interval`, but can be combined if needed for advanced scheduling.

3. **`canExecute` (Expr)**  
   - A boolean expression checked each cycle.
   - If false, skip execution for that cycle but do not cancel the task.  
   - If true, perform the action(s) and (if applicable) decrement `maxCount`.

4. **`maxCount` (integer)**  
   - Run the action only this many successful times.  
   - Each time the action actually executes (i.e., after `canExecute` is true and action runs), decrement an internal counter.  
   - When the counter reaches zero, cancel the task permanently.  
   - **Value must be a positive integer**; otherwise, parser error.

5. **`while` (Expr)**  
   - A boolean expression re-evaluated before each execution cycle.  
   - If false on evaluation, cancel the task immediately (no further executions).  
   - If true, proceed to check `canExecute` (if present), then run action.  
   - Useful for “run until this condition becomes false.”  
   - Example: `while=HpMin() <= 100`.

#### 4.2. Key Precedence & Order of Evaluation

1. **`while` check (if present)**  
   - Evaluate `<Expr>` in `while`.  
   - If false → cancel task and do not run action.  
   - If true → proceed.

2. **`canExecute` check (if present)**  
   - Evaluate `<Expr>` in `canExecute`.  
   - If false → skip running the action for this cycle; do not decrement `maxCount`; schedule next cycle.  
   - If true → run action(s).

3. **Action execution**  
   - Invoke each function in `<ActionList>` in sequence.

4. **`maxCount` decrement (if present)**  
   - After successful action execution, decrement counter.  
   - If counter reaches zero → cancel task; otherwise → schedule next cycle after `interval` seconds (or according to `period`).

5. **Scheduling next cycle**  
   - Use `interval` (if present) or `period` to compute next run time.  
   - If neither is specified and no modifiers remain, the action runs exactly once and finishes.

#### 4.3. Examples

- **Single Execution (no modifiers)**  
  ```txt
  [OnSpawned]
  act { AddHp("@l", 10) };
  ```
  - Runs once immediately (register-and-run behavior) and does not repeat.

- **Fixed Repetition Until Condition Fails**  
  ```txt
  [OnSpawned]
  act { Attack(1) } mod { interval=2, while=HpMin() <= 100 };
  ```
  - Every 2 seconds, check `HpMin() <= 100`; if true, execute `Attack(1)`; if false, cancel.

- **Conditional Repetition with Count Limit**  
  ```txt
  [OnSpawned]
  act { AddHp("@l", 3) } mod {
      interval=1,
      canExecute=ResourceCount("sun") >= 1,
      maxCount=10
  };
  ```
  - Every 1 second, check `ResourceCount("sun") >= 1`; if false, skip; if true, run `AddHp` and decrement count.  
  - When count reaches zero, cancel.

- **Combined Example**  
  ```txt
  [OnSpawned]
  act { Attack(2) } mod {
      interval=3,
      while=ResourceCount("sun") >= 1,
      maxCount=4
  };
  ```
  - Check `ResourceCount("sun") >= 1` every 3 seconds; if true, execute `Attack(2)` and decrement counter.  
  - If counter hits zero or `ResourceCount("sun") >= 1` becomes false, cancel.

---

## Coding and Documentation Notes

- **Namespace**  
  - All runtime scripts reside in `namespace RuntimeScripting`.
- **Public APIs**  
  - Use XML documentation comments for all public classes, methods, and properties.
  - Inline comments are encouraged for complex logic, especially inside parser or scheduler code.
- **DSL File Requirements**  
  - Must be saved as UTF-8 with LF (`\n`) line endings.  
  - Lines beginning with `#` are treated as comments and ignored by the parser.

---

## Appendix: File Descriptions

- **ParsedAction.cs** – stores data parsed from one line of script.
- **ParsedEvent.cs** – maps an event name to its actions.
- **TextScriptParser.cs** – reads script files and produces parsed events and actions.
- **ConditionEvaluator.cs** – evaluates boolean expressions in conditions.
- **ScheduledAction.cs** – handles repeating actions and execution timing.
- **RuntimeTextScriptController.cs** – MonoBehaviour that loads scripts, triggers events, and schedules actions.
- **GameLogic.cs** – contains the gameplay methods used by actions and conditions.
- **ExpressionEvaluator.cs** – parses arithmetic expressions for integer and float values.
- **FunctionEnums.cs** – lists GameLogic functions available to scripts.

---

Adhering to these guidelines ensures consistency and maintainability across all contributions to the project.

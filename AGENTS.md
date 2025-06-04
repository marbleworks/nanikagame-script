# AGENTS.md

## Overview
This file defines the guidelines and constraints for AI agents (e.g., ChatGPT) working on the Data-Driven Script Engine project. It explains how to interact with the repository, adhere to its conventions, and contribute new functionality without violating existing structures. 

---

## 1. Repository Structure and File Roles

```
/Assets
  /Scripts
    /RuntimeScripts
      ParsedAction.cs
      ParsedEvent.cs
      TextScriptParser.cs
      RuntimeTextScriptController.cs
      ScheduledAction.cs
      ConditionEvaluator.cs
      GameLogic.cs
  /ScriptFiles
    OnSpawned_Attack5.interval9.txt
    OnSpawned_AddHp2.interval1.txt
    OnEvolutionSummoned_AddStrength1.txt
    OnSpawned_AddSpikes1.interval3.txt
    OnEvolutionSummoned_AddSpeed1_AddStrength1.txt
    OnEvolutionSummoned_RemoveDebuff20.txt
    OnSpawned_Attack50.ifUseSun6.interval3.txt
    OnSpawned_AttackDynamicCherry.interval2.txt
    OnSpawned_SetVolt1.interval1.txt
    OnCombo_AttackComboCount.txt
    OnCombo_IfCombo10_Invuln30.txt
    OnStartRound_AddMaxHp20.txt
    OnHpChanged_IfHpMin100_Strength10.txt

README.md
AGENTS.md       ← (this file)
```

- **`/Assets/Scripts/RuntimeScripts/`**  
  Contains the core C# components: the parser, condition evaluator, scheduler, and stubbed game logic. 

- **`/Assets/ScriptFiles/`**  
  Holds the text-based script files. Each file defines a single `On<EventName>` event block using the project’s DSL.

- **`README.md`**  
  Provides project overview, setup instructions, and basic usage examples.

- **`AGENTS.md`**  
  (This file) Details the rules and constraints that AI agents must follow when generating, modifying, or reviewing code and scripts in this repository.

---

## 2. AI Agent Roles and Constraints

### 2.1 Purpose
- When asked to modify code or add features, the AI agent must adhere to existing project conventions: DSL syntax, naming, folder structure, and coding style.
- When generating or editing script files in the DSL, the agent must use the prescribed format (PascalCase event names, composite conditions, `intervalFunc`/`interval`/`canExecute`, etc.).

### 2.2 Agent Guidelines
1. **Do Not Rename Existing Files**  
   - Existing script files in `/Assets/ScriptFiles/` must remain unchanged. If adding new scripts, follow the naming convention: `On<EventName>_<Description>.txt`.

2. **Always Use PascalCase for Event Names**  
   - Event headings must be written as `[OnEventName]`. Do not use lowercase, snake_case, or any other naming style.

3. **Respect the DSL Syntax**  
   - Conditions: `<FunctionCall> <Comparator> <Integer>` or composite logical expressions (`&&`, `||`, `!`, parentheses).  
   - Actions: `ActionName(arg1, arg2, ...) [canExecute=...] [interval=...|intervalFunc=...] [period=...]`.  
   - Options are separated by spaces, and the key and value must be joined by `=`.  
   - Do not introduce unsupported keywords or change existing keywords.

4. **Avoid Large-Scale Modifications to C# Code**  
   - Core classes like `TextScriptParser`, `ConditionEvaluator`, and `ScheduledAction` should not be heavily refactored.  
   - If new logic is required, add new methods or classes rather than restructuring existing ones.

5. **Maintain the Namespace and Folder Layout**  
   - Do not move files out of `RuntimeScripts` or change the `namespace RuntimeScripting`.  
   - Keep the directory hierarchy unchanged unless explicitly instructed otherwise.

6. **Performance Considerations**  
   - Every call to `ConditionEvaluator` or `EvaluateInterval` may occur each frame for multiple scheduled actions. Avoid adding expensive string operations or regular expressions that run every frame.  
   - If extending DSL parsing or evaluation logic, strive to cache results or use compiled patterns to minimize runtime overhead.

7. **Error Handling**  
   - Parsing or evaluation errors must be caught and logged via `Debug.LogWarning` or `Debug.LogError` without crashing the game.  
   - Skip invalid lines in script files rather than aborting the entire load process.

8. **Adhere to Existing Coding Style**  
   - Follow Microsoft’s C# style guidelines: PascalCase for class/method names, camelCase for local variables, 4-space indentation, etc.  
   - Public methods and classes should have XML documentation comments.  
   - Use consistent formatting when adding or editing code.

---

## 3. DSL (Text Script) Rules

### 3.1 Event Block Definition
- Each script file must start with a block header in the format:  
  ```
  [OnEventName]
  ```
  where `EventName` is PascalCase (e.g., `OnSpawned`, `OnCombo`, `OnHpChanged`, `OnStartRound`).
- Although multiple event blocks can appear in a single file, it is **strongly recommended** that each file define exactly one event block for clarity and maintainability.

### 3.2 Condition and Command Syntax
```
<Condition> : <ActionCall> [options…]
```
- **`<Condition>`**  
  - A comparison expression: `<FunctionCall> <Comparator> <Integer>`, where `<Comparator>` is one of `<=`, `>=`, `<`, `>`, `==`.  
  - Composite logical expressions using `&&`, `||`, `!`, and parentheses.  
  - Examples:  
    ```
    HpMin() <= 100 && ComboCount() >= 5
    (HpMin() <= 50 || Shield() >= 20) && NotDebuffed(@l)
    ```

- **Colon Separator**  
  - The first `:` encountered splits the condition from the action. Whitespace around `:` is allowed.

- **`<ActionCall>`**  
  - Must be in the form `ActionName(arg1, arg2, …)`. Arguments are comma-separated.  
  - String literals do not require quotation marks (e.g., `@l, strength, 10`).  
  - Examples:  
    ```
    Attack(5)
    AddPlayerEffect(@l, strength, 1)
    SetNanikaEffectFor(#f, volt, 1)
    ```

- **`[options…]`** (order is flexible)  
  - `canExecute=<FunctionCall>`  
  - `interval=<Number>` or `intervalFunc=<FunctionCall>`  
  - `period=<Number>`  
  - Examples:  
    ```
    Attack(50) canExecute=UseResource(sun,6) interval=3
    AddHp(@l, 2) intervalFunc=interval(1) period=10
    ```

### 3.3 Function Call Rules
- **No-Argument Functions:**  
  - `HpMin()`, `ComboCount()`, `Shield()`, etc.

- **Functions with String Arguments:**  
  - `NanikaCount("#a[id=cherry]")`, `NotDebuffed(@l)`, `UseResource(sun,6)`, etc.

- At runtime, all function calls refer to static methods in `GameLogic`. Return values must be `int` or `bool`.

### 3.4 One-Shot vs. Periodic Execution
- **One-Shot Execution**  
  - If no `interval` or `intervalFunc` is specified, the action runs immediately once.

- **Periodic Execution**  
  - `interval=<Number>`: Fixed interval in seconds, repeats indefinitely.  
  - `intervalFunc=<FunctionCall>`: Each cycle, call the function to get the next wait time.  
  - `period=<Number>`: Cumulative time limit. When total elapsed time exceeds this, the scheduled action stops.  
    - Example: `interval=3 period=12` → Executes every 3 seconds for up to 12 seconds, then stops.

---

## 4. Coding and Documentation Guidelines

### 4.1 C# Code Style
- **Naming Conventions**  
  - Classes, methods, and public properties: PascalCase.  
  - Local variables and private fields: camelCase.  
  - Constants: UPPER_SNAKE_CASE.

- **Namespace**  
  - All runtime scripts reside under `namespace RuntimeScripting`.

- **Comments**  
  - Provide XML documentation comments on public classes and methods.  
  - Add inline comments to explain complex logic or regular expressions.

- **Error Handling**  
  - Use `try/catch(Exception ex)` around parsing and evaluation logic.  
  - Log errors with `Debug.LogError` or `Debug.LogWarning` and gracefully skip the faulty part.

### 4.2 DSL File (TextAsset) Style
- **Encoding**  
  - UTF-8 without BOM.  
  - Use LF line endings consistently.

- **Comments**  
  - Lines starting with `#` are treated as comments and ignored by the parser.  
  - If including original `def ...:` code as comments, ensure the entire line begins with `#`.

- **Blank Lines**  
  - Blank lines are ignored. They may be used to improve readability.

- **File Naming**  
  - Use `<EventName>_<Description>.txt`, e.g., `OnHpChanged_IfHpMin100_Strength10.txt`.

### 4.3 Documentation Conventions
- **README.md**  
  - Include setup instructions (how to import into Unity and attach `RuntimeTextScriptController`).  
  - Provide basic DSL format examples and a list of common `GameLogic` functions.

- **AGENTS.md**  
  - (This file) Guidelines for AI agents working in this repository.

- **Additional Docs**  
  - If needed, create a separate `DSL_REFERENCE.md` to document the DSL grammar and examples.  
  - Maintain a `TEST_CASES.md` listing unit and integration test scenarios.

---

## 5. Testing and Debugging Recommendations

### 5.1 Unit Tests
- **TextScriptParser**  
  - Verify correct parsing of simple lines, lines with single conditions, and lines with composite conditions.  
  - Test function and argument extraction, and validate that `ParsedAction` properties match the expected values.

- **ConditionEvaluator**  
  - Ensure it correctly evaluates expressions such as `HpMin() <= 100 && ComboCount() >= 5`, `(HpMin() <= 50 || Shield() >= 20) && NotDebuffed(@l)`, and unary NOT.  
  - Verify function calls return correct integer or boolean values.

- **ScheduledAction**  
  - Test that `intervalFunc` and `interval` produce correct next execution times.  
  - If `canExecute=UseResource(sun,6)`, mock `GameLogic.UseResource` returning `true` or `false` and confirm that the action executes or skips appropriately.  
  - For `period=10`, ensure that after 10 seconds of cumulative elapsed time the action stops.

### 5.2 Integration Tests
- **OnHpChanged Integration**  
  1. Mock `GameLogic.HpMin()` to return `90` and `GameLogic.ComboCount()` to return `6`.  
  2. Load a script file containing `HpMin() <= 100 && ComboCount() >= 5: Attack(10) interval=2`.  
  3. Trigger `Trigger_OnHpChanged(90)` and verify that `Attack(10)` is called every 2 seconds.

- **Merging Multiple Files**  
  - If two files each define `[OnSpawned]` with different actions, verify that both actions run in the intended order (based on file load order).

### 5.3 Debugging Tips
- **Verbose Logging**  
  - Add debug logs in `TextScriptParser` to print warnings for skipped lines or unknown actions.  
  - In `ConditionEvaluator`, log any tokenization or parsing errors with context to help locate malformed conditions.  
  - In `ScheduledAction`, log each execution time and action for tracing.

- **Hot Reloading**  
  - Implement a `ReloadScripts()` method in `RuntimeTextScriptController` to re-parse script files at runtime. This allows quick iteration without restarting the game.

---

## 6. Future Enhancements

1. **DSL Editor Extension**  
   - Create a Unity Custom Inspector or standalone editor window to visualize and edit script blocks. Include dropdowns for event names, function names, and argument lists to reduce typos.

2. **Refactoring Assistance**  
   - Build a validation tool or CI check that ensures script references match actual `ActionType` and `GameLogic` method names.  
   - Integrate DSL grammar tests into the build pipeline to catch errors early.

3. **Script Visualization**  
   - Provide a graph or flowchart view in the Unity Editor showing event-to-action relationships, conditions, and scheduling details.

4. **Support Multiple Script Formats**  
   - Abstract the parser behind an interface (e.g., `IScriptParser`) to allow JSON, YAML, or Lua-based script definitions in the future.  
   - Maintain compatibility by mapping new formats to the existing `ParsedAction` and `ParsedEvent` structures.

---

## 7. Appendix: Key Files and Descriptions

- **ParsedAction.cs**  
  Holds one line’s parsed data: `ActionType`, `Args[]`, `Interval`, `Period`, `Condition`, `CanExecuteRaw`, `IntervalFuncRaw`.

- **ParsedEvent.cs**  
  Maps an event name (e.g., `OnSpawned`) to a list of its `ParsedAction` instances.

- **TextScriptParser.cs**  
  Parses script files line by line, extracts conditions and commands, and creates `ParsedAction` and `ParsedEvent` objects.

- **ConditionEvaluator.cs**  
  Evaluates composite boolean conditions. Implements tokenization and a recursive-descent parser that supports comparison and logical operators, parentheses, and function calls via `GameLogic`.

- **ScheduledAction.cs**  
  Manages periodic execution of actions. Calculates next execution time based on `interval` or `intervalFunc`, checks `canExecute`, and enforces `period` limits.

- **RuntimeTextScriptController.cs**  
  A Unity `MonoBehaviour` that loads all scripts at Awake, listens for external calls like `Trigger_OnSpawned()`, evaluates conditions, converts `ParsedAction` to `ActionParameter`, and either executes one-shot actions or schedules repeating actions.

- **GameLogic.cs**  
  Contains stubbed or real game logic methods—damage, buffs, resource checks, counters, etc. These are invoked by `ExecuteActionImmediately` and by condition evaluator for function calls.

---

This concludes the guidelines for AI agents working on the Data-Driven Script Engine project. Adhering to these rules ensures consistency, maintainability, and performance across all contributions.

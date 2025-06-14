# Runtime Scripting

## Script Loading Modes

 `RuntimeTextScriptController` supports configurable merge behavior when loading scripts.

```
controller.Load("Scripts/Foo", ScriptLoadMode.Overwrite);
```

Available modes:

- **FullReplace** – clear all existing events before loading new ones.
- **Overwrite** – replace events with the same name but keep others intact.
- **Append** – add new actions to existing events when names match.

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
  - `AddPlayerEffect("strength", 3)`
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
      interval = 2,
      while = HpMin() <= 100,
      canExecute=UseResource("sun", 1),
      maxCount = 5
  };
  ```

#### 4.1. Supported Keys

1. **`interval` (number)**  
   - Fixed time in seconds between each execution cycle.
   - Example: `interval = 2` → run every 2 seconds.

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
   - Example: `while = HpMin() <= 100`.

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
  act { Attack(1) } mod { interval = 2, while = HpMin() <= 100 };
  ```
  - Every 2 seconds, check `HpMin() <= 100`; if true, execute `Attack(1)`; if false, cancel.

- **Conditional Repetition with Count Limit**  
  ```txt
  [OnSpawned]
  act { AddHp("@l", 3) } mod {
      interval = 1,
      canExecute=ResourceCount("sun") >= 1,
      maxCount = 10
  };
  ```
  - Every 1 second, check `ResourceCount("sun") >= 1`; if false, skip; if true, run `AddHp` and decrement count.  
  - When count reaches zero, cancel.

- **Combined Example**  
  ```txt
  [OnSpawned]
  act { Attack(2) } mod {
      interval = 3,
      while = ResourceCount("sun") >= 1,
      maxCount = 4
  };
  ```
  - Check `ResourceCount("sun") >= 1` every 3 seconds; if true, execute `Attack(2)` and decrement counter.  
  - If counter hits zero or `ResourceCount("sun") >= 1` becomes false, cancel.

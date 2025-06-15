# AI Contribution Guide

Please do not modify README.md. Instead, record your changes here in the chat.

## Purpose
This document outlines the expectations for AI agents contributing to the Data-Driven Script Engine. It describes the project layout, conventions, and the DSL syntax (including new modifiers) that must be followed when creating or modifying code or text scripts.

---

## Repository Overview
```
/Assets
  /Scripts
    /RuntimeScripting
      Program files here

README.md
AGENTS.md  ← this file
```
- **RuntimeScripting** contain the core C# logic (namespace `RuntimeScripting`).
- **README.md** explains project setup and examples.
- **AGENTS.md** (this document) details how AI agents should interact with the repository.

---

## Key Agent Guidelines

1. **Performance Considerations**  
   - Minimize expensive operations in per-frame or timer-driven code paths (e.g., avoid heavy allocations inside `Update()` loops).  
   - Use caching where appropriate (e.g., parse each DSL file only once and reuse the AST).

2. **Error Handling**  
   - Handle errors gracefully. Use `Debug.LogWarning` for non-critical skips (e.g., an invalid line in a DSL file) and `Debug.LogError` for serious failures (e.g., parser exceptions).  
   - Skipped or invalid lines should not crash the game—log a warning and continue parsing the next line.

3. **Coding Style**  
   - Follow Microsoft C# conventions.
   - Add XML doc comments (`/// <summary>…</summary>`) to all public APIs.
   - Keep complex logic commented inline for maintainability.

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

- **ActionParameter.cs** – Simple class storing an action’s name and argument list, used when invoking actions.
- **ActionParser.cs** – Parses act { … } mod { … }; syntax into ParsedAction objects, handling action lists and modifiers.
- **ActionTokenizer.cs** – Tokenizer for action and modifier text, producing tokens like identifiers, numbers, strings and punctuation.
- **ConditionEvaluator.cs** – Static helper that evaluates boolean expressions by invoking the ConditionParser; returns true on success, false on errors.
- **ConditionParser.cs** – Recursive‑descent parser for boolean expressions, supporting arithmetic, comparison and logical operators. Parses tokens from ConditionTokenizer.
- **ConditionStack.cs** – Utility for combining nested conditions while parsing; stores active conditions and applies them to actions.
- **ConditionTokenizer.cs** – Tokenizer specialized for boolean condition syntax (&&, ||, comparison ops, etc.).
- **ExpressionEvaluator.cs** – Evaluates arithmetic expressions to integers or floats via a generic ExpressionParser. Returns zero on errors.
- **ExpressionParser.cs** – Generic recursive‑descent parser for arithmetic expressions, parameterized by numeric type; handles operators and function calls.
- **ExpressionTokenizer.cs** – Tokenizer for arithmetic expressions, producing tokens for numbers, identifiers, operators, parentheses and strings.
- **GameLogic.cs** – Placeholder class containing sample game functions and action execution logic. Provides ExecuteAction, numeric functions, and condition evaluation.
- **IGameLogic.cs** – Interface defining how scripting interacts with game logic: executing parsed actions and evaluating conditions or functions.
- **ParsedAction.cs** – Data model for a single script action, including timing, conditions, and repetition parameters like interval, period, while and maxCount.
- **ParsedEvent.cs** – Container mapping an event name to a list of ParsedAction objects.
- **ScriptController.cs** – Unity MonoBehaviour that executes parsed events and manages ScheduledAction coroutines.
- **ScriptLoader.cs** – Utility for loading script files or text, storing parsed events, and triggering them through a registered ScriptController.
- **ScheduledAction.cs** – Executes actions periodically without drift, honoring modifiers such as interval, period, while, maxCount, and canExecute.
- **ScriptLoadMode.cs** – Enumeration controlling how newly loaded scripts merge with existing ones: FullReplace, Overwrite, or Append.
- **ScriptTokenizer.cs** – Low-level tokenizer for the script DSL, providing methods to read tokens, peek ahead, and handle comments and whitespace. Recognizes keywords like act, mod, if and else.
- **TextScriptParser.cs** – Main parser that converts script text into events and actions using ScriptTokenizer, ActionParser, and ConditionStack for nested condition handling.
- **TokenizerBase.cs** – Abstract base class shared by all tokenizers, offering utilities for scanning text, reading numbers or strings, and checking identifiers.

---

Adhering to these guidelines ensures consistency and maintainability across all contributions to the project.

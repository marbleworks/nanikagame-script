using System;
using System.Collections.Generic;
using System.Text;

namespace RuntimeScripting
{
    /// <summary>
    /// Parses the block-style DSL with [Event] sections, if/else clauses and act/mod statements.
    /// </summary>
    public static class NewTextScriptParser
    {
        /// <summary>
        /// Parses a script string written in the new DSL format.
        /// </summary>
        /// <param name="script">Full script text.</param>
        /// <returns>Dictionary mapping event names to parsed events.</returns>
        public static Dictionary<string, ParsedEvent> ParseString(string script)
        {
            var parser = new Parser(script);
            return parser.Parse();
        }

        private class Parser
        {
            private readonly string text;
            private int index;
            private readonly Dictionary<string, ParsedEvent> events = new Dictionary<string, ParsedEvent>();
            private readonly Stack<string> conditionStack = new Stack<string>();
            private string currentEvent;

            public Parser(string text)
            {
                this.text = text;
            }

            public Dictionary<string, ParsedEvent> Parse()
            {
                while (SkipWhite())
                {
                    if (Peek() == '[')
                    {
                        ParseEventSection();
                    }
                    else
                    {
                        // ignore unexpected characters
                        index++;
                    }
                }

                return events;
            }

            private void ParseEventSection()
            {
                Expect('[');
                var name = ReadUntil(']');
                Expect(']');
                currentEvent = name.Trim();
                if (!events.TryGetValue(currentEvent, out var pe))
                {
                    pe = new ParsedEvent { EventName = currentEvent };
                    events.Add(currentEvent, pe);
                }
                SkipLine();
                while (SkipWhite())
                {
                    if (StartsWith("["))
                        break;
                    if (StartsWith("if"))
                    {
                        ParseIfStatement(pe);
                    }
                    else if (StartsWith("act"))
                    {
                        var actions = ParseActStatement();
                        foreach (var pa in actions)
                        {
                            ApplyCondition(pa);
                            pe.Actions.Add(pa);
                        }
                    }
                    else
                    {
                        // unknown or empty line
                        SkipLine();
                    }
                }
            }

            private void ParseIfStatement(ParsedEvent pe)
            {
                ExpectString("if");
                SkipWhite();
                Expect('(');
                var expr = ReadEnclosed('(', ')');
                Expect(')');
                SkipWhite();
                Expect('{');
                conditionStack.Push(expr.Trim());
                SkipLine();
                while (SkipWhite() && !StartsWith("}"))
                {
                    if (StartsWith("if"))
                    {
                        ParseIfStatement(pe);
                    }
                    else if (StartsWith("act"))
                    {
                        var actions = ParseActStatement();
                        foreach (var pa in actions)
                        {
                            ApplyCondition(pa);
                            pe.Actions.Add(pa);
                        }
                    }
                    else
                    {
                        SkipLine();
                    }
                }
                Expect('}');
                conditionStack.Pop();
                SkipWhite();
                // else or else if
                if (StartsWith("else"))
                {
                    ExpectString("else");
                    SkipWhite();
                    string cond = null;
                    if (StartsWith("if"))
                    {
                        ExpectString("if");
                        SkipWhite();
                        Expect('(');
                        cond = ReadEnclosed('(', ')');
                        Expect(')');
                        conditionStack.Push($"!({expr.Trim()}) && ({cond.Trim()})");
                    }
                    else
                    {
                        conditionStack.Push($"!({expr.Trim()})");
                    }
                    SkipWhite();
                    Expect('{');
                    SkipLine();
                    while (SkipWhite() && !StartsWith("}"))
                    {
                        if (StartsWith("if"))
                        {
                            ParseIfStatement(pe);
                        }
                        else if (StartsWith("act"))
                        {
                            var actions = ParseActStatement();
                            foreach (var pa in actions)
                            {
                                ApplyCondition(pa);
                                pe.Actions.Add(pa);
                            }
                        }
                        else
                        {
                            SkipLine();
                        }
                    }
                    Expect('}');
                    conditionStack.Pop();
                }
            }

            private List<ParsedAction> ParseActStatement()
            {
                ExpectString("act");
                SkipWhite();
                Expect('{');
                var actionsContent = ReadEnclosed('{', '}');
                Expect('}');
                var actions = ParseActionList(actionsContent);
                SkipWhite();
                Dictionary<string, string> mods = null;
                if (StartsWith("mod"))
                {
                    ExpectString("mod");
                    SkipWhite();
                    Expect('{');
                    var modContent = ReadEnclosed('{', '}');
                    Expect('}');
                    mods = ParseModifierList(modContent);
                }
                Expect(';');
                SkipLine();

                var list = new List<ParsedAction>();
                foreach (var act in actions)
                {
                    var pa = new ParsedAction
                    {
                        ActionType = ParseActionType(act.name)
                    };
                    pa.Args.AddRange(act.args);
                    if (mods != null)
                    {
                        if (mods.TryGetValue("interval", out var iv))
                        {
                            if (float.TryParse(iv, out var interval))
                                pa.Interval = interval;
                            else
                                pa.IntervalFuncRaw = iv;
                        }
                        if (mods.TryGetValue("period", out var pd))
                        {
                            if (float.TryParse(pd, out var period))
                                pa.Period = period;
                            else
                                pa.PeriodFuncRaw = pd;
                        }
                        if (mods.TryGetValue("canExecute", out var ce))
                            pa.CanExecuteRaw = ce;
                        if (mods.TryGetValue("intervalFunc", out var ivf))
                            pa.IntervalFuncRaw = ivf;
                    }
                    list.Add(pa);
                }
                return list;
            }

            private List<(string name, List<string> args)> ParseActionList(string text)
            {
                var list = new List<(string, List<string>)>();
                int depth = 0;
                int start = 0;
                for (int i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || (text[i] == ',' && depth == 0))
                    {
                        var part = text.Substring(start, i - start).Trim();
                        if (part.Length > 0)
                        {
                            list.Add(ParseActionExpr(part));
                        }
                        start = i + 1;
                    }
                    else if (text[i] == '(')
                        depth++;
                    else if (text[i] == ')')
                        depth--;
                }
                return list;
            }

            private (string name, List<string> args) ParseActionExpr(string text)
            {
                int open = text.IndexOf('(');
                string name = open >= 0 ? text.Substring(0, open).Trim() : text.Trim();
                var args = new List<string>();
                if (open >= 0)
                {
                    int close = text.LastIndexOf(')');
                    if (close > open)
                    {
                        var argContent = text.Substring(open + 1, close - open - 1);
                        args = ParseArgList(argContent);
                    }
                }
                return (name, args);
            }

            private List<string> ParseArgList(string text)
            {
                var list = new List<string>();
                int depth = 0;
                int start = 0;
                for (int i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || (text[i] == ',' && depth == 0))
                    {
                        var part = text.Substring(start, i - start).Trim();
                        if (part.Length > 0)
                            list.Add(part);
                        start = i + 1;
                    }
                    else if (text[i] == '(')
                        depth++;
                    else if (text[i] == ')')
                        depth--;
                }
                return list;
            }

            private Dictionary<string, string> ParseModifierList(string text)
            {
                var dict = new Dictionary<string, string>();
                int depth = 0;
                int start = 0;
                for (int i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || (text[i] == ',' && depth == 0))
                    {
                        var part = text.Substring(start, i - start).Trim();
                        if (part.Length > 0)
                        {
                            int eq = part.IndexOf('=');
                            if (eq > 0)
                            {
                                string key = part.Substring(0, eq).Trim();
                                string value = part.Substring(eq + 1).Trim();
                                dict[key] = value;
                            }
                        }
                        start = i + 1;
                    }
                    else if (text[i] == '(')
                        depth++;
                    else if (text[i] == ')')
                        depth--;
                }
                return dict;
            }

            private ActionType ParseActionType(string name)
            {
                return Enum.TryParse<ActionType>(name, out var at) ? at : ActionType.Attack;
            }

            private void ApplyCondition(ParsedAction pa)
            {
                if (conditionStack.Count == 0)
                    return;
                var sb = new StringBuilder();
                foreach (var cond in conditionStack)
                {
                    if (sb.Length > 0) sb.Insert(0, "(").Append(") && ");
                    sb.Insert(0, cond);
                }
                pa.Condition = sb.ToString();
            }

            // Helper reading utilities
            private char Peek()
            {
                if (index >= text.Length) return '\0';
                return text[index];
            }

            private bool StartsWith(string s)
            {
                SkipWhite();
                return string.Compare(text, index, s, 0, s.Length, StringComparison.Ordinal) == 0;
            }

            private bool SkipWhite()
            {
                bool moved = false;
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                    moved = true;
                }
                return index < text.Length;
            }

            private void SkipLine()
            {
                while (index < text.Length && text[index] != '\n') index++;
                if (index < text.Length) index++;
            }

            private void Expect(char c)
            {
                SkipWhite();
                if (index >= text.Length || text[index] != c)
                    throw new Exception($"Expected '{c}' at {index}");
                index++;
            }

            private void ExpectString(string s)
            {
                SkipWhite();
                for (int i = 0; i < s.Length; i++)
                {
                    if (index + i >= text.Length || text[index + i] != s[i])
                        throw new Exception($"Expected '{s}' at {index}");
                }
                index += s.Length;
            }

            private string ReadUntil(char c)
            {
                int start = index;
                while (index < text.Length && text[index] != c) index++;
                return text.Substring(start, index - start);
            }

            private string ReadEnclosed(char open, char close)
            {
                int depth = 1;
                int start = index;
                while (index < text.Length)
                {
                    char ch = text[index++];
                    if (ch == open)
                        depth++;
                    else if (ch == close)
                    {
                        depth--;
                        if (depth == 0)
                            break;
                    }
                }
                return text.Substring(start, index - start - 1);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RuntimeScripting
{
    /// <summary>
    /// Parses the block-style DSL with [Event] sections, if/else clauses and act/mod statements.
    /// </summary>
    public static class TextScriptParser
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
            private readonly string _text;
            private int _index;
            private readonly Dictionary<string, ParsedEvent> _events = new();
            private readonly Stack<string> _conditionStack = new();

            public Parser(string text)
            {
                _text = text;
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
                        _index++;
                    }
                }

                return _events;
            }

            private void ParseEventSection()
            {
                Expect('[');
                var name = ReadUntil(']').Trim();
                Expect(']');
                if (!_events.TryGetValue(name, out var pe))
                {
                    pe = new ParsedEvent {EventName = name};
                    _events.Add(name, pe);
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
                var accumulated = expr.Trim();
                _conditionStack.Push(accumulated);
                SkipWhite();
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
                _conditionStack.Pop();
                SkipWhite();

                // Handle any number of else-if clauses and a final else
                while (StartsWith("else"))
                {
                    ExpectString("else");
                    SkipWhite();
                    var hasElseIf = false;
                    if (StartsWith("if"))
                    {
                        hasElseIf = true;
                        ExpectString("if");
                        SkipWhite();
                        Expect('(');
                        var cond = ReadEnclosed('(', ')');
                        Expect(')');
                        var trimmed = cond.Trim();
                        _conditionStack.Push($"!({accumulated}) && ({trimmed})");
                        accumulated = $"({accumulated}) || ({trimmed})";
                    }
                    else
                    {
                        _conditionStack.Push($"!({accumulated})");
                    }

                    SkipWhite();
                    Expect('{');
                    SkipWhite();
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
                    _conditionStack.Pop();
                    SkipWhite();

                    if (!hasElseIf)
                        break;
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
                SkipWhite();

                var list = new List<ParsedAction>();
                foreach (var act in actions)
                {
                    var pa = new ParsedAction
                    {
                        FunctionName = act.name
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
                        if (mods.TryGetValue("maxCount", out var mc) && int.TryParse(mc, out var mcv))
                            pa.MaxCount = mcv;
                        if (mods.TryGetValue("while", out var wh))
                            pa.WhileRaw = wh;
                    }

                    list.Add(pa);
                }

                return list;
            }

            private static List<(string name, List<string> args)> ParseActionList(string text)
            {
                var list = new List<(string, List<string>)>();
                var depth = 0;
                var inString = false;
                var stringChar = '\0';
                var start = 0;
                for (var i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || (text[i] == ',' && depth == 0 && !inString))
                    {
                        var part = text.Substring(start, i - start).Trim();
                        if (part.Length > 0)
                        {
                            list.Add(ParseActionExpr(part));
                        }

                        start = i + 1;
                    }
                    else if (!inString && text[i] == '(')
                        depth++;
                    else if (!inString && text[i] == ')')
                        depth--;
                    else if (text[i] == '"' || text[i] == '\'')
                    {
                        if (inString && text[i] == stringChar)
                            inString = false;
                        else if (!inString)
                        {
                            inString = true;
                            stringChar = text[i];
                        }
                    }
                }

                return list;
            }

            private static (string name, List<string> args) ParseActionExpr(string text)
            {
                var open = text.IndexOf('(');
                var name = open >= 0 ? text[..open].Trim() : text.Trim();
                var args = new List<string>();
                if (open >= 0)
                {
                    var close = text.LastIndexOf(')');
                    if (close > open)
                    {
                        var argContent = text.Substring(open + 1, close - open - 1);
                        args = ParseArgList(argContent);
                    }
                }

                return (name, args);
            }

            private static List<string> ParseArgList(string text)
            {
                var list = new List<string>();
                var depth = 0;
                var inString = false;
                var stringChar = '\0';
                var start = 0;
                for (var i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || (text[i] == ',' && depth == 0 && !inString))
                    {
                        var part = text.Substring(start, i - start).Trim();
                        if (part.Length > 0)
                            list.Add(Unquote(part));
                        start = i + 1;
                    }
                    else if (!inString && text[i] == '(')
                        depth++;
                    else if (!inString && text[i] == ')')
                        depth--;
                    else if (text[i] == '"' || text[i] == '\'')
                    {
                        if (inString && text[i] == stringChar)
                            inString = false;
                        else if (!inString)
                        {
                            inString = true;
                            stringChar = text[i];
                        }
                    }
                }

                return list;
            }

            private static Dictionary<string, string> ParseModifierList(string text)
            {
                var dict = new Dictionary<string, string>();
                var depth = 0;
                var inString = false;
                var stringChar = '\0';
                var start = 0;
                for (var i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || (text[i] == ',' && depth == 0 && !inString))
                    {
                        var part = text.Substring(start, i - start).Trim();
                        if (part.Length > 0)
                        {
                            var eq = part.IndexOf('=');
                            if (eq > 0)
                            {
                                var key = part[..eq].Trim();
                                var value = part[(eq + 1)..].Trim();
                                dict[key] = Unquote(value);
                            }
                        }

                        start = i + 1;
                    }
                    else if (!inString && text[i] == '(')
                        depth++;
                    else if (!inString && text[i] == ')')
                        depth--;
                    else if (text[i] == '"' || text[i] == '\'')
                    {
                        if (inString && text[i] == stringChar)
                            inString = false;
                        else if (!inString)
                        {
                            inString = true;
                            stringChar = text[i];
                        }
                    }
                }

                return dict;
            }

            private void ApplyCondition(ParsedAction pa)
            {
                if (_conditionStack.Count == 0)
                    return;
                var sb = new StringBuilder();
                foreach (var cond in _conditionStack)
                {
                    if (sb.Length > 0) sb.Insert(0, "(").Append(") && ");
                    sb.Insert(0, cond);
                }

                pa.Condition = sb.ToString();
            }

            // Helper reading utilities
            private char Peek()
            {
                return _index >= _text.Length ? '\0' : _text[_index];
            }

            private bool StartsWith(string s)
            {
                SkipWhite();
                return string.Compare(_text, _index, s, 0, s.Length, StringComparison.Ordinal) == 0;
            }

            private bool SkipWhite()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                {
                    _index++;
                }

                return _index < _text.Length;
            }

            private void SkipLine()
            {
                while (_index < _text.Length && _text[_index] != '\n') _index++;
                if (_index < _text.Length) _index++;
            }

            private void Expect(char c)
            {
                SkipWhite();
                if (_index >= _text.Length || _text[_index] != c)
                    throw new Exception($"Expected '{c}' at {_index}");
                _index++;
            }

            private void ExpectString(string s)
            {
                SkipWhite();
                for (var i = 0; i < s.Length; i++)
                {
                    if (_index + i >= _text.Length || _text[_index + i] != s[i])
                        throw new Exception($"Expected '{s}' at {_index}");
                }

                _index += s.Length;
            }

            private string ReadUntil(char c)
            {
                var start = _index;
                while (_index < _text.Length && _text[_index] != c) _index++;
                return _text.Substring(start, _index - start);
            }

            private string ReadEnclosed(char open, char close)
            {
                var depth = 1;
                var start = _index;
                var inString = false;
                var stringChar = '\0';

                while (_index < _text.Length)
                {
                    var ch = _text[_index];

                    if (inString)
                    {
                        if (ch == '\\' && _index + 1 < _text.Length)
                        {
                            // Skip escaped characters inside strings
                            _index += 2;
                            continue;
                        }

                        if (ch == stringChar)
                        {
                            inString = false;
                        }
                    }
                    else
                    {
                        if (ch == '"' || ch == '\'')
                        {
                            inString = true;
                            stringChar = ch;
                        }
                        else if (ch == open)
                        {
                            depth++;
                        }
                        else if (ch == close)
                        {
                            depth--;
                            if (depth == 0)
                            {
                                // Do not consume the closing character so that caller can verify it
                                return _text.Substring(start, _index - start);
                            }
                        }
                    }

                    _index++;
                }

                // If we reach here the text was malformed; return the remainder
                return _text[start..];
            }

            private static string Unquote(string value)
            {
                if (value.Length >= 2)
                {
                    if ((value[0] == '"' && value[^1] == '"') ||
                        (value[0] == '\'' && value[^1] == '\''))
                    {
                        return value.Substring(1, value.Length - 2);
                    }
                }

                return value;
            }
        }
    }
}

using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Provides entry points for parsing script text into event/action structures.
    /// </summary>
    public static class TextScriptParser
    {
        /// <summary>
        /// Parses a script string written in the DSL format.
        /// </summary>
        /// <param name="script">Full script text.</param>
        /// <returns>Dictionary mapping event names to parsed events.</returns>
        public static Dictionary<string, ParsedEvent> ParseString(string script)
        {
            var tokenizer = new ScriptTokenizer(script);
            var parser = new Parser(tokenizer);
            return parser.Parse();
        }

        private class Parser
        {
            private readonly ScriptTokenizer _tokenizer;
            private readonly Dictionary<string, ParsedEvent> _events = new();
            private readonly ConditionStack _conditions = new();

            public Parser(ScriptTokenizer tokenizer) => _tokenizer = tokenizer;

            public Dictionary<string, ParsedEvent> Parse()
            {
                while (_tokenizer.SkipWhite())
                {
                    if (_tokenizer.Peek() == '[')
                    {
                        ParseEventSection();
                    }
                    else
                    {
                        _tokenizer.SkipLine();
                    }
                }

                return _events;
            }

            private void ParseEventSection()
            {
                _tokenizer.Expect('[');
                var name = _tokenizer.ReadUntil(']').Trim();
                _tokenizer.Expect(']');
                if (!_events.TryGetValue(name, out var pe))
                {
                    pe = new ParsedEvent { EventName = name };
                    _events.Add(name, pe);
                }

                _tokenizer.SkipLine();
                while (_tokenizer.SkipWhite())
                {
                    if (_tokenizer.StartsWith("["))
                        break;
                    if (_tokenizer.StartsWith("if"))
                    {
                        ParseIfStatement(pe);
                    }
                    else if (_tokenizer.StartsWith("act"))
                    {
                        AddActions(pe, new ActionParser(_tokenizer).Parse());
                    }
                    else
                    {
                        _tokenizer.SkipLine();
                    }
                }
            }

            private void AddActions(ParsedEvent pe, List<ParsedAction> actions)
            {
                foreach (var pa in actions)
                {
                    _conditions.Apply(pa);
                    pe.Actions.Add(pa);
                }
            }

            private void ParseIfStatement(ParsedEvent pe)
            {
                _tokenizer.ExpectString("if");
                _tokenizer.SkipWhite();
                _tokenizer.Expect('(');
                var expr = _tokenizer.ReadEnclosed('(', ')');
                _tokenizer.Expect(')');
                _tokenizer.SkipWhite();
                _tokenizer.Expect('{');
                var accumulated = expr.Trim();
                _conditions.Push(accumulated);
                _tokenizer.SkipWhite();
                while (_tokenizer.SkipWhite() && !_tokenizer.StartsWith("}"))
                {
                    if (_tokenizer.StartsWith("if"))
                    {
                        ParseIfStatement(pe);
                    }
                    else if (_tokenizer.StartsWith("act"))
                    {
                        AddActions(pe, new ActionParser(_tokenizer).Parse());
                    }
                    else
                    {
                        _tokenizer.SkipLine();
                    }
                }

                _tokenizer.Expect('}');
                _conditions.Pop();
                _tokenizer.SkipWhite();

                while (_tokenizer.StartsWith("else"))
                {
                    _tokenizer.ExpectString("else");
                    _tokenizer.SkipWhite();
                    var hasElseIf = false;
                    if (_tokenizer.StartsWith("if"))
                    {
                        hasElseIf = true;
                        _tokenizer.ExpectString("if");
                        _tokenizer.SkipWhite();
                        _tokenizer.Expect('(');
                        var cond = _tokenizer.ReadEnclosed('(', ')');
                        _tokenizer.Expect(')');
                        var trimmed = cond.Trim();
                        _conditions.Push($"!({accumulated}) && ({trimmed})");
                        accumulated = $"({accumulated}) || ({trimmed})";
                    }
                    else
                    {
                        _conditions.Push($"!({accumulated})");
                    }

                    _tokenizer.SkipWhite();
                    _tokenizer.Expect('{');
                    _tokenizer.SkipWhite();
                    while (_tokenizer.SkipWhite() && !_tokenizer.StartsWith("}"))
                    {
                        if (_tokenizer.StartsWith("if"))
                        {
                            ParseIfStatement(pe);
                        }
                        else if (_tokenizer.StartsWith("act"))
                        {
                            AddActions(pe, new ActionParser(_tokenizer).Parse());
                        }
                        else
                        {
                            _tokenizer.SkipLine();
                        }
                    }

                    _tokenizer.Expect('}');
                    _conditions.Pop();
                    _tokenizer.SkipWhite();

                    if (!hasElseIf)
                        break;
                }
            }
        }
    }
}

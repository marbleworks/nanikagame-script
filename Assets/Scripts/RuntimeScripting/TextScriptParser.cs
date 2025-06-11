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
                while (true)
                {
                    var token = _tokenizer.PeekToken();
                    if (token.Type == ScriptTokenType.Eof)
                        break;
                    if (token.Type == ScriptTokenType.LBracket)
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
                _tokenizer.Expect(ScriptTokenType.LBracket);
                var name = _tokenizer.ReadUntil(']').Trim();
                _tokenizer.Expect(ScriptTokenType.RBracket);
                if (!_events.TryGetValue(name, out var pe))
                {
                    pe = new ParsedEvent { EventName = name };
                    _events.Add(name, pe);
                }

                _tokenizer.SkipLine();
                while (true)
                {
                    var tk = _tokenizer.PeekToken();
                    if (tk.Type is ScriptTokenType.Eof or ScriptTokenType.LBracket)
                        break;
                    if (tk.Type == ScriptTokenType.If)
                    {
                        ParseIfStatement(pe);
                    }
                    else if (tk.Type == ScriptTokenType.Act)
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
                _tokenizer.Expect(ScriptTokenType.If);
                _tokenizer.Expect(ScriptTokenType.LParen);
                var expr = _tokenizer.ReadEnclosed('(', ')');
                _tokenizer.Expect(ScriptTokenType.RParen);
                _tokenizer.Expect(ScriptTokenType.LBrace);
                var accumulated = expr.Trim();
                _conditions.Push(accumulated);
                while (_tokenizer.PeekToken().Type != ScriptTokenType.RBrace &&
                       _tokenizer.PeekToken().Type != ScriptTokenType.Eof)
                {
                    if (_tokenizer.PeekToken().Type == ScriptTokenType.If)
                    {
                        ParseIfStatement(pe);
                    }
                    else if (_tokenizer.PeekToken().Type == ScriptTokenType.Act)
                    {
                        AddActions(pe, new ActionParser(_tokenizer).Parse());
                    }
                    else
                    {
                        _tokenizer.SkipLine();
                    }
                }

                _tokenizer.Expect(ScriptTokenType.RBrace);
                _conditions.Pop();

                while (_tokenizer.PeekToken().Type == ScriptTokenType.Else)
                {
                    _tokenizer.Expect(ScriptTokenType.Else);
                    var hasElseIf = false;
                    if (_tokenizer.PeekToken().Type == ScriptTokenType.If)
                    {
                        hasElseIf = true;
                        _tokenizer.Expect(ScriptTokenType.If);
                        _tokenizer.Expect(ScriptTokenType.LParen);
                        var cond = _tokenizer.ReadEnclosed('(', ')');
                        _tokenizer.Expect(ScriptTokenType.RParen);
                        var trimmed = cond.Trim();
                        _conditions.Push($"!({accumulated}) && ({trimmed})");
                        accumulated = $"({accumulated}) || ({trimmed})";
                    }
                    else
                    {
                        _conditions.Push($"!({accumulated})");
                    }

                    _tokenizer.Expect(ScriptTokenType.LBrace);
                    while (_tokenizer.PeekToken().Type != ScriptTokenType.RBrace &&
                           _tokenizer.PeekToken().Type != ScriptTokenType.Eof)
                    {
                        if (_tokenizer.PeekToken().Type == ScriptTokenType.If)
                        {
                            ParseIfStatement(pe);
                        }
                        else if (_tokenizer.PeekToken().Type == ScriptTokenType.Act)
                        {
                            AddActions(pe, new ActionParser(_tokenizer).Parse());
                        }
                        else
                        {
                            _tokenizer.SkipLine();
                        }
                    }

                    _tokenizer.Expect(ScriptTokenType.RBrace);
                    _conditions.Pop();

                    if (!hasElseIf)
                        break;
                }
            }
        }
    }
}

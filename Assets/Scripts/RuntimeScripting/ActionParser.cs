using System;
using System.Collections.Generic;

namespace RuntimeScripting
{
    /// <summary>
    /// Parses <c>act { ... } mod { ... };</c> statements into <see cref="ParsedAction"/> objects.
    /// </summary>
    internal sealed class ActionParser
    {
        private readonly ScriptTokenizer _tokenizer;

        public ActionParser(ScriptTokenizer tokenizer)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        }

        /// <summary>
        /// Parses an act statement including optional modifiers.
        /// </summary>
        public List<ParsedAction> Parse()
        {
            _tokenizer.ExpectString("act");
            _tokenizer.SkipWhite();
            _tokenizer.Expect('{');
            var actionsContent = _tokenizer.ReadEnclosed('{', '}');
            _tokenizer.Expect('}');
            var actions = ParseActionList(actionsContent);
            _tokenizer.SkipWhite();
            Dictionary<string, string> mods = null;
            if (_tokenizer.StartsWith("mod"))
            {
                _tokenizer.ExpectString("mod");
                _tokenizer.SkipWhite();
                _tokenizer.Expect('{');
                var modContent = _tokenizer.ReadEnclosed('{', '}');
                _tokenizer.Expect('}');
                mods = ParseModifierList(modContent);
            }

            _tokenizer.Expect(';');
            _tokenizer.SkipWhite();

            var list = new List<ParsedAction>();
            foreach (var act in actions)
            {
                var pa = new ParsedAction {FunctionName = act.name};
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
                        list.Add(ParseActionExpr(part));
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
                        list.Add(ScriptTokenizer.Unquote(part));
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
                            dict[key] = ScriptTokenizer.Unquote(value);
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
    }
}

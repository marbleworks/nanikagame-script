using System;
using System.Collections.Generic;
using System.Text;

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
            _tokenizer.Expect(ScriptTokenType.Act);
            _tokenizer.Expect(ScriptTokenType.LBrace);
            var actionsContent = _tokenizer.ReadEnclosed('{', '}');
            _tokenizer.Expect(ScriptTokenType.RBrace);
            var actions = ParseActionList(actionsContent);
            Dictionary<string, string> mods = null;
            if (_tokenizer.PeekToken().Type == ScriptTokenType.Mod)
            {
                _tokenizer.Expect(ScriptTokenType.Mod);
                _tokenizer.Expect(ScriptTokenType.LBrace);
                var modContent = _tokenizer.ReadEnclosed('{', '}');
                _tokenizer.Expect(ScriptTokenType.RBrace);
                mods = ParseModifierList(modContent);
            }

            _tokenizer.Expect(ScriptTokenType.Semicolon);


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
            var tokenizer = new ActionTokenizer(text);
            var list = new List<(string, List<string>)>();
            var token = tokenizer.Next();
            while (token.Type != ActionTokenType.Eof)
            {
                if (token.Type != ActionTokenType.Identifier)
                    throw new InvalidOperationException("Expected action name");

                var name = token.Value;
                token = tokenizer.Next();
                var args = new List<string>();

                if (token.Type == ActionTokenType.LParen)
                {
                    var sb = new System.Text.StringBuilder();
                    var depth = 1;
                    token = tokenizer.Next();
                    while (token.Type != ActionTokenType.Eof && depth > 0)
                    {
                        if (token.Type == ActionTokenType.LParen)
                        {
                            depth++;
                            sb.Append('(');
                            token = tokenizer.Next();
                            continue;
                        }

                        if (token.Type == ActionTokenType.RParen)
                        {
                            depth--;
                            if (depth == 0)
                                break;
                            sb.Append(')');
                            token = tokenizer.Next();
                            continue;
                        }

                        AppendToken(sb, token);
                        token = tokenizer.Next();
                    }

                    args = ParseArgList(sb.ToString());
                    if (token.Type == ActionTokenType.RParen)
                        token = tokenizer.Next();
                }

                list.Add((name, args));
                if (token.Type == ActionTokenType.Comma)
                    token = tokenizer.Next();
            }

            return list;
        }

        private static (string name, List<string> args) ParseActionExpr(string text)
        {
            var tokenizer = new ActionTokenizer(text);
            var token = tokenizer.Next();
            if (token.Type != ActionTokenType.Identifier)
                throw new InvalidOperationException("Invalid action expression");

            var name = token.Value;
            var args = new List<string>();
            token = tokenizer.Next();
            if (token.Type == ActionTokenType.LParen)
            {
                var sb = new System.Text.StringBuilder();
                var depth = 1;
                token = tokenizer.Next();
                while (token.Type != ActionTokenType.Eof && depth > 0)
                {
                    if (token.Type == ActionTokenType.LParen)
                    {
                        depth++;
                        sb.Append('(');
                    }
                    else if (token.Type == ActionTokenType.RParen)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            break;
                        }
                        sb.Append(')');
                    }
                    else
                    {
                        AppendToken(sb, token);
                    }

                    token = tokenizer.Next();
                }

                args = ParseArgList(sb.ToString());
            }

            return (name, args);
        }

        private static List<string> ParseArgList(string text)
        {
            var tokenizer = new ActionTokenizer(text);
            var list = new List<string>();
            var token = tokenizer.Next();
            var sb = new System.Text.StringBuilder();
            var depth = 0;
            while (token.Type != ActionTokenType.Eof)
            {
                if (token.Type == ActionTokenType.Comma && depth == 0)
                {
                    var part = sb.ToString().Trim();
                    if (part.Length > 0)
                        list.Add(ScriptTokenizer.Unquote(part));
                    sb.Clear();
                }
                else
                {
                    if (token.Type == ActionTokenType.LParen)
                        depth++;
                    else if (token.Type == ActionTokenType.RParen)
                        depth--;

                    AppendToken(sb, token);
                }

                token = tokenizer.Next();
            }

            var last = sb.ToString().Trim();
            if (last.Length > 0)
                list.Add(ScriptTokenizer.Unquote(last));

            return list;
        }

        private static Dictionary<string, string> ParseModifierList(string text)
        {
            var tokenizer = new ActionTokenizer(text);
            var dict = new Dictionary<string, string>();
            var token = tokenizer.Next();
            while (token.Type != ActionTokenType.Eof)
            {
                if (token.Type != ActionTokenType.Identifier)
                    break;

                var key = token.Value;
                token = tokenizer.Next();
                if (token.Type != ActionTokenType.Assign)
                    break;

                token = tokenizer.Next();
                var sb = new System.Text.StringBuilder();
                var depth = 0;
                while (token.Type != ActionTokenType.Eof && !(token.Type == ActionTokenType.Comma && depth == 0))
                {
                    if (token.Type == ActionTokenType.LParen)
                        depth++;
                    else if (token.Type == ActionTokenType.RParen)
                        depth--;

                    AppendToken(sb, token);
                    token = tokenizer.Next();
                }

                dict[key] = ScriptTokenizer.Unquote(sb.ToString().Trim());

                if (token.Type == ActionTokenType.Comma)
                    token = tokenizer.Next();
            }

            return dict;
        }

        private static void AppendToken(StringBuilder sb, ActionToken token)
        {
            if (token.Type == ActionTokenType.String)
            {
                sb.Append('"').Append(token.Value).Append('"');
            }
            else
            {
                sb.Append(token.Value);
            }
        }
    }
}

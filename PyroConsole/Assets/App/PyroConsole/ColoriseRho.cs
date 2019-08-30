namespace App
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Text;
    using Pyro.Language;
    using Pyro.RhoLang.Lexer;

    /// <summary>
    /// Colorise Rho script.
    /// </summary>
    public partial class ColoriseRho
    {
        public ColoriseRho(Dictionary<ERhoToken, string> colorMap)
        {
            _colorMap = colorMap;
        }

        private const string _defaultFont = "";
        private const string _boldFont = "";
        private StringBuilder _sb;

        private Dictionary<ERhoToken, string> _colorMap = new Dictionary<ERhoToken, string>();

        public void SetColors(Dictionary<ERhoToken, string> colors)
            => _colorMap = colors;

        private void BeginUpdate()
        {
        }

        private void EndUpdate()
        {
        }

        public string Colorise(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            BeginUpdate();
            var lex = new RhoLexer(text);
            _sb = new StringBuilder();
            try
            {
                lex.Process();

                foreach (var tok in lex.Tokens)
                {
                    switch (tok.Type)
                    {
                        case ERhoToken.Nop:
                            continue;
                        case ERhoToken.NewLine:
                            _sb.Append('\n');
                            continue;
                    }

                    //Debug.Log(tok);

                    ColoriseRhoToken(tok, tok.Slice);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.StackTrace);
                Debug.LogError($"{e.Message}");
                _sb.Clear();
                _sb.Append(text);
            }
            finally
            {
                EndUpdate();
            }

            return _sb.ToString();
        }

        private void DefaultColors(LexerBase lex, string input)
        {
            //var slice = new Slice(lex, 0, input.Length - 1) {LineNumber = 0};
            //SetRhoSliceColor(slice, Color.White, _defaultFont);
        }

        private string GetTokenColor(ERhoToken token)
        {
            return _colorMap.TryGetValue(token, out var val) ? val : "white";
        }

        private bool ColoriseRhoToken(RhoToken tok, Slice slice)
        {
            if (slice.Length < 0)
                return false;

            bool Render(string color)
                => SetRhoSliceColor(slice, color, _defaultFont);

            bool RenderBold(string color)
                => SetRhoSliceColor(slice, color, _boldFont);

            var token = tok.Type;
            switch (token)
            {
                case ERhoToken.Assert:
                case ERhoToken.Fun:
                case ERhoToken.Class:
                case ERhoToken.Ident:
                    return RenderBold(GetTokenColor(token));
                case ERhoToken.LessEquiv:
                    return Render("#707070");
                case ERhoToken.String:
                    // color the quotes too
                    var expanded = new Slice(slice.Lexer, slice.LineNumber, slice.Start - 1, slice.End + 1);
                    return SetRhoSliceColor(expanded, "#FFA500", _boldFont);
                case ERhoToken.PiSlice:
                    var expanded2 = new Slice(slice.Lexer, slice.LineNumber, slice.Start - 1, slice.End + 1);
                    return SetRhoSliceColor(expanded2, "#FFA5FF", _boldFont);
                default:
                    return Render(GetTokenColor(token));
            }
        }

        private bool SetSliceColor(Slice slice, string color, string font)
        {
            _sb.Append($"<color={color}>{slice.Text}</color>");
            return true;
        }

        private bool SetRhoSliceColor(Slice slice, string color, string font)
        {
            _sb.Append($"<color={color}>{slice.Text}</color>");
            return true;
        }
    }
}


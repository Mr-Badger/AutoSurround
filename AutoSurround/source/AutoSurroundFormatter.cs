using AutoSurround.Entities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;

namespace AutoSurround
{
    internal sealed class AutoSurroundFormatter
    {
        private readonly IWpfTextView view;
        private readonly IEditorOperationsFactoryService factory;
        
        private bool isChangingText;
        private ITextChange change;
        private Dictionary<char, SelectionWrapper> delimiterMap;

        public AutoSurroundFormatter(IWpfTextView view, IEditorOperationsFactoryService factory)
        {
            this.view = view;
            this.view.TextBuffer.Changed += TextBuffer_Changed;
            this.view.TextBuffer.PostChanged += TextBuffer_PostChanged;
            this.factory = factory;

            delimiterMap = new Dictionary<char, SelectionWrapper>();

            SetDelimiterMap();
            OptionPage.OnUpdate += (s, e) => SetDelimiterMap();
        }

        private void SetDelimiterMap()
        {
            delimiterMap.Clear();

            var options = OptionPage.Instance;
            if (options.EnableBraces) delimiterMap.Add('(', new SelectionWrapper("(", ")"));
            if (options.EnableParentheses) delimiterMap.Add('{', new SelectionWrapper("{", "}"));
            if (options.EnableSquareBrackets) delimiterMap.Add('[', new SelectionWrapper("[", "]"));
            if (options.EnableAngleBracket) delimiterMap.Add('<', new SelectionWrapper("<", ">"));
            if (options.EnableDoublequotes) delimiterMap.Add('"', new SelectionWrapper("\"", "\""));
            if (options.EnableSingequotes) delimiterMap.Add('\'', new SelectionWrapper("\'", "\'"));
            if (options.EnableCommentBlock) delimiterMap.Add('/', new SelectionWrapper("/*", "*/"));
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            if (!isChangingText)
            {
                isChangingText = true;
                WrapCode(e);
            }
        }

        private void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            isChangingText = false;
        }

        private void WrapCode(TextContentChangedEventArgs e)
        {
            if (e.Changes != null)
            {
                for (int i = e.Changes.Count - 1; i >= 0; i--)
                {
                    change = e.Changes[i];
                    HandleChange();
                }
            }
        }

        private void HandleChange()
        {
            if (change.NewLength == 0 || string.IsNullOrWhiteSpace(change.OldText))
                return;

            //todo: virtual space check

            var newText = change.NewText.Trim();

            if (newText.Length != 1)
                return;

            if (SingleQuoteRule(newText[0]) && change.OldLength == 1)
                return;

            if (delimiterMap.TryGetValue(newText[0], out var delimiter))
            {
                var spaces = view.Selection.End.IsInVirtualSpace
                           ? new string(' ', view.Selection.End.VirtualSpaces)
                           : string.Empty;

                var replacedText = delimiter.OpenTag + change.OldText + spaces + delimiter.CloseTag;

                var edit = view.TextBuffer.CreateEdit();
                edit.Replace(change.NewEnd - 1, 1, replacedText);
                edit.Apply();
            }
        }

        private bool SingleQuoteRule(char symbol)
        {
            return symbol != '\'' || change.OldText[0] == '"';
        }
    }
}

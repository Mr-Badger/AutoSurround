using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;

namespace AutoSurround
{
    internal sealed class AutoSurroundFormatter
    {
        private IWpfTextView view;

        private bool isChangingText;

        private Dictionary<char, (string open, string close)> delimiterMap;
        
        public AutoSurroundFormatter(IWpfTextView view)
        {
            this.view = view;
            this.view.TextBuffer.Changed += TextBuffer_Changed;
            this.view.TextBuffer.PostChanged += (o, e) => isChangingText = false;

            delimiterMap = new Dictionary<char, (string, string)>();

            SetDelimiterMap();
            OptionPage.OnUpdate += (o, s) => SetDelimiterMap();
        }

        private void SetDelimiterMap()
        {
            delimiterMap.Clear();

            var options = OptionPage.Instance;
            if (options.EnableBraces)         delimiterMap.Add('(', ("(",  ")"));
            if (options.EnableParentheses)    delimiterMap.Add('{', ("{",  "}"));
            if (options.EnableSquareBrackets) delimiterMap.Add('[', ("[",  "]"));
            if (options.EnableAngleBracket)   delimiterMap.Add('<', ("<",  ">"));
            if (options.EnableDoublequotes)   delimiterMap.Add('"', ("\"", "\""));
            if (options.EnableSingequotes)    delimiterMap.Add('\'',("\'", "\'"));
            if (options.EnableCommentBlock)   delimiterMap.Add('/', ("/*", "*/"));
        }

        private void TextBuffer_Changed(object o, TextContentChangedEventArgs e)
        {
            if (!isChangingText)
            {
                isChangingText = true;
                FormatCode(e);
            }
        }

        private void FormatCode(TextContentChangedEventArgs e)
        {
            if (e.Changes != null)
            {
                for (int i = e.Changes.Count - 1; i >= 0; i--)
                {
                    HandleChange(e.Changes[i]);
                }
                for (int i = e.Changes.Count - 1; i >= 0; i--)
                {
                    SetCursors(e.Changes[i]);
                }
            }
        }

        private void SetCursors(ITextChange textChange)
        {
            /*
            view.Selection.Clear();
            var noSelect = false;
            var atStart = false;
            var selectAll = false;

            var a = view.TextViewLines;
            view.Selection.SelectedSpans
            var selectedSpans = view.Selection.VirtualSelectedSpans;
            foreach (var span in view.Selection.SelectedSpans)
            {
                //var point = new SnapshotPoint()
                (var snap = new SnapshotSpan(span.Start, span.Length);
                Debug.WriteLine(span.Length);
            }
            */
        }

        private void HandleChange(ITextChange change)
        {
            // Visual Studio uses fake carret indent in front of empty lines and inserts spaces if needed
            var newText = Math.Abs(change.LineCountDelta) > 1 && change.OldText.StartsWith("\r\n")
                        ? change.NewText.TrimStart() 
                        : change.NewText;

            if (change.OldLength == 0 || newText.Length != 1)
                return;

            if (string.IsNullOrWhiteSpace(change.OldText))
                return;
            
            if (delimiterMap.TryGetValue(newText[0], out var tags))
            {
                var text = (newText[0] == '/' ? "*" : "") + change.OldText;

                var edit = view.TextBuffer.CreateEdit();
                edit.Insert(change.NewPosition + change.NewLength, text + tags.close);
                edit.Apply();
            }
        }
        
    }
}

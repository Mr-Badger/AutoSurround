using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace AutoSurround
{
    [ContentType("code")]
    [Export(typeof(IWpfTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class TextViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        IEditorOperationsFactoryService factory = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            new AutoSurroundFormatter(textView, factory);
        }
    }
}

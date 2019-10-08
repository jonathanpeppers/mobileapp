using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.iOS.Extensions
{
    public static class TextViewExtensions
    {
        public static void RejectAutocorrect(this UITextView textView)
        {
            var originalText = textView.Text;
            NSRange originalRange = textView.SelectedRange;
            CGPoint originalOffset = textView.ContentOffset;

            //Force any pending autocorrection to be applied
            textView.ResignFirstResponder();
            textView.BecomeFirstResponder();

            string finalText = textView.Text;
            if (originalText != finalText)
            {
                textView.Text = originalText;
                textView.SelectedRange = originalRange;
                textView.SetContentOffset(originalOffset, false);
            }
        }
    }
}

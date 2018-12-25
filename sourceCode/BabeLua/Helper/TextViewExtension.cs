using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Helper
{
    static class TextViewExtension
    {
        internal static SnapshotPoint? GetCaretPosition(this ITextView view)
        {
            return view.BufferGraph.MapDownToFirstMatch(
               new SnapshotPoint(view.TextBuffer.CurrentSnapshot, view.Caret.Position.BufferPosition),
               PointTrackingMode.Positive,
               (snapshot) => { return snapshot.ContentType.IsOfType("Lua"); },
               PositionAffinity.Successor
            );
        }

        internal static ITrackingSpan GetCaretSpan(this ITextView view)
        {
            var caretPoint = view.GetCaretPosition();
            Debug.Assert(caretPoint != null);
            var snapshot = caretPoint.Value.Snapshot;
            var caretPos = caretPoint.Value.Position;

            // fob(
            //    ^
            //    +---  Caret here
            //
            // We want to lookup fob, not fob(
            //
            ITrackingSpan span;
            if (caretPos != snapshot.Length)
            {
                string curChar = snapshot.GetText(caretPos, 1);
                if (!curChar[0].IsIdentifier() && caretPos > 0)
                {
                    string prevChar = snapshot.GetText(caretPos - 1, 1);
                    if (prevChar[0].IsIdentifier())
                    {
                        caretPos--;
                    }
                }
                span = snapshot.CreateTrackingSpan(
                    caretPos,
                    1,
                    SpanTrackingMode.EdgeInclusive
                );
            }
            else
            {
                span = snapshot.CreateTrackingSpan(
                    caretPos,
                    0,
                    SpanTrackingMode.EdgeInclusive
                );
            }

            return span;
        }

        internal static SnapshotSpan GetToken(this ITextView view, SnapshotPoint point)
        {
            var snapshot = view.TextSnapshot;
            if (point > snapshot.Length || point < 0) throw new ArgumentOutOfRangeException();
            int start = point, end = point;
            while(start > 0)
            {
                char ch = snapshot[start - 1];
                if (ch.IsIdentifier() || ch == '.' || ch == ':')
                {
                    start--;
                    continue;
                }
                break;
            }
            while(end < snapshot.Length - 1)
            {
                char ch = snapshot[end];
                if(ch.IsIdentifier())
                {
                    end++;
                    continue;
                }
                break;
            }
            return new SnapshotSpan(snapshot, start, end - start);
        }
    }
}

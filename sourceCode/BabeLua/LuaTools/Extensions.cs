/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Microsoft.LuaTools.Analysis;
using Microsoft.LuaTools.Intellisense;
//using Microsoft.LuaTools.Interpreter;
//using Microsoft.LuaTools.Interpreter.Default;
//using Microsoft.LuaTools.Parsing.Ast;
using Microsoft.LuaTools.Project;
//using Microsoft.LuaTools.Repl;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
//using Microsoft.VisualStudio.Repl;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudioTools;

namespace Microsoft.LuaTools {
#if INTERACTIVE_WINDOW
    using IReplEvaluator = IInteractiveEngine;
#endif

    public static class Extensions {
/*
        public static StandardGlyphGroup ToGlyphGroup(this LuaMemberType objectType) {
            StandardGlyphGroup group;
            switch (objectType) {
                case LuaMemberType.Class: group = StandardGlyphGroup.GlyphGroupClass; break;
                case LuaMemberType.DelegateInstance: 
                case LuaMemberType.Delegate: group = StandardGlyphGroup.GlyphGroupDelegate; break;
                case LuaMemberType.Enum: group = StandardGlyphGroup.GlyphGroupEnum; break;
                case LuaMemberType.Namespace: group = StandardGlyphGroup.GlyphGroupNamespace; break;
                case LuaMemberType.Multiple: group = StandardGlyphGroup.GlyphGroupOverload; break;
                case LuaMemberType.Field: group = StandardGlyphGroup.GlyphGroupField; break;
                case LuaMemberType.Module: group = StandardGlyphGroup.GlyphGroupModule; break;
                case LuaMemberType.Property: group = StandardGlyphGroup.GlyphGroupProperty; break;
                case LuaMemberType.Instance: group = StandardGlyphGroup.GlyphGroupVariable; break;
                case LuaMemberType.Constant: group = StandardGlyphGroup.GlyphGroupVariable; break;
                case LuaMemberType.EnumInstance: group = StandardGlyphGroup.GlyphGroupEnumMember; break;
                case LuaMemberType.Event: group = StandardGlyphGroup.GlyphGroupEvent; break;
                case LuaMemberType.Keyword: group = StandardGlyphGroup.GlyphKeyword; break;
                case LuaMemberType.Function:
                case LuaMemberType.Method:
                default:
                    group = StandardGlyphGroup.GlyphGroupMethod;
                    break;
            }
            return group;
        }
*/
        internal static bool CanComplete(this ClassificationSpan token) {
            return token.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Keyword) |
                token.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Identifier);
        }

        /// <summary>
        /// Returns the span to use for the provided intellisense session.
        /// </summary>
        /// <returns>A tracking span. The span may be of length zero if there
        /// is no suitable token at the trigger point.</returns>
        internal static ITrackingSpan GetApplicableSpan(this IIntellisenseSession session, ITextBuffer buffer) {
            var snapshot = buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(buffer);

            var span = snapshot.GetApplicableSpan(triggerPoint);
            if (span != null) {
                return span;
            }
            return snapshot.CreateTrackingSpan(triggerPoint.GetPosition(snapshot), 0, SpanTrackingMode.EdgeInclusive);
        }

        /// <summary>
        /// Returns the applicable span at the provided position.
        /// </summary>
        /// <returns>A tracking span, or null if there is no token at the
        /// provided position.</returns>
        internal static ITrackingSpan GetApplicableSpan(this ITextSnapshot snapshot, ITrackingPoint point) {
            return snapshot.GetApplicableSpan(point.GetPosition(snapshot));
        }

        /// <summary>
        /// Returns the applicable span at the provided position.
        /// </summary>
        /// <returns>A tracking span, or null if there is no token at the
        /// provided position.</returns>
        internal static ITrackingSpan GetApplicableSpan(this ITextSnapshot snapshot, int position) {
            var classifier = snapshot.TextBuffer.GetLuaClassifier();
            var line = snapshot.GetLineFromPosition(position);
            if (classifier == null || line == null) {
                return null;
            }

            var spanLength = position - line.Start.Position;
            // Increase position by one to include 'fob' in: "abc.|fob"
            if (spanLength < line.Length) {
                spanLength += 1;
            }
            
            var classifications = classifier.GetClassificationSpans(new SnapshotSpan(line.Start, spanLength));
            // Handle "|"
            if (classifications == null || classifications.Count == 0) {
                return null;
            }

            var lastToken = classifications[classifications.Count - 1];
            // Handle "fob |"
            if (lastToken == null || position > lastToken.Span.End) {
                return null;
            }

            if (position > lastToken.Span.Start) {
                if (lastToken.CanComplete()) {
                    // Handle "fo|o"
                    return snapshot.CreateTrackingSpan(lastToken.Span, SpanTrackingMode.EdgeInclusive);
                } else {
                    // Handle "<|="
                    return null;
                }
            }

            var secondLastToken = classifications.Count >= 2 ? classifications[classifications.Count - 2] : null;
            if (lastToken.Span.Start == position && lastToken.CanComplete() && 
                (secondLastToken == null ||             // Handle "|fob"
                 position > secondLastToken.Span.End || // Handle "if |fob"
                 !secondLastToken.CanComplete())) {     // Handle "abc.|fob"
                return snapshot.CreateTrackingSpan(lastToken.Span, SpanTrackingMode.EdgeInclusive);
            }

            // Handle "abc|."
            // ("ab|c." would have been treated as "ab|c")
            if (secondLastToken != null && secondLastToken.Span.End == position && secondLastToken.CanComplete()) {
                return snapshot.CreateTrackingSpan(secondLastToken.Span, SpanTrackingMode.EdgeInclusive);
            }

            return null;
        }

        internal static ITrackingSpan CreateTrackingSpan(this IQuickInfoSession session, ITextBuffer buffer) {
            var triggerPoint = session.GetTriggerPoint(buffer);
            var position = triggerPoint.GetPosition(buffer.CurrentSnapshot);
            if (position == buffer.CurrentSnapshot.Length) {
                return ((IIntellisenseSession)session).GetApplicableSpan(buffer);
            }

            return buffer.CurrentSnapshot.CreateTrackingSpan(position, 1, SpanTrackingMode.EdgeInclusive);
        }

        internal static ITrackingSpan CreateTrackingSpan(this ISmartTagSession session, ITextBuffer buffer) {
            var triggerPoint = session.GetTriggerPoint(buffer);
            var position = triggerPoint.GetPosition(buffer.CurrentSnapshot);
            if (position == buffer.CurrentSnapshot.Length) {
                return ((IIntellisenseSession)session).GetApplicableSpan(buffer);
            }

            var triggerChar = triggerPoint.GetCharacter(buffer.CurrentSnapshot);
            if (position != 0 && !char.IsLetterOrDigit(triggerChar)) {
                // end of line, back up one char as we may have an identifier
                return buffer.CurrentSnapshot.CreateTrackingSpan(position - 1, 1, SpanTrackingMode.EdgeInclusive);
            }

            return buffer.CurrentSnapshot.CreateTrackingSpan(position, 1, SpanTrackingMode.EdgeInclusive);
        }
/*        
        public static ILuaInterpreterFactory GetLuaInterpreterFactory(this IVsHierarchy self) {
            var node = (self.GetProject().GetCommonProject() as LuaProjectNode);
            if (node != null) {
                return node.GetInterpreterFactory();
            }
            return null;
        }
*/
        public static IEnumerable<IVsProject> EnumerateLoadedProjects(this IVsSolution solution) {
            var guid = new Guid(LuaConstants.ProjectFactoryGuid);
            IEnumHierarchies hierarchies;
            ErrorHandler.ThrowOnFailure((solution.GetProjectEnum(
                (uint)(__VSENUMPROJFLAGS.EPF_MATCHTYPE | __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION),
                ref guid,
                out hierarchies)));
            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            uint fetched;
            while (ErrorHandler.Succeeded(hierarchies.Next(1, hierarchy, out fetched)) && fetched == 1) {
                var project = hierarchy[0] as IVsProject;
                if (project != null) {
                    yield return project;
                }
            }
        }
/*
        public static IModuleContext GetModuleContext(this ITextBuffer buffer) {
            if (buffer == null) {
                return null;
            }

            var analyzer = buffer.GetAnalyzer();
            if (analyzer == null) {
                return null;
            }

            var path = buffer.GetFilePath();
            if (string.IsNullOrEmpty(path)) {
                return null;
            }

            var entry = analyzer.GetAnalysisFromFile(path);
            if (entry == null) {
                return null;
            }
            return entry.AnalysisContext;
        }
*/
        internal static LuaProjectNode GetLuaProject(this IVsProject project) {
            return ((IVsHierarchy)project).GetProject().GetCommonProject() as LuaProjectNode;
        }

        internal static LuaProjectNode GetLuaProject(this EnvDTE.Project project) {
            return project.GetCommonProject() as LuaProjectNode;
        }
/*        
        internal static void GotoSource(this LocationInfo location) {
            string zipFileName = VsProjectAnalyzer.GetZipFileName(location.ProjectEntry);
            if (zipFileName == null) {
                LuaToolsPackage.NavigateTo(
                    location.FilePath,
                    Guid.Empty,
                    location.Line - 1,
                    location.Column - 1);
            }
        }

        internal static bool TryGetAnalysis(this ITextBuffer buffer, out IProjectEntry analysis) {
            return buffer.Properties.TryGetProperty<IProjectEntry>(typeof(IProjectEntry), out analysis);
        }

        internal static bool TryGetLuaAnalysis(this ITextBuffer buffer, out ILuaProjectEntry analysis) {
            IProjectEntry entry;
            if (buffer.TryGetAnalysis(out entry) && (analysis = entry as ILuaProjectEntry) != null) {
                return true;
            }
            analysis = null;
            return false;
        }

        internal static IProjectEntry GetAnalysis(this ITextBuffer buffer) {
            IProjectEntry res;
            buffer.TryGetAnalysis(out res);
            return res;
        }

        internal static ILuaProjectEntry GetLuaAnalysis(this ITextBuffer buffer) {
            ILuaProjectEntry res;
            buffer.TryGetLuaAnalysis(out res);
            return res;
        }

        internal static EnvDTE.Project GetProject(this ITextBuffer buffer) {
            var path = buffer.GetFilePath();
            if (path != null && LuaToolsPackage.Instance != null) {
                var item = LuaToolsPackage.Instance.DTE.Solution.FindProjectItem(path);
                if (item != null) {
                    return item.ContainingProject;
                }
            }
            return null;
        }

        internal static VsProjectAnalyzer GetAnalyzer(this ITextView textView) {
            LuaReplEvaluator evaluator;
            if (textView.Properties.TryGetProperty<LuaReplEvaluator>(typeof(LuaReplEvaluator), out evaluator)) {
                return evaluator.ReplAnalyzer;
            }
            return textView.TextBuffer.GetAnalyzer();
        }
*/
        internal static SnapshotPoint? GetCaretPosition(this ITextView view) {
            return view.BufferGraph.MapDownToFirstMatch(
               new SnapshotPoint(view.TextBuffer.CurrentSnapshot, view.Caret.Position.BufferPosition),
               PointTrackingMode.Positive,
               LuaCoreConstants.IsLuaContent,
               PositionAffinity.Successor
            );
        }
/*
        internal static ExpressionAnalysis GetExpressionAnalysis(this ITextView view) {
            ITrackingSpan span = GetCaretSpan(view);
            return span.TextBuffer.CurrentSnapshot.AnalyzeExpression(span, false);
        }
*/
        internal static ITrackingSpan GetCaretSpan(this ITextView view) {
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
            if (caretPos != snapshot.Length) {
                string curChar = snapshot.GetText(caretPos, 1);
                if (!IsIdentifierChar(curChar[0]) && caretPos > 0) {
                    string prevChar = snapshot.GetText(caretPos - 1, 1);
                    if (IsIdentifierChar(prevChar[0])) {
                        caretPos--;
                    }
                }
                span = snapshot.CreateTrackingSpan(
                    caretPos,
                    1,
                    SpanTrackingMode.EdgeInclusive
                );
            } else {
                span = snapshot.CreateTrackingSpan(
                    caretPos,
                    0,
                    SpanTrackingMode.EdgeInclusive
                );
            }

            return span;
        }

        private static bool IsIdentifierChar(char curChar) {
            return Char.IsLetterOrDigit(curChar) || curChar == '_';
        }

        /// <summary>
        /// Reads a string from the socket which is encoded as:
        ///     U, byte count, bytes 
        ///     A, byte count, ASCII
        ///     
        /// Which supports either UTF-8 or ASCII strings.
        /// </summary>
        internal static string ReadString(this Socket socket) {
            byte[] cmd_buffer = new byte[4];
            if (socket.Receive(cmd_buffer, 1, SocketFlags.None) == 1) {
                bool isUnicode = cmd_buffer[0] == 'U';

                if (socket.Receive(cmd_buffer) == 4) {
                    int filenameLen = BitConverter.ToInt32(cmd_buffer, 0);
                    byte[] buffer = new byte[filenameLen];
                    if (filenameLen != 0) {
                        int bytesRead = 0;
                        do {
                            bytesRead += socket.Receive(buffer, bytesRead, filenameLen - bytesRead, SocketFlags.None);
                        } while (bytesRead != filenameLen);
                    }

                    if (isUnicode) {
                        return Encoding.UTF8.GetString(buffer);
                    } else {
                        char[] chars = new char[buffer.Length];
                        for (int i = 0; i < buffer.Length; i++) {
                            chars[i] = (char)buffer[i];
                        }
                        return new string(chars);
                    }
                } else {
                    Debug.Assert(false, "Failed to read length");
                }
            } else {
                Debug.Assert(false, "Failed to read unicode/ascii byte");
            }
            return null;
        }

        internal static int ReadInt(this Socket socket) {
            byte[] cmd_buffer = new byte[4];
            if (socket.Receive(cmd_buffer) == 4) {
                return BitConverter.ToInt32(cmd_buffer, 0);
            }
            throw new InvalidOperationException();
        }
/*
        internal static VsProjectAnalyzer GetAnalyzer(this ITextBuffer buffer) {
            LuaProjectNode pyProj;
            VsProjectAnalyzer analyzer;
            if (!buffer.Properties.TryGetProperty<LuaProjectNode>(typeof(LuaProjectNode), out pyProj)) {
                var project = buffer.GetProject();
                if (project != null) {
                    pyProj = project.GetLuaProject();
                    if (pyProj != null) {
                        buffer.Properties.AddProperty(typeof(LuaProjectNode), pyProj);
                    }
                }
            }

            if (pyProj != null) {
                analyzer = pyProj.GetAnalyzer();
                return analyzer;
            }
            
            // exists for tests where we don't run in VS and for the existing changes preview
            if (buffer.Properties.TryGetProperty<VsProjectAnalyzer>(typeof(VsProjectAnalyzer), out analyzer)) {
                return analyzer;
            }

            return LuaToolsPackage.Instance.DefaultAnalyzer;
        }

        /// <summary>
        /// Checks to see if this is a REPL buffer starting with a extensible command such as %cls, %load, etc...
        /// </summary>
        internal static bool IsReplBufferWithCommand(this ITextSnapshot snapshot) {
            return snapshot.TextBuffer.Properties.ContainsProperty(typeof(IReplEvaluator)) &&
                   snapshot.Length != 0 &&
                   (snapshot[0] == '%' || snapshot[0] == '$'); // ILua and normal repl commands
        }

        internal static bool IsAnalysisCurrent(this ILuaInterpreterFactory factory) {
            var interpFact = factory as ILuaInterpreterFactoryWithDatabase;
            if (interpFact != null) {
                return interpFact.IsCurrent;
            }

            return true;
        }
*/
        internal static bool IsOpenGrouping(this ClassificationSpan span) {
            return span.ClassificationType.IsOfType(LuaPredefinedClassificationTypeNames.Grouping) &&
                span.Span.Length == 1 &&
                (span.Span.GetText() == "{" || span.Span.GetText() == "[" || span.Span.GetText() == "(");
        }

        internal static bool IsCloseGrouping(this ClassificationSpan span) {
            return span.ClassificationType.IsOfType(LuaPredefinedClassificationTypeNames.Grouping) &&
                span.Span.Length == 1 &&
                (span.Span.GetText() == "}" || span.Span.GetText() == "]" || span.Span.GetText() == ")");
        }

        internal static T Pop<T>(this List<T> list) {
            if (list.Count == 0) {
                throw new InvalidOperationException();
            }
            var res = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return res;
        }

        internal static T Peek<T>(this List<T> list) {
            if (list.Count == 0) {
                throw new InvalidOperationException();
            }
            return list[list.Count - 1];
        }

        internal static Task StartNew(this TaskScheduler scheduler, Action func) {
            return Task.Factory.StartNew(func, default(CancellationToken), TaskCreationOptions.None, scheduler);
        }
/*
        internal static int GetStartIncludingIndentation(this Node self, LuaAst ast) {
            return self.StartIndex - (self.GetIndentationLevel(ast) ?? "").Length;
        }
*/
        internal static string LimitLines(
            this string str,
            int maxLines = 30,
            int charsPerLine = 200,
            bool ellipsisAtEnd = true,
            bool stopAtFirstBlankLine = false
        ) {
            if (string.IsNullOrEmpty(str)) {
                return str;
            }

            int lineCount = 0;
            var prettyPrinted = new StringBuilder();
            bool wasEmpty = true;

            using (var reader = new StringReader(str)) {
                for (var line = reader.ReadLine(); line != null && lineCount < maxLines; line = reader.ReadLine()) {
                    if (string.IsNullOrWhiteSpace(line)) {
                        if (wasEmpty) {
                            continue;
                        }
                        wasEmpty = true;
                        if (stopAtFirstBlankLine) {
                            lineCount = maxLines;
                            break;
                        }
                        lineCount += 1;
                        prettyPrinted.AppendLine();
                    } else {
                        wasEmpty = false;
                        lineCount += (line.Length / charsPerLine) + 1;
                        prettyPrinted.AppendLine(line);
                    }
                }
            }
            if (ellipsisAtEnd && lineCount >= maxLines) {
                prettyPrinted.AppendLine("...");
            }
            return prettyPrinted.ToString().Trim();
        }
    }
}

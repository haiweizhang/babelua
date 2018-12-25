using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

using Babe.Lua.DataModel;
using Microsoft.VisualStudio.Text.Operations;

namespace Babe.Lua.Intellisense
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("Lua")]
    [Name("LuaCompletion")]
    class LuaCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        IGlyphService GlyphService;
        
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new LuaCompletionSource(this, textBuffer);
        }

        public System.Windows.Media.ImageSource GetImageSource(Type type)
        {
            StandardGlyphGroup group;
            StandardGlyphItem item = StandardGlyphItem.GlyphItemPublic;
            switch(type.Name)
            {
                case "LuaTable": group = StandardGlyphGroup.GlyphGroupClass; break;
                case "LuaFunction": group = StandardGlyphGroup.GlyphGroupMethod; break;
                case "LuaMember": group = StandardGlyphGroup.GlyphGroupField; break;
                default:
                    group = StandardGlyphGroup.GlyphGroupVariable;break;
            }
            return GlyphService.GetGlyph(group, item);
        }
    }

    class LuaCompletionSource : ICompletionSource
    {
        LuaCompletionSourceProvider _provider;
        private ITextBuffer _buffer;
        private bool _disposed = false;
        
        //each table functions

        public LuaCompletionSource(LuaCompletionSourceProvider provider, ITextBuffer buffer)
        {
            _buffer = buffer;
            _provider = provider;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                throw new ObjectDisposedException("LuaCompletionSource");
         
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
                return;

            var line = triggerPoint.GetContainingLine();
            SnapshotPoint start = triggerPoint;

            var word = start;
            word -= 1;
            var ch = word.GetChar();

            List<Completion> completions = new List<Completion>();

            while (word > line.Start && (word - 1).GetChar().IsWordOrDot())
            {
                word -= 1;
            }

            if (ch == '.' || ch == ':')
            {
                String w = snapshot.GetText(word.Position, start - 1 - word);
                if (!FillTable(w, ch, completions)) return;
            }
            else
            {
                char front = word > line.Start ? (word - 1).GetChar() : char.MinValue;
                if (front == '.' || front == ':')
                {
                    int loc = (word - 1).Position;
                    while (loc > 0 && snapshot[loc - 1].IsWordOrDot()) loc--;
                    int len = word - 1 - loc;
                    if (len <= 0) return;
                    string w = snapshot.GetText(loc, len);
                    if (!FillTable(w, front, completions)) return;
                }
                else
                {
                    String w = snapshot.GetText(word.Position, start - word);
                    if (!FillWord(w, completions)) return;
                }
            }

            if (ch != '.' && ch != ':')
            {
                start = word;
            }

            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);
            var cs = new LuaCompletionSet("All", "All", applicableTo, completions, null);
            completionSets.Add(cs);
        }

        public bool FillWord(String word, List<Completion> completions)
        {
            int count = completions.Count;
            //提示全局索引
            var list = IntellisenseHelper.GetGlobal();
            //提示当前文件单词
            var tokens = IntellisenseHelper.GetFileTokens();

            list = list.Concat(tokens).Distinct();

            foreach (LuaMember s in list)
            {
                completions.Add(new LuaCompletion(
                    show : s.Name, 
                    completion : s is LuaFunction ? s.ToString() : s.Name, 
                    description : string.Format("{0} {1}{2}", s.ToString(), GetMemberPath(s), s.Comment),
                    //description : s.GetType().Name + " : " + s.ToString() + " in file: " + s.File + s.Comment, 
                    icon : _provider.GetImageSource(s.GetType())
                    ));
            }

#if DEBUG
            System.Diagnostics.Debug.Print("fill items:" + (completions.Count - count));
#endif

            return completions.Count > count;
        }

        public bool FillTable(String word, char dot, List<Completion> completions)
        {
            var count = completions.Count;
            
            var table = IntellisenseHelper.GetTable(word);

			if (table != null)
            {
				var result = table.GetFullMembers();
                if (dot == '.')
                {
					foreach (var list in result)
					{
						foreach (LuaMember l in list.Value)
						{

                            if (completions.Exists((cp) => { return cp.DisplayText == l.Name; }))
                            {
                                continue;
                            }
                            completions.Add(new Completion(
                                l.Name, 
                                l is LuaFunction ? l.ToString() : l.Name, 
                                string.Format("{0}{1}{2} {3}{4}", list.Key, dot, l.ToString(), GetMemberPath(l), l.Comment),
                                //list.Key + dot + l.ToString() + l.Comment, 
                                _provider.GetImageSource(l.GetType()), "icon"));
						}
					}
                }
                else
                {
					foreach (var list in result)
					{
						foreach (LuaMember l in list.Value)
						{
							if (l is LuaFunction)
							{
                                completions.Add(new Completion(
                                    l.Name, 
                                    l.ToString(),
                                    string.Format("{0}{1}{2} {3}{4}", list.Key, dot, l.ToString(), GetMemberPath(l), l.Comment),
                                    //list.Key + dot + l.ToString() + l.Comment, 
                                    _provider.GetImageSource(l.GetType()), "icon"));
							}
						}
					}
                }
            }
            else //找不到table。拿文件单词进行提示。
            {
                var snapshot = _buffer.CurrentSnapshot;
				var tokens = FileManager.Instance.CurrentFileToken;

				var tempTable = new HashSet<String>();

                foreach (LuaMember lm in tokens)
                {
                    var point = LineAndColumnNumberToSnapshotPoint(snapshot, lm.Line, lm.Column);
                    if (point > 0)
                    {
                        char preview = snapshot[point - 1];
                        if (preview == '.' || preview == ':')
                        {
							if (tempTable.Contains(lm.Name)) continue;
							tempTable.Add(lm.Name);
							completions.Add(new Completion(
                                lm.Name, 
                                lm.Name, 
                                string.Format("{0} {1}", lm.Name, GetMemberPath(lm)),
                                //lm.Name, 
                                _provider.GetImageSource(lm.GetType()), "icon"));
                        }
                    }
                }
            }

#if DEBUG
            System.Diagnostics.Debug.Print("fill items:" + (completions.Count - count));
#endif
            return completions.Count > count;
        }

        private static SnapshotPoint LineAndColumnNumberToSnapshotPoint(ITextSnapshot snapshot, int lineNumber, int columnNumber)
        {
            var line = snapshot.GetLineFromLineNumber(lineNumber);
            var snapshotPoint = new SnapshotPoint(snapshot, line.Start + columnNumber);
            return snapshotPoint;
        }

        private string GetMemberPath(LuaMember member)
        {
            if (member.File == null) return string.Empty;
            else return string.Format("in file: {0}", member.File.Path);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}


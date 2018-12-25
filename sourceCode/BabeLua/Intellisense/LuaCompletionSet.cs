using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Intellisense
{
    class LuaCompletionSet : CompletionSet
    {
        public LuaCompletionSet(string moniker,
            string displayName,
            ITrackingSpan applicableTo,
            IEnumerable<Completion> completions,
            IEnumerable<Completion> completionBuilders)
            : base(moniker, displayName, applicableTo, completions, completionBuilders)
        {

        }

        public override IList<Completion> Completions
        {
            get
            {
                return base.Completions;
            }
        }

        public override void SelectBestMatch()
        {
            this.SelectBestMatch(CompletionMatchType.MatchDisplayText, true);
        }

        public override void Filter()
        {
            base.Filter(CompletionMatchType.MatchDisplayText, false);
        }

        public override void Recalculate()
        {
            base.Recalculate();
        }
    }
}

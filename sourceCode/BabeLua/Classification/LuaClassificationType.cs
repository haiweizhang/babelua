using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Babe.Lua.Classification
{
    internal static class OrdinaryClassificationDefinition
    {
        #region Type definition
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CType")]
        internal static ClassificationTypeDefinition CType = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("LuaFrameworkType")]
        internal static ClassificationTypeDefinition LuaFrameworkType = null;
        #endregion
    }
}

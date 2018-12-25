using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Babe.Lua.Classification
{
    #region Format definition
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "LuaFrameworkType")]
    [Name("LuaFrameworkType")]
    [UserVisible(false)]
    [Order(Before = Priority.Default)]
    internal sealed class LuaFrameworkDefinition : ClassificationFormatDefinition
    {
        /// <summary>
        /// Defines the visual format for the "ordinary" classification type
        /// </summary>
        public LuaFrameworkDefinition()
        {
            this.DisplayName = "LuaFramework"; //human readable version of the name
            this.ForegroundColor = HighlightTag.LuaFrameworkColor;
        }
    }

    /// <summary>
    /// Defines an editor format for the OrdinaryClassification type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CType")]
    [Name("CType")]
    //this should be visible to the end user
    [UserVisible(false)]
    //set the priority to be after the default classifiers
    [Order(Before = Priority.Default)]
    internal sealed class CDefinition : ClassificationFormatDefinition
    {
        /// <summary>
        /// Defines the visual format for the "ordinary" classification type
        /// </summary>
        public CDefinition()
        {
            this.DisplayName = "C"; //human readable version of the name
            this.ForegroundColor = HighlightTag.CColor;
        }
    }
    #endregion //Format definition

}

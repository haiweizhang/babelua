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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.LuaTools {
    internal static class LuaCoreConstants {
        public const string ContentType = "Lua";
        public const string BaseRegistryKey = "LuaTools";
        
        [Export, Name(ContentType), BaseDefinition("code")]
        internal static ContentTypeDefinition ContentTypeDefinition = null;

        internal static bool IsLuaContent(ITextBuffer buffer) {
            return buffer.ContentType.IsOfType(LuaCoreConstants.ContentType);
        }

        internal static bool IsLuaContent(ITextSnapshot buffer) {
            return buffer.ContentType.IsOfType(LuaCoreConstants.ContentType);
        }
    }
}

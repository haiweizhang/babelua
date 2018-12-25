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


namespace Microsoft.LuaTools {
    public static class LuaPredefinedClassificationTypeNames {
        /// <summary>
        /// Open grouping classification.  Used for (, [, {, ), ], and }...  A subtype of the Lua
        /// operator grouping.
        /// </summary>
        public const string Grouping = "Lua grouping";
       
        /// <summary>
        /// Classification used for comma characters when used outside of a literal, comment, etc...
        /// </summary>
        public const string Comma = "Lua comma";

        /// <summary>
        /// Classification used for . characters when used outside of a literal, comment, etc...
        /// </summary>
        public const string Dot = "Lua dot";

        /// <summary>
        /// Classification used for all other operators
        /// </summary>
        public const string Operator = "Lua operator";
    }
}

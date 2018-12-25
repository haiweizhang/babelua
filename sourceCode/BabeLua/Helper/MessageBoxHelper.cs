using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.LuaTools.Helper
{
	class MessageBoxHelper
	{
		public static void Show(string Title, string Content)
		{
			OLEMSGICON icon = OLEMSGICON.OLEMSGICON_CRITICAL;
			OLEMSGBUTTON buttons = OLEMSGBUTTON.OLEMSGBUTTON_OK;
			OLEMSGDEFBUTTON defaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST;
			VsShellUtilities.ShowMessageBox(null, Content, Title, icon, buttons, defaultButton);
		}
	}
}

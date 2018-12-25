using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Babe.Lua.Helper;

namespace Babe.Lua.Package
{
	class UnhandledExceptionCatcher
	{
		public UnhandledExceptionCatcher()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			System.Windows.Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

			System.Windows.Forms.Application.ThreadException += Application_ThreadException;
		}

		void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			System.Windows.MessageBox.Show("BabeLua has run into an error.", "Error");

			Logger.LogError(e.Exception);
		}

		void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			System.Windows.MessageBox.Show("BabeLua has run into an error.", "Error");
			e.Handled = true;

			Logger.LogError(e.Exception);
		}

		void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			System.Windows.MessageBox.Show("BabeLua has run into an error.", "Error");
			Logger.LogError(e.ExceptionObject as Exception);
		}
	}
}

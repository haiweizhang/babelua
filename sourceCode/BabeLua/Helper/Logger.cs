using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Babe.Lua.Package;


namespace Babe.Lua.Helper
{
	class Logger
	{
		public static void UploadLog()
		{
			UpdateUserData("run");

			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);

				if (File.Exists(path))
				{
					StreamReader reader = new StreamReader(path, new UTF8Encoding(false));

					byte[] dat = UTF8Encoding.UTF8.GetBytes("data=" + reader.ReadToEnd());
					reader.Dispose();

					System.Net.HttpWebRequest req = System.Net.WebRequest.CreateHttp("http://babelua.duapp.com/");
					req.Method = "POST";
					req.ContentLength = dat.Length;
					req.ContentType = "application/x-www-form-urlencoded";
					var resstream = req.GetRequestStream();
					resstream.Write(dat, 0, dat.Length);
					resstream.Dispose();

					req.Timeout = 3000;
					req.BeginGetResponse((ar) =>
					{
						var resp = req.EndGetResponse(ar) as System.Net.HttpWebResponse;
						byte[] buf = new byte[resp.ContentLength];
						var respstream = resp.GetResponseStream();
						respstream.Read(buf, 0, buf.Length);
						respstream.Dispose();

						if (buf.Length == 1 && buf[0] == 49)
						{
							File.Delete(path);
						}
						resp.Close();
					}, null);
				}
			}
			catch { }
			
		}

		public static void UpdateUserData(string type)
		{
			try
			{
				System.Net.HttpWebRequest req = System.Net.WebRequest.CreateHttp(string.Format("http://babelua.duapp.com/user.php?type={0}&guid={1}&version={2}", type, BabePackage.Setting.UserGUID, SettingConstants.Version));

                req.Method = "POST";
				req.ContentLength = 0;
				req.Timeout = 1000;
                req.BeginGetResponse(null, null);
				System.Diagnostics.Debug.Print("send user data:" + type);
			}
			catch { }
		}

		///<summary>
		///Log a runtime error.
		///</summary>
		public static void LogError(Exception e, bool dump = false)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);

				//string dumpname = Path.GetRandomFileName() + ".dump";

				Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();
				json["Version"] = SettingConstants.Version;
				json["Guid"] = BabePackage.Setting.UserGUID;
				json["Type"] = e.GetType().FullName;
				json["Time"] = DateTime.Now.ToString();
				json["Position"] = string.Format("{0}--->{1}", e.Source, e.TargetSite);
				json["Message"] = e.Message;
				//json["HasDump"] = dump;
				//if (dump)
				//{
				//	json["Dump"] = dumpname;
				//}
				json["StackTrace"] = e.StackTrace;



				using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false)))
				{
					writer.WriteLine(json.ToString());
					writer.WriteLine();
				}

				//if(dump)
				//{
				//	MiniDump.TryDump(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, dumpname), MiniDump.MiniDumpType.WithFullMemory);
				//}
			}
			catch { }
		}

		///<summary>
		///Log a runtime error message.
		///</summary>
		public static void LogMessage(string message)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);

				Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();
				json["Version"] = SettingConstants.Version;
				json["Guid"] = BabePackage.Setting.UserGUID;
				json["Type"] = "LogMessage";
				json["Time"] = DateTime.Now.ToString();
				json["Message"] = message;

				using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false)))
				{
					writer.WriteLine(json.ToString());
					writer.WriteLine();
				}
			}
			catch { }
		}
	}
}

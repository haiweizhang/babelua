using Babe.Lua.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Helper
{
    class Updater
    {
        public static void CheckVersion()
        {
            try
            {
                System.Net.HttpWebRequest req = System.Net.WebRequest.CreateHttp(string.Format("http://babelua.duapp.com/version"));

                req.Timeout = 1000;
                req.BeginGetResponse((ir) =>
                {
                    var resp = req.EndGetResponse(ir);
                    Version latest_version, local_version;
                    using (var reader = new StreamReader(resp.GetResponseStream()))
                    {
                        latest_version = new Version(reader.ReadToEnd());
                    }

                    local_version = new Version(SettingConstants.Version.Substring(2));

                    System.Diagnostics.Debug.Print("latest version: " + latest_version + "local version: " + local_version);

                    if(latest_version > local_version)
                    {
                        UpdateVersionGuide(local_version, latest_version);
                    }
                }, null);
            }
            catch { }
        }

        static void UpdateVersionGuide(Version from, Version to)
        {
            System.Windows.MessageBox.Show("BabeLua have a new version: " + to + ".\r\nLocal version is: " + from + ".\r\nPlease update in time.");
        }
    }
}

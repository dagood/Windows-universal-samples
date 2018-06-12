using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            FileAssociations.EnsureAssociationsSet();

            string full = string.Join(" ", args);

            if (full.StartsWith("httpredirect://"))
            {
                full = "http://" + full.Substring("httpredirect://".Length);
            }
            if (full.StartsWith("httpsredirect://"))
            {
                full = "https://" + full.Substring("httpsredirect://".Length);
            }

            Console.WriteLine("Foo: " + full);
            //Console.ReadKey();
            Process.Start(@"C:\Users\dagood\AppData\Local\Vivaldi\Application\vivaldi.exe", full);
        }
    }

    public class FileAssociation
    {
        public string Protocol { get; set; }
        public string ProgId { get; set; }
        public string FileTypeDescription { get; set; }
        public string ExecutableFilePath { get; set; }
    }

    public class FileAssociations
    {
        // needed so that Explorer windows get refreshed after the registry is updated
        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        public static void EnsureAssociationsSet()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                new FileAssociation
                {
                    Protocol = "httpredirect",
                    ProgId = "httpredirect",
                    FileTypeDescription = "URL:Http Link Redirection",
                    ExecutableFilePath = filePath
                },
                new FileAssociation
                {
                    Protocol = "httpsredirect",
                    ProgId = "httpsredirect",
                    FileTypeDescription = "URL:Https Link Redirection",
                    ExecutableFilePath = filePath
                });
        }

        public static void EnsureAssociationsSet(params FileAssociation[] associations)
        {
            bool madeChanges = false;
            foreach (var association in associations)
            {
                madeChanges |= SetAssociation(
                    association.Protocol,
                    association.ProgId,
                    association.FileTypeDescription,
                    association.ExecutableFilePath);
            }

            if (madeChanges)
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static bool SetAssociation(string protocol, string progId, string fileTypeDescription, string applicationFilePath)
        {
            bool madeChanges = false;
            //madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
            madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}", fileTypeDescription);
            madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}", "URL Protocol", "");
            madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\DefaultIcon", @"C:\Users\dagood\AppData\Local\Vivaldi\Application\vivaldi.exe,0");
            madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", "\"" + applicationFilePath + "\" \"%1\"");
            return madeChanges;
        }

        private static bool SetKeyDefaultValue(
            string keyPath,
            string value)
        {
            return SetKeyDefaultValue(keyPath, null, value);
        }

        private static bool SetKeyDefaultValue(
            string keyPath,
            string keyName,
            string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                if (key.GetValue(keyName) as string != value)
                {
                    key.SetValue(keyName, value);
                    return true;
                }
            }

            return false;
        }
    }
}

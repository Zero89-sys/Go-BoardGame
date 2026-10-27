using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;


namespace Gogame.Models
{
    public class CookieStorage
    {
        // File path
        private static readonly string _file = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Gogame", "session.json"
        );

        // Data record
        private record StoredCookie(string Name, string Value, string Path, string Domain,
            DateTime ExpiresUtc, bool Secure, bool HttpOnly);


        // Saving cookies from the container to a file
        public void Save(CookieContainer container, Uri baseUri)
        {
            // Obtaining cookies for a given URL address
            var persistent = container.GetCookies(baseUri).Cast<Cookie>()
                .Where(c => c.Expires != DateTime.MinValue && !c.Expired)
                .Select(c => new StoredCookie(c.Name, c.Value, c.Path, c.Domain,
                        c.Expires.ToUniversalTime(), c.Secure, c.HttpOnly))
                .ToList();

            if(persistent.Count == 0)
            {
                Delete();
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            File.WriteAllText(_file, JsonSerializer.Serialize(persistent));
        }

        // Loading cookies
        public void Load(CookieContainer container, Uri baseUri)
        {
            try
            {
                if (!File.Exists(_file)) return;

                var stored = JsonSerializer.Deserialize<List<StoredCookie>>(File.ReadAllText(_file));
                if (stored == null) return;

                // Validity check for each cookie
                foreach (var s in stored)
                {
                    if (s.ExpiresUtc <= DateTime.UtcNow) continue;

                    container.Add(baseUri, new Cookie(s.Name, s.Value, s.Path, s.Domain)
                    {
                        Expires = s.ExpiresUtc.ToLocalTime(),
                        Secure = s.Secure,
                        HttpOnly = s.HttpOnly
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("$Cookie load failed: {ex.Message}");
                Delete();
            }
        }

        //Deleting the cookie file
        public void Delete()
        {
            try
            {
                if (File.Exists(_file))
                {
                    File.Delete(_file);
                }
            }
            catch { }
        }
    }
}

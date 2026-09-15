using System.Security.Cryptography;

namespace OfflineDaoc.Launcher;

// Portable-only bootstrap: each extracted installation generates its own local
// credentials. The server creates the account on first successful client login.
public static class PortableCredentials
{
    public static (string Account, string Password) ReadOrCreate(string runtime)
    {
        string path = Path.Combine(runtime, "account.txt");
        if (!File.Exists(path))
        {
            // The legacy DAoC login packet has a 20-character password field.
            string content = "Account: offline\r\nPassword: " + Convert.ToHexString(RandomNumberGenerator.GetBytes(10)) + "\r\n";
            try
            {
                using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(file);
                writer.Write(content);
            }
            catch (IOException) when (File.Exists(path)) { /* Another launcher created it first. Never replace it. */ }
        }
        var values = File.ReadLines(path).Select(line => line.Split(':', 2)).Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
        if (!values.TryGetValue("Account", out string? account) || string.IsNullOrWhiteSpace(account) ||
            !values.TryGetValue("Password", out string? password) || string.IsNullOrEmpty(password))
            throw new InvalidDataException("Local account.txt is invalid. Restore it from your progress backup; do not delete existing account credentials.");
        return (account, password);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WPFLauncher.Properties;
using System.Threading.Tasks;
using System.Net.Http;

namespace WPFLauncher
{
    public class Updater
    {
        private int version = 0;

        public async Task<int> GetVersionAsync()
        {
            if (version > 0)
                return version;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(Constants.RemoteVersionUrl);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var text = await response.Content.ReadAsStringAsync();
                        version = Convert.ToInt32(text);
                    }
                }
                catch
                {
                    return 0;
                }
            }

            return version;
        }

        public async Task<bool> CheckForNewVersionAsync()
        {
            var version = await GetVersionAsync();
            var currentVersion = Settings.Default.localVersion;
            return version > currentVersion;
        }

        public async Task<List<FileData>> GetPatchlistAsync()
        {
            var remotePatchlist = new List<FileData>();
            
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(Constants.RemoteFileList);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var text = await response.Content.ReadAsStringAsync();

                        var patchlistLines = text.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                        Constants.RemoteFilePath = patchlistLines[0];

                        for (var i = 1; i < patchlistLines.Length; i++)
                        {
                            var line = patchlistLines[i].Split(new[] {'\t'}, StringSplitOptions.RemoveEmptyEntries);
                            var file = new FileData(line[0], line[1], line[2]);
                            remotePatchlist.Add(file);
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }
            return remotePatchlist;
        }
        
        private bool LocalFileExists(FileData file)
        {
            if (!File.Exists(file.FileName)) return false;
            var localHash = CalculateHash(file.FileName);
            return localHash == file.Hash;
        }

        public List<FileData> GetFilesToDownload(List<FileData> remotepatchList)
        {
            Parallel.ForEach(remotepatchList, file =>
            {
                file.FileState = LocalFileExists(file) ? FileState.Found : FileState.Missing;
            });

            return remotepatchList.Where(x => x.FileState == FileState.Missing).ToList();
        }

        public void SaveLocalVersion(int version)
        {
            Settings.Default.localVersion = version;
            Settings.Default.Save();
        }

        private string CalculateHash(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return StringifyHash(md5.ComputeHash(stream));
                }
            }
        }

        private string StringifyHash(byte[] md5)
        {
            return BitConverter.ToString(md5).Replace("-", "").ToLowerInvariant();
        }
    }
}
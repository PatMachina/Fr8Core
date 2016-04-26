﻿using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Dropbox.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Dropbox.Api.Sharing;
using terminalDropbox.Interfaces;

namespace terminalDropbox.Services
{
    public class DropboxService : IDropboxService
    {
        private const string Path = "";
        private const string UserAgent = "DockyardApp";
        private const int ReadWriteTimeout = 10 * 1000;
        private const int Timeout = 20;

        /// <summary>
        /// Gets file paths from dropbox
        /// </summary>
        /// <param name="authorizationTokenDO"></param>
        /// <returns></returns>
        public async Task<List<string>> GetFileList(AuthorizationTokenDO authorizationTokenDO)
        {
            var client = new DropboxClient(authorizationTokenDO.Token, CreateDropboxClientConfig(UserAgent));

            var result = await client.Files.ListFolderAsync(Path);

            return result.Entries.Select(x => x.PathLower).ToList();
        }

        /// <summary>
        /// Gets file shared link. If file not shared, shares it.
        /// </summary>
        /// <param name="authorizationTokenDO"></param>
        /// <param name="path">Path to file</param>
        /// <returns></returns>
        public string GetFileSharedUrl(AuthorizationTokenDO authorizationTokenDO, string path)
        {
            var client = new DropboxClient(authorizationTokenDO.Token, CreateDropboxClientConfig(UserAgent));

            // Trying to get file links
            var links = client.Sharing.ListSharedLinksAsync(path).Result.Links;
            if (links.Count > 0)
                return links[0].Url;

            // If file is not shared already, we create a sharing ulr for this file.
            var createResult = client.Sharing.CreateSharedLinkWithSettingsAsync(path).Result;
            return createResult.Url;
        }

        private static DropboxClientConfig CreateDropboxClientConfig(string userAgent)
        {
            return new DropboxClientConfig
            {
                UserAgent = userAgent,
                HttpClient = CreateHttpClient()
            };
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient(new WebRequestHandler { ReadWriteTimeout = ReadWriteTimeout })
            {
                // Specify request level timeout which decides maximum time that can be spent on
                // download/upload files.
                Timeout = TimeSpan.FromMinutes(Timeout)
            };
        }
    }
}
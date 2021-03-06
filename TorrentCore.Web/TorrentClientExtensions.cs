﻿// This file is part of TorrentCore.
//     https://torrentcore.org
// Copyright (c) 2017 Sam Fisher.
// 
// TorrentCore is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, version 3.
// 
// TorrentCore is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with TorrentCore.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace TorrentCore.Web
{
    public static class TorrentClientExtensions
    {
        public static Uri EnableWebUI(this TorrentClient client, int port)
        {
            var listenUri = new Uri($"http://localhost:{port}");
            return client.EnableWebUI(listenUri);
        }

        public static Uri EnableWebUI(this TorrentClient client, Uri listenUri)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(listenUri.ToString())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .ConfigureServices(s => { s.Add(new ServiceDescriptor(typeof(TorrentClient), client)); })
                .Build();

            host.Start();

            return listenUri;
        }
    }
}

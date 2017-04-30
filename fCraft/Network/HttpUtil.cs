// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> Static class for assisting with making web requests. </summary>
    public static class HttpUtil
    {

        // Dns lookup, to make sure that IPv4 is preferred for heartbeats
        static readonly Dictionary<string, IPAddress> TargetAddresses = new Dictionary<string, IPAddress>();
        static DateTime nextDnsLookup = DateTime.MinValue;
        static readonly TimeSpan DnsRefreshInterval = TimeSpan.FromMinutes(30);


        static IPAddress RefreshTargetAddress([NotNull] Uri requestUri)
        {
            if (requestUri == null) throw new ArgumentNullException("requestUri");

            string hostName = requestUri.Host.ToLowerInvariant();
            IPAddress targetAddress;
            if (!TargetAddresses.TryGetValue(hostName, out targetAddress) || DateTime.UtcNow >= nextDnsLookup)
            {
                try
                {
                    // Perform a DNS lookup on given host. Throws SocketException if no host found.
                    IPAddress[] allAddresses = Dns.GetHostAddresses(requestUri.Host);
                    // Find a suitable IPv4 address. Throws InvalidOperationException if none found.
                    targetAddress = allAddresses.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                }
                catch (SocketException ex)
                {
                    Logger.Log(LogType.Error,
                               "Heartbeat.RefreshTargetAddress: Error looking up heartbeat server URLs: {0}",
                               ex);
                }
                catch (InvalidOperationException)
                {
                    Logger.Log(LogType.Warning,
                               "Heartbeat.RefreshTargetAddress: {0} does not have an IPv4 address!", requestUri.Host);
                }
                TargetAddresses[hostName] = targetAddress;
                nextDnsLookup = DateTime.UtcNow + DnsRefreshInterval;
            }
            return targetAddress;
        }

        // Creates an HTTP request to the given URL
        public static HttpWebRequest CreateRequest(Uri uri, TimeSpan timeout)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(Server.BindIPEndPointCallback);
            request.Method = "GET";
            request.Timeout = (int)timeout.TotalMilliseconds;
            request.UserAgent = Updater.UserAgent;
            
            if (uri.Scheme == "http") {
                request.Proxy = new WebProxy("http://" + RefreshTargetAddress(uri) + ":" + uri.Port);
            }
            return request;
        }
    }
}
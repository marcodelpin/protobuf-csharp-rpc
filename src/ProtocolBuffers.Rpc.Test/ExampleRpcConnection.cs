using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using CSharpTest.Net.RpcLibrary;
using Google.ProtocolBuffers.Rpc;
using Google.ProtocolBuffers.Rpc.Messages;

namespace Google.ProtocolBuffers
{
    /// <summary>
    /// An example implementation that uses a URI to specify the type and properties of a connection and allows
    /// the connection to re-establish itself if the connection is lost.
    /// </summary>
    class ExampleRpcConnection : IRpcDispatch, IDisposable
    {
        private RpcClient _rpcClient;
        private readonly Guid _iid;
        private readonly Uri _connectionUri;
        private readonly string _connection;
        private readonly RpcAuthenticationType _authenticationType;
        private readonly NetworkCredential _credentials;

        public ExampleRpcConnection(Guid iid, string connection)
        {
            _iid = iid;
            _rpcClient = null;
            _connectionUri = new Uri(connection, UriKind.Absolute);
            _connection = connection;
            _authenticationType = RpcAuthenticationType.Anonymous;
            _credentials = null;
            RetryLimit = 1;
            RetryWaitTime = 0;
            ExceptionTypeResolution = RpcErrorTypeBehavior.OnlyUseLoadedAssemblies;
            
            if (StringComparer.OrdinalIgnoreCase.Equals(_connectionUri.UserInfo, "self"))
            {
                _authenticationType = RpcAuthenticationType.Self;
            }
            else if (!String.IsNullOrEmpty(_connectionUri.UserInfo))
            {
                var uri = new UriBuilder(connection);
                string domainName = "";
                string userName = Uri.UnescapeDataString(uri.UserName);
                int ixOffset = userName.LastIndexOf('\\');
                if (ixOffset >= 0)
                {
                    domainName = Uri.UnescapeDataString(userName.Substring(0, ixOffset));
                    userName = Uri.UnescapeDataString(userName.Substring(ixOffset + 1));
                }
                _credentials = new NetworkCredential(userName, Uri.UnescapeDataString(uri.Password), domainName);
                _authenticationType = RpcAuthenticationType.User;
            }
        }

        public string Connection { get { return _connection; } }
        public RpcErrorTypeBehavior ExceptionTypeResolution { get; set; }
        public int RetryLimit { get; set; }
        public int RetryWaitTime { get; set; }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            RpcClient temp = _rpcClient;
            _rpcClient = null;

            if (temp != null)
                temp.Dispose();
        }

        public TMessage CallMethod<TMessage, TBuilder>(string method, IMessageLite request, IBuilderLite<TMessage, TBuilder> response) 
            where TMessage : IMessageLite<TMessage, TBuilder> 
            where TBuilder : IBuilderLite<TMessage, TBuilder>
        {
            int retryLimit = _rpcClient == null ? Math.Max(0, RetryLimit - 1) : RetryLimit;
            int errors = 0;
            while (true)
            {
                if (_rpcClient == null)
                {
                    _rpcClient = OpenConnection();
                    _rpcClient.ExceptionTypeResolution = ExceptionTypeResolution;
                }

                try
                {
                    return _rpcClient.CallMethod(method, request, response);
                }
                catch (Exception ex)
                {
                    if (ex is RpcException || ex is ObjectDisposedException)
                    {
                        Close();

                        if (errors++ < retryLimit)
                        {
                            if (RetryWaitTime > 0)
                                System.Threading.Thread.Sleep(RetryWaitTime);
                            continue;
                        }
                    }
                    throw;
                }
            }
        }

        private RpcClient OpenConnection()
        {
            switch (_connectionUri.Scheme.ToLowerInvariant())
            {
                case "lrpc":
                {
                    var conn = RpcClient.ConnectRpc(_iid, "ncalrpc", null, _connectionUri.Segments[1])
                        .Authenticate(RpcAuthenticationType.Self);
                    return conn;
                }
                case "rpc":
                {
                    var conn = RpcClient.ConnectRpc(_iid, "ncacn_ip_tcp", _connectionUri.DnsSafeHost, _connectionUri.Port.ToString(CultureInfo.InvariantCulture));
                    if (_credentials == null)
                    {
                        conn.Authenticate(_authenticationType);
                    }
                    else
                    {
                        conn.Authenticate(_authenticationType, _credentials);
                    }
                    return conn;
                }
                case "np":
                {
                    string pipe = _connectionUri.PathAndQuery;
                    if (pipe.StartsWith("/pipe/", StringComparison.Ordinal) == false)
                        throw new ArgumentException("Named pipe connections must start with the '/pipe/' prefix.");
                    pipe = pipe.Replace('/', '\\');
                    var conn = RpcClient.ConnectRpc(_iid, "ncacn_np", _connectionUri.DnsSafeHost, pipe);
                    if (_credentials == null)
                    {
                        conn.Authenticate(_authenticationType);
                    }
                    else
                    {
                        conn.Authenticate(_authenticationType, _credentials);
                    }
                    return conn;
                }
                default:
                    throw new ArgumentOutOfRangeException("connection", "The connection has an invalid URI Scheme.");
            }
        }
    }
}

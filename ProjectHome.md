A small rpc library for using Google's ProtoBuffer services over Win32 RPC on LRPC/TCP/Named Pipes.  Integrates with NTLM/Kerberos authentication.

## LRPC Example ##
**Protocol buffer**
```
import "google/protobuf/csharp_options.proto";
import "google/protobuf/csharp_rpc_messages.proto";
option (google.protobuf.csharp_file_options).service_generator_type = IRPCDISPATCH;

option optimize_for = SPEED;

message SearchRequest {
  repeated string Criteria = 1;
}

message SearchResponse {
  message ResultItem {
    required string url = 1;
    optional string name = 2;
  }
  repeated ResultItem results = 1;
}

service SearchService {
  rpc Search (SearchRequest) returns (SearchResponse);
}
```
**Server-side**
```

    //obtain the interface id for rpc registration
    Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
    //Create the server with a stub pointing to our implementation
    using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
        //allow GSS_NEGOTIATE
        .AddAuthNegotiate()
        //LRPC named 'lrpctest'
        .AddProtocol("ncalrpc", "lrpctest")
        //Begin responding
        .StartListening())
    {
        //Wait for connections
        Console.ReadLine();
    }
```

**Client-side**
```
    //obtain the interface id for rpc registration
    Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
    //Create the rpc client connection and give it to the new SearchService
    using (SearchService client = new SearchService(
            RpcClient.ConnectRpc(iid, "ncalrpc", null, "lrpctest")
            .Authenticate(RpcAuthenticationType.Self)))
    {
        //Create the request:
        var request = SearchRequest.CreateBuilder().AddCriteria("Test Criteria").Build();

        //Just call the service:
        SearchResponse results = client.Search(request);
    }
```

For more information see: [Replacing WCF with RPC and Protobuffers](http://csharptest.net/1177/wcf-replacement-for-cross-processmachine-communication)
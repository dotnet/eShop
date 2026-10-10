using Grpc.Core;

namespace eShop.Basket.UnitTests.Helpers;

public class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _requestHeaders;
        private readonly CancellationToken _cancellationToken;
        private readonly Metadata _responseTrailers;
        private readonly AuthContext _authContext;
        private readonly Dictionary<object, object> _userState;
        private WriteOptions _writeOptions;

        public Metadata ResponseHeaders { get; private set; }

        private TestServerCallContext(Metadata requestHeaders, CancellationToken cancellationToken)
        {
            _requestHeaders = requestHeaders;
            _cancellationToken = cancellationToken;
            _responseTrailers = new Metadata();
            _authContext = new AuthContext(string.Empty, new Dictionary<string, List<AuthProperty>>());
            _userState = new Dictionary<object, object>();
        }

        protected override string MethodCore => "MethodName";
        protected override string HostCore => "HostName";
        protected override string PeerCore => "PeerName";
        protected override DateTime DeadlineCore { get; }
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => _cancellationToken;
        protected override Metadata ResponseTrailersCore => _responseTrailers;
        protected override Status StatusCore { get; set; }
        protected override WriteOptions WriteOptionsCore { get => _writeOptions; set { _writeOptions = value; } }
        protected override AuthContext AuthContextCore => _authContext;

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options)
        {
            throw new NotImplementedException();
        }

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            if (ResponseHeaders != null)
            {
                throw new InvalidOperationException("Response headers have already been written.");
            }

            ResponseHeaders = responseHeaders;
            return Task.CompletedTask;
        }

        protected override IDictionary<object, object> UserStateCore => _userState;

        internal void SetUserState(object key, object value)
            => _userState[key] = value;

        public static TestServerCallContext Create(Metadata requestHeaders = null, CancellationToken cancellationToken = default)
        {
            return new TestServerCallContext(requestHeaders: new Metadata(), cancellationToken);
        }
    }

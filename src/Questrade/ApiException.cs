using System;
using System.Net.Http;

namespace PollQT.Questrade
{
    internal class ApiException : Exception
    {
        public ApiException(string message, Exception innerException) : base(message, innerException) { }
        public ApiException(string message) : base(message) { }
        public ApiException() : base() { }
    }

    internal class UnauthorizedException : ApiException
    {
        public UnauthorizedException() : base() { }
        public UnauthorizedException(string message) : base(message) { }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class UnexpectedStatusException : ApiException
    {
        public HttpResponseMessage HttpResponse { get; }
        public UnexpectedStatusException(HttpResponseMessage httpResponse) => HttpResponse = httpResponse;
    }
}

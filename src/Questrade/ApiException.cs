using System;
using System.Net.Http;

namespace PollQT.Questrade
{
    class ApiException : Exception
    {
        public ApiException(string message, Exception innerException) : base(message, innerException) { }
        public ApiException(string message) : base(message) { }
        public ApiException() : base() { }
    }
    class UnauthorizedException : ApiException
    {
        public UnauthorizedException() : base() { }
        public UnauthorizedException(string message) : base(message) { }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
    }
    class UnexpectedStatusException : ApiException
    {
        public HttpResponseMessage HttpResponse { get; }
        public UnexpectedStatusException(HttpResponseMessage httpResponse) => HttpResponse = httpResponse;
    }
}

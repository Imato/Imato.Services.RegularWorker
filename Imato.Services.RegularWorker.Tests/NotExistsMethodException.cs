namespace Imato.Services.RegularWorker.Tests
{
    public class NotExistsMethodException<T> : ApplicationException
    {
        public NotExistsMethodException(string methodName)
            : base($"Method {methodName} not exists in type {typeof(T).Name}")
        {
        }
    }
}
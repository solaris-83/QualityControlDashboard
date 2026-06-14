namespace QualityControl.WPF.Exceptions
{
    public class ApplicationBaseException : Exception
    {
        public string Code { get; }

        protected ApplicationBaseException(string code, string message) : base(message)
        {
            Code = code;
        }
    }
}

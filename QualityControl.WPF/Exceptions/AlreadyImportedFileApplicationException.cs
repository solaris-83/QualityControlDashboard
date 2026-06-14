

namespace QualityControl.WPF.Exceptions
{
    public class AlreadyImportedFileApplicationException : ApplicationBaseException
    {
        public AlreadyImportedFileApplicationException(string message) : base("ALREADY_IMPORTED_FILE", message)
        {
        }
    }
}

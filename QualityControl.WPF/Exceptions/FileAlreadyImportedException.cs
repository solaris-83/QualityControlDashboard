

namespace QualityControl.WPF.Exceptions
{
    public class FileAlreadyImportedException : Exception
    {
        public FileAlreadyImportedException()
        {
        }

        public FileAlreadyImportedException(string message) : base(message)
        {
        }
    }
}

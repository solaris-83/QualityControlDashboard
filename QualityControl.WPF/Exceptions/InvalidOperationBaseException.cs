using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControl.WPF.Exceptions
{
    internal class InvalidOperationBaseException : ApplicationBaseException
    {
        public InvalidOperationBaseException(string message) : base("INVALID_OPERATION", message)
        {
        }
    }
}

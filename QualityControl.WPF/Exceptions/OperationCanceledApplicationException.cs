using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControl.WPF.Exceptions
{
    internal class OperationCanceledApplicationException : ApplicationBaseException
    {
        public OperationCanceledApplicationException(string message) : base("OPERATION_CANCELED", message)
        {
            
        }
    }
}

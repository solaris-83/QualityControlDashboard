using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControl.WPF.Exceptions
{
    public class DatabaseApplicationException : ApplicationBaseException
    {
        public DatabaseApplicationException(string message) : base("DATABASE_ERROR", message)
        {
        }
    }
}

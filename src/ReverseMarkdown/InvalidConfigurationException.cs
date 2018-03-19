using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseMarkdown
{
    public class InvalidConfigurationException: Exception
    {
        public InvalidConfigurationException(string message)
            :base(message)
        {
        }
    }
}

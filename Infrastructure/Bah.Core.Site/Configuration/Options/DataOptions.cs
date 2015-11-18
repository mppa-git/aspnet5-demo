using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Configuration.Options
{
    public class ConnectionOptions
    {
        public string ConnectionString { get; set; }
    }

    public class DataOptions
    {
        public ConnectionOptions DefaultConnection { get; set; }
    }
}

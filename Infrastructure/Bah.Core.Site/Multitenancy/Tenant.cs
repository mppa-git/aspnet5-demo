using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Multitenancy
{
    public interface ITenant
    {
        string Name { get; }
        string DbConnectionString { get; }
    }

    public class Tenant : ITenant
    {
        public string Name { get; set; }
        public string DbConnectionString { get; set; }
    }
}

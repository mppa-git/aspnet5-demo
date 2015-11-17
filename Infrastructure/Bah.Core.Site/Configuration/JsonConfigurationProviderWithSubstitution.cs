using Bah.Core.Site.Utils;
using Microsoft.Framework.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bah.Core.Site.Configuration
{
    public class JsonConfigurationProviderWithSubstitution : JsonConfigurationProvider
    {
        public JsonConfigurationProviderWithSubstitution(string path, bool optional, object variables)
            : base(path, optional)
        {
            this.Variables = variables;
        }

        private object Variables;
        public override void Load()
        {
            base.Load();

            if (this.Variables == null)
                return;

            var keys = new List<string>(this.Data.Keys);
            foreach (var key in keys)
            {
                var value = this.Data[key];
                if (value == null) continue;
                var newValue = FormattableObject.Format(Variables, value, null);
                this.Data[key] = newValue;
            }
        }
    }
}

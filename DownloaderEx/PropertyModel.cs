using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderEx
{
    public class PropertyModel
    {
        private string _name = String.Empty;
        private string _value = String.Empty;

        public PropertyModel(string name, string value)
        {
            _name = name;
            _value = value;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Value
        {
            get { return _value; }
        }
    }
}

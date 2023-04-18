using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.MVVM.Model
{
    public class DataViewProperty
    {
        public string PropertyName { get; set; }
        public object PropertyValue { get; set; }
        public DataViewProperty(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}

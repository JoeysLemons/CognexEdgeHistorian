using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.MVVM.Models
{
    public class Tag
    {
		public string Name { get; set; }
		public int UpdateRate { get; set; }
		public bool LimitChecking { get; }
		public bool PassFail { get; }
		public string DataType { get; set; }
		public object Value { get; set; }
		public string Location { get; set; }
		public List<string> Filters { get; set; }
		public object AssociatedImage { get; set; }
		public int ID { get; set; }
		private string _browseName;

		public string BrowseName
		{
			get { return _browseName; }
		}

		public void SetBrowseName(string browseName)
		{
			_browseName = browseName;
		}


		protected Tag(string name)
		{
			Name = name;
		}

	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeMonitoringService.Models
{
    public class Tag
    {
		public string Name { get; set; }
		public int ID { get; set; }
		public int UpdateRate { get; set; }
		public bool LimitChecking { get; }
		public bool PassFail { get; }
		public string DataType { get; set; }
		public object Value { get; set; }
		public string Location { get; set; }
		public List<string> Filters { get; set; }
		public string AssociatedImageName { get; set; }
		public string NodeId { get; set; }
		public string Timestamp { get; set; }
		private string _browseName;
		public List<Tag> Children 
		{ 
			get; 
			set; 
		} = new List<Tag>();
		public string BrowseName
		{
			get { return _browseName; }
		}

		public void SetBrowseName(string browseName)
		{
			_browseName = browseName;
		}


		public Tag(string name, string nodeId)
		{
			Name = name;
			NodeId = nodeId;
		}

		public Tag(Tag copyTag)
		{
			this.ID = copyTag.ID;
			this.Name = copyTag.Name;
			this.AssociatedImageName = copyTag.AssociatedImageName;
			this.Value = copyTag.Value;
			this.Timestamp = copyTag.Timestamp;
		}
    }
}

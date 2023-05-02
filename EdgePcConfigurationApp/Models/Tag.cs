using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EdgePcConfigurationApp.Models
{
    public class Tag : ObservableObject
    {
		public ICommand SubscribeTagCommand;
		public string Name { get; set; }
		public int UpdateRate { get; set; }
		public bool LimitChecking { get; }
		public bool PassFail { get; }
		public string DataType { get; set; }
		public object Value { get; set; }
		public string Location { get; set; }
		public int TagId { get; set; }
		public List<Tag> Children 
		{ 
			get; 
			set; 
		} = new List<Tag>();

		public bool IsChecked { get; set; } = false;

		public string NodeId { get; set; }
		public string CameraName { get; set; }
		public string Timestamp { get; set; }
		private string _browseName;
		public string BrowseName
		{
			get { return _browseName; }
		}

		public void SetBrowseName(string browseName)
		{
			_browseName = browseName;
		}

        public Tag(string name, string id, string cameraName)
		{
			Name = name;
			NodeId = id;
			CameraName = cameraName;
		}
    }
}

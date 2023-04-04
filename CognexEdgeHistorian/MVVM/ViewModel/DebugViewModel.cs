using CognexEdgeHistorian.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class DebugViewModel : ViewModelBase
    {
		public ICommand ConnectToCamera { get; }
        public ICommand DisconnectFromCamera { get; }

		public Session session { get; set; }
		private string _endpoint;

		public string Endpoint
		{
			get { return _endpoint; }
			set 
			{
                    _endpoint = value;
					OnPropertyChanged(nameof(Endpoint));
			}
		}

        public void Disconnect(object parameter)
        {
            session?.Dispose();
        }

		public async void Connect(object parameter)
		{
            var config = OPCUAUtils.CreateApplicationConfiguration();
            await OPCUAUtils.InitializeApplication();
            session = await OPCUAUtils.ConnectToServer(config, $"opc.tcp://{Endpoint}");

            ReferenceDescriptionCollection references;
            Byte[] continuationPoint;

            session.Browse(
                null,
                null,
                ObjectIds.ObjectsFolder,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                uint.MaxValue,
                out continuationPoint,
                out references);

            await BrowseChildren(session, references);

            MessageBox.Show("Connected");
        }

        private static async Task BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {
            foreach (var reference in references)
            {
                Console.WriteLine($"DisplayName: {reference.DisplayName}, NodeId: {reference.NodeId}");

                ReferenceDescriptionCollection childReferences;
                Byte[] continuationPoint;

                session.Browse(
                    null,
                    null,
                    ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris),
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    uint.MaxValue,
                    out continuationPoint,
                    out childReferences);

                if (childReferences.Count > 0)
                {
                    await BrowseChildren(session, childReferences);
                }
            }
        }

        public DebugViewModel()
		{
			ConnectToCamera = new RelayCommand(Connect);
            DisconnectFromCamera = new RelayCommand(Disconnect);
		}


    }
}

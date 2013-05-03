﻿using System;
using System.Linq;
using System.Collections.Generic;

using OpenHome.Os.App;
using OpenHome.Net.ControlPoint.Proxies;
using OpenHome.Os;

namespace OpenHome.Av
{
    /*public interface IServiceOpenHomeOrgRadio1
    {
        IWatchable<uint> Id { get; }
        IWatchable<uint> IdArray { get; }
        IWatchable<string> Metadata { get; }
        IWatchable<string> TransportState { get; }
        IWatchable<string> Uri { get; }
    }

    public interface IReceiver : IServiceOpenHomeOrgReceiver1
    {
        string ProtocolInfo { get; }
    }

    public abstract class Receiver : IReceiver, IWatchableService
    {
        protected Receiver(string aId)
        {
            iId = aId;
        }

        public abstract void Dispose();

        public IService Create(IManagableWatchableDevice aDevice)
        {
            return new ServiceReceiver(aDevice, this);
        }

        public string Id
        {
            get
            {
                return iId;
            }
        }

        internal abstract IServiceOpenHomeOrgReceiver1 Service { get; }

        public IWatchable<string> Metadata
        {
            get
            {
                return Service.Metadata;
            }
        }

        public IWatchable<string> TransportState
        {
            get
            {
                return Service.TransportState;
            }
        }

        public IWatchable<string> Uri
        {
            get
            {
                return Service.Uri;
            }
        }

        public string ProtocolInfo
        {
            get
            {
                return iProtocolInfo;
            }
        }

        private string iId;

        protected string iProtocolInfo;
    }

    public class ServiceOpenHomeOrgReceiver1 : IServiceOpenHomeOrgReceiver1
    {
        public ServiceOpenHomeOrgReceiver1(IWatchableThread aThread, string aId, CpProxyAvOpenhomeOrgReceiver1 aService)
        {
            iLock = new object();
            iDisposed = false;

            lock (iLock)
            {
                iService = aService;

                iService.SetPropertyMetadataChanged(HandleMetadataChanged);
                iService.SetPropertyTransportStateChanged(HandleTransportStateChanged);
                iService.SetPropertyUriChanged(HandleUriChanged);

                iMetadata = new Watchable<string>(aThread, string.Format("Metadata({0})", aId), iService.PropertyMetadata());
                iTransportState = new Watchable<string>(aThread, string.Format("TransportState({0})", aId), iService.PropertyTransportState());
                iUri = new Watchable<string>(aThread, string.Format("Uri({0})", aId), iService.PropertyUri());
            }
        }

        public void Dispose()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgReceiver1.Dispose");
                }

                iService.Dispose();
                iService = null;

                iMetadata.Dispose();
                iMetadata = null;

                iTransportState.Dispose();
                iTransportState = null;

                iUri.Dispose();
                iUri = null;

                iDisposed = true;
            }
        }

        public IWatchable<string> Metadata
        {
            get
            {
                return iMetadata;
            }
        }

        public IWatchable<string> TransportState
        {
            get
            {
                return iTransportState;
            }
        }

        public IWatchable<string> Uri
        {
            get
            {
                return iUri;
            }
        }

        private void HandleMetadataChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iMetadata.Update(iService.PropertyMetadata());
            }
        }

        private void HandleTransportStateChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iTransportState.Update(iService.PropertyTransportState());
            }
        }

        private void HandleUriChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iUri.Update(iService.PropertyUri());
            }
        }

        private object iLock;
        private bool iDisposed;

        private CpProxyAvOpenhomeOrgReceiver1 iService;

        private Watchable<string> iMetadata;
        private Watchable<string> iTransportState;
        private Watchable<string> iUri;
    }

    public class MockServiceOpenHomeOrgReceiver1 : IServiceOpenHomeOrgReceiver1, IMockable
    {
        public MockServiceOpenHomeOrgReceiver1(IWatchableThread aThread, string aId, string aMetadata, string aTransportState, string aUri)
        {
            iMetadata = new Watchable<string>(aThread, string.Format("Metadata({0})", aId), aMetadata);
            iTransportState = new Watchable<string>(aThread, string.Format("TransportState({0})", aId), aTransportState);
            iUri = new Watchable<string>(aThread, string.Format("Uri({0})", aId), aUri);
        }

        public void Dispose()
        {
            iMetadata.Dispose();
            iMetadata = null;

            iTransportState.Dispose();
            iTransportState = null;

            iUri.Dispose();
            iUri = null;
        }

        public IWatchable<string> Metadata
        {
            get
            {
                return iMetadata;
            }
        }

        public IWatchable<string> TransportState
        {
            get
            {
                return iTransportState;
            }
        }

        public IWatchable<string> Uri
        {
            get
            {
                return iUri;
            }
        }

        public void Execute(IEnumerable<string> aValue)
        {
            string command = aValue.First().ToLowerInvariant();
            if (command == "metadata")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iMetadata.Update(value.First());
            }
            else if (command == "transportstate")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iTransportState.Update(value.First());
            }
            else if (command == "uri")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iUri.Update(value.First());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private Watchable<string> iMetadata;
        private Watchable<string> iTransportState;
        private Watchable<string> iUri;
    }

    public class WatchableReceiverFactory : IWatchableServiceFactory
    {
        public WatchableReceiverFactory(IWatchableThread aThread, IWatchableThread aSubscribeThread)
        {
            iLock = new object();
            iThread = aThread;

            iSubscribeThread = aSubscribeThread;
        }

        public void Subscribe(IWatchableDevice aDevice, Action<IWatchableService> aCallback)
        {
            iSubscribeThread.Schedule(() =>
            {
                if (iService == null && iPendingService == null)
                {
                    WatchableDevice d = aDevice as WatchableDevice;
                    iPendingService = new CpProxyAvOpenhomeOrgReceiver1(d.Device);
                    iPendingService.SetPropertyInitialEvent(delegate
                    {
                        lock (iLock)
                        {
                            if (iPendingService != null)
                            {
                                iService = new WatchableReceiver(iThread, string.Format("Receiver({0})", aDevice.Udn), iPendingService);
                                iPendingService = null;
                                aCallback(iService);
                            }
                        }
                    });
                    iPendingService.Subscribe();
                }
            });
        }

        public void Unsubscribe()
        {
            iSubscribeThread.Schedule(() =>
            {
                lock (iLock)
                {
                    if (iPendingService != null)
                    {
                        iPendingService.Dispose();
                        iPendingService = null;
                    }

                    if (iService != null)
                    {
                        iService.Dispose();
                        iService = null;
                    }
                }
            });
        }

        private object iLock;
        private IWatchableThread iSubscribeThread;
        private CpProxyAvOpenhomeOrgReceiver1 iPendingService;
        private WatchableReceiver iService;
        private IWatchableThread iThread;
    }

    public class WatchableReceiver : Receiver
    {
        public WatchableReceiver(IWatchableThread aThread, string aId, CpProxyAvOpenhomeOrgReceiver1 aService)
            : base(aId)
        {
            iProtocolInfo = aService.PropertyProtocolInfo();

            iService = new ServiceOpenHomeOrgReceiver1(aThread, aId, aService);
        }

        public override void Dispose()
        {
            iService.Dispose();
            iService = null;
        }

        internal override IServiceOpenHomeOrgReceiver1 Service
        {
            get
            {
                return iService;
            }
        }

        private ServiceOpenHomeOrgReceiver1 iService;
    }

    public class MockWatchableReceiver : Receiver, IMockable
    {
        public MockWatchableReceiver(IWatchableThread aThread, string aId, string aMetadata, string aProtocolInfo, string aTransportState, string aUri)
            : base(aId)
        {
            iProtocolInfo = aProtocolInfo;

            iService = new MockServiceOpenHomeOrgReceiver1(aThread, aId, aMetadata, aTransportState, aUri);
        }

        public override void Dispose()
        {
            iService.Dispose();
            iService = null;
        }

        internal override IServiceOpenHomeOrgReceiver1 Service
        {
            get
            {
                return iService;
            }
        }

        public void Execute(IEnumerable<string> aValue)
        {
            string command = aValue.First().ToLowerInvariant();
            if (command == "metadata" || command == "transportstate" || command == "uri")
            {
                iService.Execute(aValue);
            }
            else if (command == "protocolinfo")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iProtocolInfo = string.Join(" ", value);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private MockServiceOpenHomeOrgReceiver1 iService;
    }

    public class ServiceReceiver : IReceiver, IService
    {
        public ServiceReceiver(IManagableWatchableDevice aDevice, IReceiver aService)
        {
            iDevice = aDevice;
            iService = aService;
        }

        public void Dispose()
        {
            iDevice.Unsubscribe<ServiceReceiver>();
            iDevice = null;
        }

        public IWatchableDevice Device
        {
            get { return iDevice; }
        }

        public string ProtocolInfo
        {
            get { return iService.ProtocolInfo; }
        }

        public IWatchable<string> Metadata
        {
            get { return iService.Metadata; }
        }

        public IWatchable<string> TransportState
        {
            get { return iService.TransportState; }
        }

        public IWatchable<string> Uri
        {
            get { return iService.Uri; }
        }

        private IManagableWatchableDevice iDevice;
        private IReceiver iService;
    }*/
}

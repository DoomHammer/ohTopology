﻿using System;
using System.Linq;
using System.Collections.Generic;

using OpenHome.Os.App;
using OpenHome.Net.ControlPoint.Proxies;
using OpenHome.Os;

namespace OpenHome.Av
{
    public interface IServiceOpenHomeOrgVolume1
    {
        IWatchable<int> Balance { get; }
        IWatchable<uint> BalanceMax { get; }
        IWatchable<int> Fade { get; }
        IWatchable<uint> FadeMax { get; }
        IWatchable<bool> Mute { get; }
        IWatchable<uint> Value { get; }
        IWatchable<uint> VolumeLimit { get; }
        IWatchable<uint> VolumeMax { get; }
        IWatchable<uint> VolumeMilliDbPerStep { get; }
        IWatchable<uint> VolumeSteps { get; }
        IWatchable<uint> VolumeUnity { get; }

        void SetBalance(int aValue);
        void SetFade(int aValue);
        void SetMute(bool aValue);
        void SetVolume(uint aValue);
        void VolumeDec();
        void VolumeInc();
    }

    public abstract class Volume : IServiceOpenHomeOrgVolume1, IWatchableService
    {

        protected Volume(string aId, IWatchableDevice aDevice)
        {
            iId = aId;
            iDevice = aDevice;
        }

        public abstract void Dispose();

        public IService Create(IManagableWatchableDevice aDevice)
        {
            return new ServiceVolume(aDevice, this);
        }

        internal abstract IServiceOpenHomeOrgVolume1 Service { get; }

        public string Id
        {
            get
            {
                return iId;
            }
        }

        public IWatchableDevice Device
        {
            get
            {
                return iDevice;
            }
        }

        public IWatchable<int> Balance
        {
            get
            {
                return Service.Balance;
            }
        }

        public IWatchable<uint> BalanceMax
        {
            get
            {
                return Service.BalanceMax;
            }
        }

        public IWatchable<int> Fade
        {
            get
            {
                return Service.Fade;
            }
        }

        public IWatchable<uint> FadeMax
        {
            get
            {
                return Service.FadeMax;
            }
        }

        public IWatchable<bool> Mute
        {
            get
            {
                return Service.Mute;
            }
        }

        public IWatchable<uint> Value
        {
            get
            {
                return Service.Value;
            }
        }

        public IWatchable<uint> VolumeLimit
        {
            get
            {
                return Service.VolumeLimit;
            }
        }

        public IWatchable<uint> VolumeMax
        {
            get
            {
                return Service.VolumeMax;
            }
        }

        public IWatchable<uint> VolumeMilliDbPerStep
        {
            get
            {
                return Service.VolumeMilliDbPerStep;
            }
        }

        public IWatchable<uint> VolumeSteps
        {
            get
            {
                return Service.VolumeSteps;
            }
        }

        public IWatchable<uint> VolumeUnity
        {
            get
            {
                return Service.VolumeUnity;
            }
        }

        public void SetBalance(int aValue)
        {
            Service.SetBalance(aValue);
        }

        public void SetFade(int aValue)
        {
            Service.SetFade(aValue);
        }

        public void SetMute(bool aValue)
        {
            Service.SetMute(aValue);
        }

        public void SetVolume(uint aValue)
        {
            Service.SetVolume(aValue);
        }

        public void VolumeDec()
        {
            Service.VolumeDec();
        }

        public void VolumeInc()
        {
            Service.VolumeInc();
        }

        private string iId;
        private IWatchableDevice iDevice;
    }

    public class ServiceOpenHomeOrgVolume1 : IServiceOpenHomeOrgVolume1, IDisposable
    {
        public ServiceOpenHomeOrgVolume1(IWatchableThread aThread, string aId, CpProxyAvOpenhomeOrgVolume1 aService)
        {
            iLock = new object();
            iDisposed = false;

            lock (iLock)
            {
                iService = aService;

                iService.SetPropertyBalanceChanged(HandleBalanceChanged);
                iService.SetPropertyBalanceMaxChanged(HandleBalanceMaxChanged);
                iService.SetPropertyFadeChanged(HandleFadeChanged);
                iService.SetPropertyFadeMaxChanged(HandleFadeMaxChanged);
                iService.SetPropertyMuteChanged(HandleMuteChanged);
                iService.SetPropertyVolumeChanged(HandleVolumeChanged);
                iService.SetPropertyVolumeLimitChanged(HandleVolumeLimitChanged);
                iService.SetPropertyVolumeMaxChanged(HandleVolumeMaxChanged);
                iService.SetPropertyVolumeMilliDbPerStepChanged(HandleVolumeMilliDbPerStepChanged);
                iService.SetPropertyVolumeStepsChanged(HandleVolumeStepsChanged);
                iService.SetPropertyVolumeUnityChanged(HandleVolumeUnityChanged);

                iBalance = new Watchable<int>(aThread, string.Format("Balance({0})", aId), iService.PropertyBalance());
                iBalanceMax = new Watchable<uint>(aThread, string.Format("BalanceMax({0})", aId), iService.PropertyBalanceMax());
                iFade = new Watchable<int>(aThread, string.Format("Fade({0})", aId), iService.PropertyFade());
                iFadeMax = new Watchable<uint>(aThread, string.Format("FadeMax({0})", aId), iService.PropertyFadeMax());
                iMute = new Watchable<bool>(aThread, string.Format("Mute({0})", aId), iService.PropertyMute());
                iValue = new Watchable<uint>(aThread, string.Format("Value({0})", aId), iService.PropertyVolume());
                iVolumeLimit = new Watchable<uint>(aThread, string.Format("VolumeLimit({0})", aId), iService.PropertyVolumeLimit());
                iVolumeMax = new Watchable<uint>(aThread, string.Format("VolumeMax({0})", aId), iService.PropertyVolumeMax());
                iVolumeMilliDbPerStep = new Watchable<uint>(aThread, string.Format("VolumeMilliDbPerStep({0})", aId), iService.PropertyVolumeMilliDbPerStep());
                iVolumeSteps = new Watchable<uint>(aThread, string.Format("VolumeSteps({0})", aId), iService.PropertyVolumeSteps());
                iVolumeUnity = new Watchable<uint>(aThread, string.Format("VolumeUnity({0})", aId), iService.PropertyVolumeUnity());
            }
        }

        public void Dispose()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.Dispose");
                }

                iService.Dispose();
                iService = null;

                iBalance.Dispose();
                iBalance = null;

                iBalanceMax.Dispose();
                iBalanceMax = null;

                iFade.Dispose();
                iFade = null;

                iFadeMax.Dispose();
                iFadeMax = null;

                iMute.Dispose();
                iMute = null;

                iValue.Dispose();
                iValue = null;

                iVolumeLimit.Dispose();
                iVolumeLimit = null;

                iVolumeMax.Dispose();
                iVolumeMax = null;

                iVolumeMilliDbPerStep.Dispose();
                iVolumeMilliDbPerStep = null;

                iVolumeSteps.Dispose();
                iVolumeSteps = null;

                iVolumeUnity.Dispose();
                iVolumeUnity = null;

                iDisposed = true;
            }
        }

        public IWatchable<int> Balance
        {
            get
            {
                return iBalance;
            }
        }

        public IWatchable<uint> BalanceMax
        {
            get
            {
                return iBalanceMax;
            }
        }

        public IWatchable<int> Fade
        {
            get
            {
                return iFade;
            }
        }

        public IWatchable<uint> FadeMax
        {
            get
            {
                return iFadeMax;
            }
        }

        public IWatchable<bool> Mute
        {
            get
            {
                return iMute;
            }
        }

        public IWatchable<uint> Value
        {
            get{
                return iValue;
            }
        }

        public IWatchable<uint> VolumeLimit
        {
            get
            {
                return iVolumeLimit;
            }
        }

        public IWatchable<uint> VolumeMax
        {
            get
            {
                return iVolumeMax;
            }
        }

        public IWatchable<uint> VolumeMilliDbPerStep
        {
            get
            {
                return iVolumeMilliDbPerStep;
            }
        }

        public IWatchable<uint> VolumeSteps
        {
            get
            {
                return iVolumeSteps;
            }
        }

        public IWatchable<uint> VolumeUnity
        {
            get
            {
                return iVolumeUnity;
            }
        }

        public void SetBalance(int aValue)
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.SetBalance");
                }

                iService.BeginSetBalance(aValue, null);
            }
        }

        public void SetFade(int aValue)
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.SetFade");
                }

                iService.BeginSetFade(aValue, null);
            }
        }

        public void SetMute(bool aValue)
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.SetMute");
                }

                iService.BeginSetMute(aValue, null);
            }
        }

        public void SetVolume(uint aValue)
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.SetVolume");
                }

                iService.BeginSetVolume(aValue, null);
            }
        }

        public void VolumeDec()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.VolumeDec");
                }

                iService.BeginVolumeDec(null);
            }
        }

        public void VolumeInc()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceOpenHomeOrgVolume1.VolumeInc");
                }

                iService.BeginVolumeInc(null);
            }
        }

        private void HandleVolumeUnityChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iVolumeUnity.Update(iService.PropertyVolumeUnity());
            }
        }

        private void HandleVolumeStepsChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iVolumeSteps.Update(iService.PropertyVolumeSteps());
            }
        }

        private void HandleVolumeMilliDbPerStepChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iVolumeMilliDbPerStep.Update(iService.PropertyVolumeMilliDbPerStep());
            }
        }

        private void HandleVolumeMaxChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iVolumeMax.Update(iService.PropertyVolumeMax());
            }
        }

        private void HandleVolumeLimitChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iVolumeLimit.Update(iService.PropertyVolumeLimit());
            }
        }

        private void HandleVolumeChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iValue.Update(iService.PropertyVolume());
            }
        }

        private void HandleMuteChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iMute.Update(iService.PropertyMute());
            }
        }

        private void HandleFadeMaxChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iFadeMax.Update(iService.PropertyFadeMax());
            }
        }

        private void HandleFadeChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iFade.Update(iService.PropertyFade());
            }
        }

        private void HandleBalanceMaxChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iBalanceMax.Update(iService.PropertyBalanceMax());
            }
        }

        private void HandleBalanceChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iBalance.Update(iService.PropertyBalance());
            }
        }

        private bool iDisposed;
        private object iLock;

        private CpProxyAvOpenhomeOrgVolume1 iService;

        private Watchable<int> iBalance;
        private Watchable<uint> iBalanceMax;
        private Watchable<int> iFade;
        private Watchable<uint> iFadeMax;
        private Watchable<bool> iMute;
        private Watchable<uint> iValue;
        private Watchable<uint> iVolumeLimit;
        private Watchable<uint> iVolumeMax;
        private Watchable<uint> iVolumeMilliDbPerStep;
        private Watchable<uint> iVolumeSteps;
        private Watchable<uint> iVolumeUnity;
    }

    public class MockServiceOpenHomeOrgVolume1 : IServiceOpenHomeOrgVolume1, IMockable, IDisposable
    {
        public MockServiceOpenHomeOrgVolume1(IWatchableThread aThread, string aId, int aBalance, uint aBalanceMax, int aFade, uint aFadeMax, bool aMute, uint aValue, uint aVolumeLimit, uint aVolumeMax,
            uint aVolumeMilliDbPerStep, uint aVolumeSteps, uint aVolumeUnity)
        {
            uint volumeLimit = aVolumeLimit;
            if (volumeLimit > aVolumeMax)
            {
                volumeLimit = aVolumeMax;
            }
            iCurrentVolumeLimit = volumeLimit;

            uint value = aValue;
            if (value > aVolumeLimit)
            {
                value = aVolumeLimit;
            }
            iCurrentVolume = value;

            iBalance = new Watchable<int>(aThread, string.Format("Balance({0})", aId), aBalance);
            iBalanceMax = new Watchable<uint>(aThread, string.Format("BalanceMax({0})", aId), aBalanceMax);
            iFade = new Watchable<int>(aThread, string.Format("Fade({0})", aId), aFade);
            iFadeMax = new Watchable<uint>(aThread, string.Format("FadeMax({0})", aId), aFadeMax);
            iMute = new Watchable<bool>(aThread, string.Format("Mute({0})", aId), aMute);
            iValue = new Watchable<uint>(aThread, string.Format("Value({0})", aId), value);
            iVolumeLimit = new Watchable<uint>(aThread, string.Format("VolumeLimit({0})", aId), volumeLimit);
            iVolumeMax = new Watchable<uint>(aThread, string.Format("VolumeMax({0})", aId), aVolumeMax);
            iVolumeMilliDbPerStep = new Watchable<uint>(aThread, string.Format("VolumeMilliDbPerStep({0})", aId), aVolumeMilliDbPerStep);
            iVolumeSteps = new Watchable<uint>(aThread, string.Format("VolumeSteps({0})", aId), aVolumeSteps);
            iVolumeUnity = new Watchable<uint>(aThread, string.Format("VolumeUnity({0})", aId), aVolumeUnity);
        }

        public void Dispose()
        {
            iBalance.Dispose();
            iBalance = null;

            iBalanceMax.Dispose();
            iBalanceMax = null;

            iFade.Dispose();
            iFade = null;

            iFadeMax.Dispose();
            iFadeMax = null;

            iMute.Dispose();
            iMute = null;

            iValue.Dispose();
            iValue = null;

            iVolumeLimit.Dispose();
            iVolumeLimit = null;

            iVolumeMax.Dispose();
            iVolumeMax = null;

            iVolumeMilliDbPerStep.Dispose();
            iVolumeMilliDbPerStep = null;

            iVolumeSteps.Dispose();
            iVolumeSteps = null;

            iVolumeUnity.Dispose();
            iVolumeUnity = null;
        }

        public IWatchable<int> Balance
        {
            get
            {
                return iBalance;
            }
        }

        public IWatchable<uint> BalanceMax
        {
            get
            {
                return iBalanceMax;
            }
        }

        public IWatchable<int> Fade
        {
            get
            {
                return iFade;
            }
        }

        public IWatchable<uint> FadeMax
        {
            get
            {
                return iFadeMax;
            }
        }

        public IWatchable<bool> Mute
        {
            get
            {
                return iMute;
            }
        }

        public IWatchable<uint> Value
        {
            get
            {
                return iValue;
            }
        }

        public IWatchable<uint> VolumeLimit
        {
            get
            {
                return iVolumeLimit;
            }
        }

        public IWatchable<uint> VolumeMax
        {
            get
            {
                return iVolumeMax;
            }
        }

        public IWatchable<uint> VolumeMilliDbPerStep
        {
            get
            {
                return iVolumeMilliDbPerStep;
            }
        }

        public IWatchable<uint> VolumeSteps
        {
            get
            {
                return iVolumeSteps;
            }
        }

        public IWatchable<uint> VolumeUnity
        {
            get
            {
                return iVolumeUnity;
            }
        }

        public void SetBalance(int aValue)
        {
            iBalance.Update(aValue);
        }

        public void SetFade(int aValue)
        {
            iFade.Update(aValue);
        }

        public void SetMute(bool aValue)
        {
            iMute.Update(aValue);
        }

        public void SetVolume(uint aValue)
        {
            uint value = aValue;
            if (value > iCurrentVolumeLimit)
            {
                value = iCurrentVolumeLimit;
            }

            if (value != iCurrentVolume)
            {
                iCurrentVolume = value;
                iValue.Update(aValue);
            }
        }

        public void VolumeDec()
        {
            if (iCurrentVolume > 0)
            {
                --iCurrentVolume;
                iValue.Update(iCurrentVolume);
            }
        }

        public void VolumeInc()
        {
            if (iCurrentVolume < iCurrentVolumeLimit)
            {
                ++iCurrentVolume;
                iValue.Update(iCurrentVolume);
            }
        }

        public void Execute(IEnumerable<string> aValue)
        {
            string command = aValue.First().ToLowerInvariant();
            if (command == "balance")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iBalance.Update(int.Parse(value.First()));
            }
            else if (command == "fade")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iFade.Update(int.Parse(value.First()));
            }
            else if (command == "mute")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iMute.Update(bool.Parse(value.First()));
            }
            else if (command == "value")
            {
                IEnumerable<string> value = aValue.Skip(1);
                iValue.Update(uint.Parse(value.First()));
            }
            else if (command == "volumeinc")
            {
                VolumeInc();
            }
            else if (command == "volumedec")
            {
                VolumeDec();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private uint iCurrentVolume;
        private uint iCurrentVolumeLimit;

        private Watchable<int> iBalance;
        private Watchable<uint> iBalanceMax;
        private Watchable<int> iFade;
        private Watchable<uint> iFadeMax;
        private Watchable<bool> iMute;
        private Watchable<uint> iValue;
        private Watchable<uint> iVolumeLimit;
        private Watchable<uint> iVolumeMax;
        private Watchable<uint> iVolumeMilliDbPerStep;
        private Watchable<uint> iVolumeSteps;
        private Watchable<uint> iVolumeUnity;
    }

    public class WatchableVolumeFactory : IWatchableServiceFactory
    {
        public WatchableVolumeFactory(IWatchableThread aThread, IWatchableThread aSubscribeThread)
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
                    iPendingService = new CpProxyAvOpenhomeOrgVolume1(d.Device);
                    iPendingService.SetPropertyInitialEvent(delegate
                    {
                        lock (iLock)
                        {
                            if (iPendingService != null)
                            {
                                iService = new WatchableVolume(iThread, string.Format("Volume({0})", aDevice.Udn), aDevice, iPendingService);
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
        private CpProxyAvOpenhomeOrgVolume1 iPendingService;
        private WatchableVolume iService;
        private IWatchableThread iThread;
    }

    public class WatchableVolume : Volume
    {
        public WatchableVolume(IWatchableThread aThread, string aId, IWatchableDevice aDevice, CpProxyAvOpenhomeOrgVolume1 aService)
            : base(aId, aDevice)
        {
            iService = new ServiceOpenHomeOrgVolume1(aThread, aId, aService);
        }

        public override void Dispose()
        {
            iService.Dispose();
            iService = null;
        }

        internal override IServiceOpenHomeOrgVolume1 Service
        {
            get
            {
                return iService;
            }
        }

        private ServiceOpenHomeOrgVolume1 iService;
    }

    public class MockWatchableVolume : Volume, IMockable
    {
        public MockWatchableVolume(IWatchableThread aThread, string aId, IWatchableDevice aDevice, int aBalance, uint aBalanceMax, int aFade, uint aFadeMax, bool aMute, uint aVolume, uint aVolumeLimit, uint aVolumeMax,
            uint aVolumeMilliDbPerStep, uint aVolumeSteps, uint aVolumeUnity)
            : base(aId, aDevice)
        {
            iService = new MockServiceOpenHomeOrgVolume1(aThread, aId, aBalance, aBalanceMax, aFade, aFadeMax, aMute, aVolume, aVolumeLimit, aVolumeMax, aVolumeMilliDbPerStep, aVolumeSteps, aVolumeUnity);
        }

        public override void Dispose()
        {
            iService.Dispose();
            iService = null;
        }

        internal override IServiceOpenHomeOrgVolume1 Service
        {
            get
            {
                return iService;
            }
        }

        public void Execute(IEnumerable<string> aValue)
        {
            iService.Execute(aValue);
        }

        private MockServiceOpenHomeOrgVolume1 iService;
    }

    public class ServiceVolume : IServiceOpenHomeOrgVolume1, IService
    {
        public ServiceVolume(IManagableWatchableDevice aDevice, IServiceOpenHomeOrgVolume1 aService)
        {
            iDevice = aDevice;
            iService = aService;
        }

        public void Dispose()
        {
            iDevice.Unsubscribe<ServiceVolume>();
            iDevice = null;
        }

        public IWatchableDevice Device
        {
            get { return iDevice; }
        }

        public IWatchable<int> Balance
        {
            get { return iService.Balance; }
        }

        public IWatchable<uint> BalanceMax
        {
            get { return iService.BalanceMax; }
        }

        public IWatchable<int> Fade
        {
            get { return iService.Fade; }
        }

        public IWatchable<uint> FadeMax
        {
            get { return iService.FadeMax; }
        }

        public IWatchable<bool> Mute
        {
            get { return iService.Mute; }
        }

        public IWatchable<uint> Value
        {
            get { return iService.Value; }
        }

        public IWatchable<uint> VolumeLimit
        {
            get { return iService.VolumeLimit; }
        }

        public IWatchable<uint> VolumeMax
        {
            get { return iService.VolumeMax; }
        }

        public IWatchable<uint> VolumeMilliDbPerStep
        {
            get { return iService.VolumeMilliDbPerStep; }
        }

        public IWatchable<uint> VolumeSteps
        {
            get { return iService.VolumeSteps; }
        }

        public IWatchable<uint> VolumeUnity
        {
            get { return iService.VolumeUnity; }
        }

        public void SetBalance(int aValue)
        {
            iService.SetBalance(aValue);
        }

        public void SetFade(int aValue)
        {
            iService.SetFade(aValue);
        }

        public void SetMute(bool aValue)
        {
            iService.SetMute(aValue);
        }

        public void SetVolume(uint aValue)
        {
            iService.SetVolume(aValue);
        }

        public void VolumeDec()
        {
            iService.VolumeDec();
        }

        public void VolumeInc()
        {
            iService.VolumeInc();
        }

        private IManagableWatchableDevice iDevice;
        private IServiceOpenHomeOrgVolume1 iService;
    }
}

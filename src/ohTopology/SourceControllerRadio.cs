﻿using System;
using System.Threading.Tasks;

using OpenHome.Os.App;

namespace OpenHome.Av
{
    public class SourceControllerRadio : IWatcher<string>, ISourceController
    {
        public SourceControllerRadio(ITopology4Source aSource, Watchable<bool> aHasSourceControl,
            Watchable<bool> aHasInfoNext, Watchable<IInfoMetadata> aInfoNext, Watchable<bool> aHasContainer, Watchable<string> aTransportState, Watchable<bool> aCanPause,
            Watchable<bool> aCanSkip, Watchable<bool> aCanSeek, Watchable<bool> aHasPlayMode, Watchable<bool> aShuffle, Watchable<bool> aRepeat)
        {
            iDisposed = false;

            iHasSourceControl = aHasSourceControl;
            iHasContainer = aHasContainer;
            iCanPause = aCanPause;
            iCanSeek = aCanSeek;
            iTransportState = aTransportState;

            aSource.Device.Create<IProxyRadio>((radio) =>
            {
                if (!iDisposed)
                {
                    iRadio = radio;

                    aHasInfoNext.Update(false);
                    iHasContainer.Update(true);
                    iCanPause.Update(false);
                    iCanSeek.Update(false);
                    aCanSkip.Update(false);
                    aHasPlayMode.Update(false);

                    iRadio.TransportState.AddWatcher(this);

                    iHasSourceControl.Update(true);
                }
                else
                {
                    radio.Dispose();
                }
            });
        }

        public void Dispose()
        {
            if (iDisposed)
            {
                throw new ObjectDisposedException("SourceControllerRadio.Dispose");
            }

            if (iRadio != null)
            {
                iHasSourceControl.Update(false);
                iHasContainer.Update(false);
                iCanPause.Update(false);
                iCanSeek.Update(false);

                iRadio.TransportState.RemoveWatcher(this);

                iRadio.Dispose();
                iRadio = null;
            }

            iHasSourceControl = null;
            iHasContainer = null;
            iCanPause = null;
            iCanSeek = null;
            iTransportState = null;

            iDisposed = true;
        }

        public Task<IWatchableContainer<IMediaPreset>> Container
        {
            get
            {
                return iRadio.Container;
            }
        }

        public void Play()
        {
            iRadio.Play();
        }

        public void Pause()
        {
            iRadio.Pause();
        }

        public void Stop()
        {
            iRadio.Stop();
        }

        public void Previous()
        {
            throw new NotImplementedException();
        }

        public void Next()
        {
            throw new NotImplementedException();
        }

        public void Seek(uint aSeconds)
        {
            iRadio.SeekSecondAbsolute(aSeconds);
        }

        public void SetRepeat(bool aValue)
        {
            throw new NotImplementedException();
        }

        public void SetShuffle(bool aValue)
        {
            throw new NotImplementedException();
        }

        public void ItemOpen(string aId, string aValue)
        {
            iTransportState.Update(aValue);
        }

        public void ItemUpdate(string aId, string aValue, string aPrevious)
        {
            iTransportState.Update(aValue);
        }

        public void ItemClose(string aId, string aValue)
        {
        }

        private bool iDisposed;

        private IProxyRadio iRadio;

        private Watchable<bool> iHasSourceControl;
        private Watchable<bool> iHasContainer;
        private Watchable<bool> iCanPause;
        private Watchable<bool> iCanSeek;
        private Watchable<string> iTransportState;
    }
}

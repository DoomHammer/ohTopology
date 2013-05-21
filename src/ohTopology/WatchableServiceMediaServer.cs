﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using System.Xml;
using System.Xml.Linq;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

using OpenHome.Os.App;
using OpenHome.MediaServer;

using OpenHome.Net.ControlPoint;
using OpenHome.Net.ControlPoint.Proxies;


namespace OpenHome.Av
{
    public interface IMediaValue
    {
        string Value { get; }
        IEnumerable<string> Values { get; }
    }

    public interface IMediaMetadata : IEnumerable<KeyValuePair<ITag, IMediaValue>>
    {
        IMediaValue this[ITag aTag] { get; }
    }

    public interface IMediaDatum : IMediaMetadata
    {
        IEnumerable<ITag> Type { get; }
    }

    public interface IMediaServerFragment
    {
        uint Index { get; }
        uint Sequence { get; }
        IEnumerable<IMediaDatum> Data { get; }
    }

    public interface IMediaServerSnapshot
    {
        uint Total { get; }
        uint Sequence { get; }
        IEnumerable<uint> AlphaMap { get; } // null if no alpha map
        Task<IMediaServerFragment> Read(uint aIndex, uint aCount);
    }

    public interface IMediaServerContainer
    {
        IWatchable<IMediaServerSnapshot> Snapshot { get; }
    }

    public interface IMediaServerSession : IDisposable
    {
        Task<IMediaServerContainer> Query(string aValue);
        Task<IMediaServerContainer> Browse(IMediaDatum aDatum); // null = home
    }

    public interface IProxyMediaServer : IService
    {
        IEnumerable<string> Attributes { get; }
        string ManufacturerImageUri { get; }
        string ManufacturerInfo { get; }
        string ManufacturerName { get; }
        string ManufacturerUrl { get; }
        string ModelImageUri { get; }
        string ModelInfo { get; }
        string ModelName { get; }
        string ModelUrl { get; }
        string ProductImageUri { get; }
        string ProductInfo { get; }
        string ProductName { get; }
        string ProductUrl { get; }
        Task<IMediaServerSession> CreateSession();
    }

    public class MediaServerValue : IMediaValue
    {
        private readonly string iValue;
        private readonly List<string> iValues;

        public MediaServerValue(string aValue)
        {
            iValue = aValue;
            iValues = new List<string>(new string[] { aValue });
        }

        public MediaServerValue(IEnumerable<string> aValues)
        {
            iValue = aValues.First();
            iValues = new List<string>(aValues);
        }

        // IMediaServerValue

        public string Value
        {
            get { return (iValue); }
        }

        public IEnumerable<string> Values
        {
            get { return (iValues); }
        }
    }

    public class MediaDictionary
    {
        protected Dictionary<ITag, IMediaValue> iMetadata;

        protected MediaDictionary()
        {
            iMetadata = new Dictionary<ITag, IMediaValue>();
        }

        protected MediaDictionary(IMediaMetadata aMetadata)
        {
            iMetadata = new Dictionary<ITag, IMediaValue>(aMetadata.ToDictionary(x => x.Key, x => x.Value));
        }

        public void Add(ITag aTag, string aValue)
        {
            IMediaValue value = null;

            iMetadata.TryGetValue(aTag, out value);

            if (value == null)
            {
                iMetadata[aTag] = new MediaServerValue(aValue);
            }
            else
            {
                iMetadata[aTag] = new MediaServerValue(value.Values.Concat(new string[] { aValue }));
            }
        }

        // IMediaServerMetadata

        public IMediaValue this[ITag aTag]
        {
            get
            {
                IMediaValue value = null;
                iMetadata.TryGetValue(aTag, out value);
                return (value);
            }
        }
    }

    public class MediaMetadata : MediaDictionary, IMediaMetadata
    {
        public MediaMetadata()
        {
        }

        // IEnumerable<KeyValuePair<ITag, IMediaServer>>

        public IEnumerator<KeyValuePair<ITag, IMediaValue>> GetEnumerator()
        {
            return (iMetadata.GetEnumerator());
        }

        // IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (iMetadata.GetEnumerator());
        }
    }

    public class MediaDatum : MediaDictionary, IMediaDatum
    {
        private readonly ITag[] iType;

        public MediaDatum(params ITag[] aType)
        {
            iType = aType;
        }

        public MediaDatum(IMediaMetadata aMetadata, params ITag[] aType)
            : base(aMetadata)
        {
            iType = aType;
        }

        // IMediaDatum Members

        public IEnumerable<ITag> Type
        {
            get { return (iType); }
        }

        // IEnumerable<KeyValuePair<ITag, IMediaServer>>

        public IEnumerator<KeyValuePair<ITag, IMediaValue>> GetEnumerator()
        {
            return (iMetadata.GetEnumerator());
        }

        // IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class MediaServerFragment : IMediaServerFragment
    {
        private readonly uint iIndex;
        private readonly uint iSequence;
        private readonly IEnumerable<IMediaDatum> iData;

        public MediaServerFragment(uint aIndex, uint aSequence, IEnumerable<IMediaDatum> aData)
        {
            iIndex = aIndex;
            iSequence = aSequence;
            iData = aData;
        }

        // IMediaServerFragment

        public uint Index
        {
            get { return (iIndex); }
        }

        public uint Sequence
        {
            get { return (iSequence); }
        }

        public IEnumerable<IMediaDatum> Data
        {
            get { return (iData); }
        }
    }

    public class ServiceMediaServer
    {
        private readonly IWatchableThread iWatchableThread;
        private readonly IEnumerable<string> iAttributes;
        private readonly string iManufacturerImageUri;
        private readonly string iManufacturerInfo;
        private readonly string iManufacturerName;
        private readonly string iManufacturerUrl;
        private readonly string iModelImageUri;
        private readonly string iModelInfo;
        private readonly string iModelName;
        private readonly string iModelUrl;
        private readonly string iProductImageUri;
        private readonly string iProductInfo;
        private readonly string iProductName;
        private readonly string iProductUrl;

        protected ServiceMediaServer(IWatchableThread aWatchableThread, IEnumerable<string> aAttributes, 
            string aManufacturerImageUri, string aManufacturerInfo, string aManufacturerName, string aManufacturerUrl,
            string aModelImageUri, string aModelInfo, string aModelName, string aModelUrl,
            string aProductImageUri, string aProductInfo, string aProductName, string aProductUrl
            )
        {
            iWatchableThread = aWatchableThread;
            iAttributes = aAttributes;
            iManufacturerImageUri = aManufacturerImageUri;
            iManufacturerInfo = aManufacturerInfo;
            iManufacturerName = aManufacturerName;
            iManufacturerUrl = aManufacturerUrl;
            iModelImageUri = aModelImageUri;
            iModelInfo = aModelInfo;
            iModelName = aModelName;
            iModelUrl = aModelUrl;
            iProductImageUri = aProductImageUri;
            iProductInfo = aProductInfo;
            iProductName = aProductName;
            iProductUrl = aProductUrl;
        }

        internal IWatchableThread WatchableThread
        {
            get { return (iWatchableThread); }
        }

        // IService

        public IWatchableDevice Device { get { return (null); } } // TODO REmove this once it is removed from IService

        // IServiceMediaServer

        public IEnumerable<string> Attributes
        {
            get { return (iAttributes); }
        }

        public string ManufacturerImageUri
        {
            get { return (iManufacturerImageUri); }
        }

        public string ManufacturerInfo
        {
            get { return (iManufacturerInfo); }
        }

        public string ManufacturerName
        {
            get { return (iManufacturerName); }
        }

        public string ManufacturerUrl
        {
            get { return (iManufacturerUrl); }
        }

        public string ModelImageUri
        {
            get { return (iModelImageUri); }
        }

        public string ModelInfo
        {
            get { return (iModelInfo); }
        }

        public string ModelName
        {
            get { return (iModelName); }
        }

        public string ModelUrl
        {
            get { return (iModelUrl); }
        }

        public string ProductImageUri
        {
            get { return (iProductImageUri); }
        }

        public string ProductInfo
        {
            get { return (iProductInfo); }
        }

        public string ProductName
        {
            get { return (iProductName); }
        }

        public string ProductUrl
        {
            get { return (iProductUrl); }
        }
    }

    internal class ProxyMediaServerMock : IProxyMediaServer
    {
        private readonly ServiceMediaServerMock iService;

        public ProxyMediaServerMock(ServiceMediaServerMock aService)
        {
            iService = aService;
        }

        // IService

        public IWatchableDevice Device { get { return (null); } } // TODO REmove this once it is removed from IService

        // IServiceMediaServer

        public IEnumerable<string> Attributes
        {
            get { return (iService.Attributes); }
        }

        public string ManufacturerImageUri
        {
            get { return (iService.ManufacturerImageUri); }
        }

        public string ManufacturerInfo
        {
            get { return (iService.ManufacturerInfo); }
        }

        public string ManufacturerName
        {
            get { return (iService.ManufacturerName); }
        }

        public string ManufacturerUrl
        {
            get { return (iService.ManufacturerUrl); }
        }

        public string ModelImageUri
        {
            get { return (iService.ModelImageUri); }
        }

        public string ModelInfo
        {
            get { return (iService.ModelInfo); }
        }

        public string ModelName
        {
            get { return (iService.ModelName); }
        }

        public string ModelUrl
        {
            get { return (iService.ModelUrl); }
        }

        public string ProductImageUri
        {
            get { return (iService.ProductImageUri); }
        }

        public string ProductInfo
        {
            get { return (iService.ProductInfo); }
        }

        public string ProductName
        {
            get { return (iService.ProductName); }
        }

        public string ProductUrl
        {
            get { return (iService.ProductUrl); }
        }

        public Task<IMediaServerSession> CreateSession()
        {
            return (iService.CreateSession());
        }

        // IDisposable

        public void Dispose()
        {
            iService.Destroy(this);
        }
    }

    public class ServiceMediaServerMock : ServiceMediaServer, IWatchableService
    {
        private readonly ITagManager iTagManager;
        private readonly IEnumerable<IMediaMetadata> iMetadata;
        private readonly List<IProxyMediaServer> iProxies;
        private readonly List<IMediaServerSession> iSessions;
        
        public ServiceMediaServerMock(IWatchableThread aWatchableThread, IEnumerable<string> aAttributes, 
            string aManufacturerImageUri, string aManufacturerInfo, string aManufacturerName, string aManufacturerUrl,
            string aModelImageUri, string aModelInfo, string aModelName, string aModelUrl,
            string aProductImageUri, string aProductInfo, string aProductName, string aProductUrl,
            string aAppRoot)
            : base(aWatchableThread, aAttributes,
            aManufacturerImageUri, aManufacturerInfo, aManufacturerName, aManufacturerUrl,
            aModelImageUri, aModelInfo, aModelName, aModelUrl,
            aProductImageUri, aProductInfo, aProductName, aProductUrl)
        {
            iTagManager = new TagManager();
            iMetadata = ReadMetadata(aAppRoot);
            iProxies = new List<IProxyMediaServer>();
            iSessions = new List<IMediaServerSession>();
        }

        private IEnumerable<IMediaMetadata> ReadMetadata(string aAppRoot)
        {
            var path = Path.Combine(aAppRoot, "MockMediaServer.zip");

            using (var file = File.Open(path, FileMode.Open))
            {
                var zip = new ZipFile(file);

                var entries = zip.GetEnumerator();

                entries.MoveNext();

                var entry = entries.Current as ZipEntry;

                Do.Assert(entry.Name == "MockMediaServer.xml");

                Stream stream = zip.GetInputStream(entry);

                return (ReadMetadata(stream));
            }
        }

        private IEnumerable<IMediaMetadata> ReadMetadata(Stream aStream)
        {
            var reader = XmlReader.Create(aStream);

            var xml = XDocument.Load(reader);

            var items = from item in xml.Descendants("item") select new
            {
                Metadata = item.Descendants("metadatum")
            };

            var results = new List<IMediaMetadata>();

            foreach (var item in items)
            {
                var metadata = new MediaMetadata();

                var xmetadata = from metadatum in item.Metadata select new
                {
                    Tag = metadatum.Attribute("tag"),
                    Values = metadatum.Descendants("value")
                };

                foreach (var metadatum in xmetadata)
                {
                    ITag tag = iTagManager.Audio[metadatum.Tag.Value];

                    if (tag != null)
                    {
                        foreach (var value in metadatum.Values)
                        {
                            metadata.Add(tag, value.Value);
                        }
                    }
                }

                results.Add(metadata);
            }

            return (results);
        }

        internal Task<IMediaServerSession> CreateSession()
        {
            return (Task.Factory.StartNew<IMediaServerSession>(() =>
            {
                var session = new MediaServerSessionMock(this);
                iSessions.Add(session);
                return (session);
            }));
        }

        internal ITagManager TagManager
        {
            get { return (iTagManager); }
        }

        internal IEnumerable<IMediaMetadata> Metadata
        {
            get { return (iMetadata); }
        }

        internal void Destroy(IProxyMediaServer aProxy)
        {
            iProxies.Remove(aProxy);
        }

        internal void Destroy(IMediaServerSession aSession)
        {
            iSessions.Remove(aSession);
        }

        // IServiceFactory

        public IService Create(IManagableWatchableDevice aDevice)
        {
            var proxy = new ProxyMediaServerMock(this);
            iProxies.Add(proxy);
            return (proxy);
        }

        // IDispose

        public void Dispose()
        {
            Do.Assert(iProxies.Count == 0);
            Do.Assert(iSessions.Count == 0);
        }
    }

    internal class MediaServerSessionMock : IMediaServerSession
    {
        private readonly ServiceMediaServerMock iService;
        private readonly List<IMediaDatum> iRoot;

        private IMediaServerContainer iContainer;

        public MediaServerSessionMock(ServiceMediaServerMock aService)
        {
            iService = aService;

            iRoot = new List<IMediaDatum>();
            iRoot.Add(GetRootContainerTracks());
            iRoot.Add(GetRootContainerArtists());
            iRoot.Add(GetRootContainerAlbums());
            iRoot.Add(GetRootContainerGenres());
        }

        private IMediaDatum GetRootContainerTracks()
        {
            var datum = new MediaDatum(TagManager.Container.Title);
            datum.Add(TagManager.Container.Title, "Tracks");
            return (datum);
        }

        private IMediaDatum GetRootContainerArtists()
        {
            var datum = new MediaDatum(TagManager.Container.Title, TagManager.Audio.Artist, TagManager.Audio.Album);
            datum.Add(TagManager.Container.Title, "Artists");
            return (datum);
        }

        private IMediaDatum GetRootContainerAlbums()
        {
            var datum = new MediaDatum(TagManager.Container.Title, TagManager.Audio.Album);
            datum.Add(TagManager.Container.Title, "Albums");
            return (datum);
        }

        private IMediaDatum GetRootContainerGenres()
        {
            var datum = new MediaDatum(TagManager.Container.Title, TagManager.Audio.Genre);
            datum.Add(TagManager.Container.Title, "Genre");
            return (datum);
        }

        private Task<IMediaServerContainer> BrowseRootTracks()
        {
            return (Task.Factory.StartNew<IMediaServerContainer>(() =>
            {
                var metadata = iService.Metadata.Select((m) => new MediaDatum(m));
                iContainer = new MediaServerContainerMock(iService.WatchableThread, new MediaServerSnapshotMock(metadata));
                return (iContainer);
            }));
        }

        private Task<IMediaServerContainer> BrowseRootArtists()
        {
            var containers = Metadata.Select(m => m[TagManager.Audio.Artist])
                .Select(v => v.Value)
                .Distinct()
                .OrderBy(v => v)
                .Select(v =>
                {
                    var datum = new MediaDatum(TagManager.Audio.Artist, TagManager.Audio.Album);
                    datum.Add(TagManager.Audio.Artist, v);
                    return (datum);
                });

            return (Task.Factory.StartNew<IMediaServerContainer>(() =>
            {
                iContainer = new MediaServerContainerMock(iService.WatchableThread, new MediaServerSnapshotMock(containers));
                return (iContainer);
            }));
        }

        private Task<IMediaServerContainer> BrowseRootAlbums()
        {
            return (Task.Factory.StartNew<IMediaServerContainer>(() =>
            {
                var metadata = iService.Metadata.Select((m) => new MediaDatum(m));
                iContainer = new MediaServerContainerMock(iService.WatchableThread, new MediaServerSnapshotMock(metadata));
                return (iContainer);
            }));
        }

        private Task<IMediaServerContainer> BrowseRootGenres()
        {
            return (Task.Factory.StartNew<IMediaServerContainer>(() =>
            {
                var metadata = iService.Metadata.Select((m) => new MediaDatum(m));
                iContainer = new MediaServerContainerMock(iService.WatchableThread, new MediaServerSnapshotMock(metadata));
                return (iContainer);
            }));
        }

        internal IWatchableThread WatchableThread
        {
            get { return (iService.WatchableThread); }
        }

        internal ITagManager TagManager
        {
            get { return (iService.TagManager); }
        }

        internal IEnumerable<IMediaMetadata> Metadata
        {
            get { return (iService.Metadata); }
        }

        internal void Destroy(IMediaServerContainer aContainer)
        {
        }

        // IMediaServerSession

        public Task<IMediaServerContainer> Query(string aValue)
        {
            throw new NotImplementedException();
        }

        public Task<IMediaServerContainer> Browse(IMediaDatum aDatum)
        {
            if (aDatum == null)
            {
                return (Task.Factory.StartNew<IMediaServerContainer>(() =>
                {
                    iContainer = new MediaServerContainerMock(iService.WatchableThread, new MediaServerSnapshotMock(iRoot));
                    return (iContainer);
                }));
            }

            Do.Assert(aDatum.Type.Any());


            if (aDatum.Type.First() == TagManager.Container.Title)
            {
                // Top Level Container

                if (aDatum.Type.Skip(1).Any())
                {
                    ITag tag = aDatum.Type.Skip(1).First();

                    if (tag == TagManager.Audio.Artist)
                    {
                        return (BrowseRootArtists());
                    }

                    if (tag == TagManager.Audio.Album)
                    {
                        return (BrowseRootAlbums());
                    }

                    if (tag == TagManager.Audio.Genre)
                    {
                        return (BrowseRootGenres());
                    }

                    Do.Assert(false);
                }
            }

            return (BrowseRootTracks());
        }

        // Disposable

        public void Dispose()
        {
            iService.Destroy(this);
        }
    }

    internal class MediaServerContainerMock : IMediaServerContainer
    {
        private readonly Watchable<IMediaServerSnapshot> iSnapshot;

        public MediaServerContainerMock(IWatchableThread aWatchableThread, IMediaServerSnapshot aSnapshot)
        {
            iSnapshot = new Watchable<IMediaServerSnapshot>(aWatchableThread, "snapshot", aSnapshot);
        }

        // IMediaServerContainer

        public IWatchable<IMediaServerSnapshot> Snapshot
        {
            get { return (iSnapshot); }
        }
    }


    internal class MediaServerSnapshotMock : IMediaServerSnapshot
    {
        private readonly IEnumerable<IMediaDatum> iData;
        private readonly IEnumerable<uint> iAlphaMap;

        public MediaServerSnapshotMock(IEnumerable<IMediaDatum> aData)
        {
            iData = aData;
            iAlphaMap = null;
        }

        // IMediaServerSnapshot

        public uint Total
        {
            get { return ((uint)iData.Count()); }
        }

        public uint Sequence
        {
            get { return (0); }
        }

        public IEnumerable<uint> AlphaMap
        {
            get { return (iAlphaMap); }
        }

        public Task<IMediaServerFragment> Read(uint aIndex, uint aCount)
        {
            Do.Assert(aIndex + aCount < Total);

            return (Task.Factory.StartNew<IMediaServerFragment>(() =>
            {
                return (new MediaServerFragment(aIndex, 0, iData.Skip((int)aIndex).Take((int)aCount)));
            }));
        }
    }

    /*
    public class ServiceUpnpOrgContentDirectory1 : IServiceMediaServer
    {
        private class BrowseAsyncHandler
        {
            public BrowseAsyncHandler(CpProxyUpnpOrgContentDirectory1 aService, Action<IServiceMediaServerBrowseResult> aCallback)
            {
                iService = aService;
                iCallback = aCallback;
            }

            public void Browse(string aObjectId, string aBrowseFlag, string aFilter, uint aStartingIndex, uint aRequestedCount, string aSortCriteria)
            {
                iService.BeginBrowse(aObjectId, aBrowseFlag, aFilter, aStartingIndex, aRequestedCount, aSortCriteria, Callback);
            }

            private void Callback(IntPtr aAsyncHandle)
            {
                string result;
                uint numberReturned;
                uint totalMatches;
                uint updateId;

                iService.EndBrowse(aAsyncHandle, out result, out numberReturned, out totalMatches, out updateId);

                iCallback(null); // TODO
            }

            private CpProxyUpnpOrgContentDirectory1 iService;
            private Action<IServiceMediaServerBrowseResult> iCallback;
        }

        public ServiceUpnpOrgContentDirectory1(IWatchableThread aThread, string aId, CpProxyUpnpOrgContentDirectory1 aService)
        {
            iLock = new object();
            iDisposed = false;

            iService = aService;

            iService.SetPropertySystemUpdateIDChanged(HandleSystemUpdateIDChanged);

            iUpdateCount = new Watchable<uint>(aThread, string.Format("UpdateCount({0})", aId), iService.PropertySystemUpdateID());
        }

        public void Dispose()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    throw new ObjectDisposedException("ServiceUpnpOrgContentDirectory1.Dispose");
                }

                iService = null;

                iDisposed = true;
            }
        }

        public IWatchable<uint> UpdateCount
        {
            get
            {
                return iUpdateCount;
            }
        }

        public void Browse(string aId, Action<IServiceMediaServerBrowseResult> aCallback)
        {
            BrowseAsyncHandler handler = new BrowseAsyncHandler(iService, aCallback);
            handler.Browse(aId);
        }

        private void HandleSystemUpdateIDChanged()
        {
            lock (iLock)
            {
                if (iDisposed)
                {
                    return;
                }

                iUpdateCount.Update(iService.PropertySystemUpdateID());
            }
        }

        private object iLock;
        private bool iDisposed;

        private CpProxyUpnpOrgContentDirectory1 iService;

        private Watchable<uint> iUpdateCount;
    }
    */
}

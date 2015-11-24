using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using static AuroraAssetEditor.Classes.AuroraDbManager;

namespace AuroraAssetEditor.Classes {
    public class LocalOperations {

        public string GameDataDir { get; set; }

        public byte[] GetAssetData(string file, string assetDir) {

            var pathToGameDir = GameDataDir + Path.DirectorySeparatorChar + assetDir + Path.DirectorySeparatorChar;

            if(File.Exists(pathToGameDir + file)) {
                return File.ReadAllBytes(pathToGameDir + file);
                }

            return null;
            }

        public bool SendAssetData(string file, string assetDir, byte[] data) {

            return true;
            }


        public static void ParseXml(Stream xmlData, XboxTitleInfo titleInfo) {
            XboxAssetDownloader.SendStatusChanged("Parsing Title/Asset info...");
            var ret = new List<XboxTitleInfo.XboxAssetInfo>();

            XDocument marketData = XDocument.Load(xmlData);

            var liveXmlNamespace = marketData.Root.Attributes().First(p => p.Name.LocalName == "live");
            string live = liveXmlNamespace.Value;

            var fullTitle = marketData.Descendants(XName.Get("fullTitle", live)).First();

            titleInfo.Title = fullTitle.Value;

            var description = marketData.Descendants(XName.Get("description", live)).First();

            titleInfo.Description = description.Value;

            var assets = marketData.Descendants(XName.Get("image", live));

            foreach(var asset in assets) {
                var fileUrl = asset.Element(XName.Get("fileUrl", live)).Value;

                var fileUri = new Uri(fileUrl);

                var fileName = Path.GetFileNameWithoutExtension(fileUrl);

                if(fileName.StartsWith("banner", StringComparison.CurrentCultureIgnoreCase)) {
                    ret.Add(new XboxTitleInfo.XboxAssetInfo(fileUri, XboxTitleInfo.XboxAssetType.Banner, titleInfo));
                    continue;
                    }

                if(fileName.StartsWith("background", StringComparison.CurrentCultureIgnoreCase)) {
                    ret.Add(new XboxTitleInfo.XboxAssetInfo(fileUri, XboxTitleInfo.XboxAssetType.Background, titleInfo));
                    continue;
                    }

                if(fileName.StartsWith("tile", StringComparison.CurrentCultureIgnoreCase)) {
                    ret.Add(new XboxTitleInfo.XboxAssetInfo(fileUri, XboxTitleInfo.XboxAssetType.Icon, titleInfo));
                    continue;
                    }

                if(fileName.StartsWith("screen", StringComparison.CurrentCultureIgnoreCase)) {
                    ret.Add(new XboxTitleInfo.XboxAssetInfo(fileUri, XboxTitleInfo.XboxAssetType.Screenshot, titleInfo));
                    continue;
                    }
                }

            titleInfo.AssetsInfo = ret.ToArray();
            XboxAssetDownloader.SendStatusChanged("Finished parsing Title/Asset info...");
            }

        }

    internal class ContentItemLocal {
        public ContentItemLocal(ContentItem original) {
            DatabaseId = original.DatabaseId;
            TitleId = original.TitleId;
            MediaId = original.MediaId;
            DiscNum = original.DiscNum;
            TitleName = original.TitleName;

            }

        public string TitleId { get; private set; }

        public string MediaId { get; private set; }

        public string DiscNum { get; private set; }

        public string TitleName { get; private set; }

        public string DatabaseId { get; private set; }

        public string Path { get { return string.Format("{0}_{1}", TitleId, DatabaseId); } }

        public void SaveAsBoxart(byte[] data) { App.LocalOperations.SendAssetData(string.Format("GC{0}.asset", TitleId), Path, data); }

        public void SaveAsBackground(byte[] data) { App.LocalOperations.SendAssetData(string.Format("BK{0}.asset", TitleId), Path, data); }

        public void SaveAsIconBanner(byte[] data) { App.LocalOperations.SendAssetData(string.Format("GL{0}.asset", TitleId), Path, data); }

        public void SaveAsScreenshots(byte[] data) { App.LocalOperations.SendAssetData(string.Format("SS{0}.asset", TitleId), Path, data); }

        public byte[] GetBoxart() { return App.LocalOperations.GetAssetData(string.Format("GC{0}.asset", TitleId), Path); }

        public byte[] GetBackground() { return App.LocalOperations.GetAssetData(string.Format("BK{0}.asset", TitleId), Path); }

        public byte[] GetIconBanner() { return App.LocalOperations.GetAssetData(string.Format("GL{0}.asset", TitleId), Path); }

        public byte[] GetScreenshots() { return App.LocalOperations.GetAssetData(string.Format("SS{0}.asset", TitleId), Path); }
        }
    }

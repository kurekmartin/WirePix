using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WirePix.Core.Models.Devices;

namespace WirePix.Core.Devices;

public sealed class DeviceHistoryStore(string path)
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));

    public IReadOnlyList<DeviceInfo> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            XDocument document = XDocument.Load(_path);
            return document.Root?.Elements("DeviceInfo")
                       .Select(ReadDevice)
                       .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<DeviceInfo> devices)
    {
        string directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var root = new XElement("DeviceList",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
            (devices ?? []).Select(device => new XElement("DeviceInfo",
                string.IsNullOrEmpty(device.Id) ? null : new XElement("Id", device.Id),
                new XElement("Name", device.Name ?? string.Empty),
                new XElement("LastBackup", device.LastBackup.ToString("o", CultureInfo.InvariantCulture)))));
        new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(_path);
    }

    private static DeviceInfo ReadDevice(XElement element)
    {
        string id = (string)element.Element("Id") ?? (string)element.Element("ID") ?? string.Empty;
        string name = (string)element.Element("Name") ?? string.Empty;
        DateTime.TryParse(
            (string)element.Element("LastBackup"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime lastBackup);
        return new DeviceInfo(name, id, lastBackup);
    }
}

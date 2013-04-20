using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;
using System.Reflection;

public class XmlData
{
    static string AssemblyDirectory
    {
        get
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }

    public XmlData(string path = "./data", int xmlCount = 36,
                     string pattern = "dat{0}.xml", string addition = "addition.xml")
    {
        TypeToElement = new ReadOnlyDictionary<short, XElement>(
            type2elem = new Dictionary<short, XElement>());

        TypeToId = new ReadOnlyDictionary<short, string>(
            type2id = new Dictionary<short, string>());
        IdToType = new ReadOnlyDictionary<string, short>(
            id2type = new Dictionary<string, short>(StringComparer.InvariantCultureIgnoreCase));

        Tiles = new ReadOnlyDictionary<short, TileDesc>(
            tiles = new Dictionary<short, TileDesc>());
        Items = new ReadOnlyDictionary<short, Item>(
            items = new Dictionary<short, Item>());
        ObjectDescs = new ReadOnlyDictionary<short, ObjectDesc>(
            objDescs = new Dictionary<short, ObjectDesc>());
        Portals = new ReadOnlyDictionary<short, PortalDesc>(
            portals = new Dictionary<short, PortalDesc>());

        this.addition = new XElement("ExtData");

        string basePath = Path.Combine(AssemblyDirectory, path);
        string xml;
        Stream stream;
        for (int i = 0; i < xmlCount; i++)
        {
            xml = Path.Combine(basePath, string.Format(pattern, i));
            using (stream = File.OpenRead(xml))
                ProcessXml(XElement.Load(stream), false);
        }

        if (addition != null)
        {
            xml = Path.Combine(basePath, addition);
            using (stream = File.OpenRead(xml))
                ProcessXml(XElement.Load(stream), true);
        }
    }

    public void AddObjects(XElement elem)
    {
        AddObjects(elem, true);
    }
    void AddObjects(XElement root, bool addition)
    {
        foreach (var elem in root.XPathSelectElements("//Object"))
        {
            if (elem.Element("Class") == null) continue;
            string cls = elem.Element("Class").Value;
            short type = (short)Utils.FromString(elem.Attribute("type").Value);
            string id = elem.Attribute("id").Value;

            type2id[type] = id;
            id2type[id] = type;
            type2elem[type] = elem;

            switch (cls)
            {
                case "Equipment":
                    items[type] = new Item(elem);
                    break;
                case "Portal":
                    try
                    {
                        portals[type] = new PortalDesc(elem);
                    }
                    catch
                    {
                        Console.WriteLine("Error for portal: " + type + " id: " + id);
                        /*3392,1792,1795,1796,1805,1806,1810,1825 -- no location, assume nexus?* 
    *  Tomb Portal of Cowardice,  Dungeon Portal,  Portal of Cowardice,  Realm Portal,  Glowing Portal of Cowardice,  Glowing Realm Portal,  Nexus Portal,  Locked Wine Cellar Portal*/
                    }
                    break;
                default:
                    objDescs[type] = new ObjectDesc(elem);
                    break;
            }

            if (addition)
            {
                this.addition.Add(elem);
                updateCount++;
            }
        }
    }

    public void AddGrounds(XElement elem)
    {
        AddGrounds(elem, true);
    }
    void AddGrounds(XElement root, bool addition)
    {
        foreach (var elem in root.XPathSelectElements("//Ground"))
        {
            short type = (short)Utils.FromString(elem.Attribute("type").Value);
            string id = elem.Attribute("id").Value;

            type2id[type] = id;
            id2type[id] = type;
            type2elem[type] = elem;

            tiles[type] = new TileDesc(elem);

            if (addition)
            {
                this.addition.Add(elem);
                updateCount++;
            }
        }
    }

    void ProcessXml(XElement root, bool addition)
    {
        AddObjects(root, addition);
        AddGrounds(root, addition);
    }


    Dictionary<short, XElement> type2elem;
    Dictionary<short, string> type2id;
    Dictionary<string, short> id2type;

    Dictionary<short, TileDesc> tiles;
    Dictionary<short, Item> items;
    Dictionary<short, ObjectDesc> objDescs;
    Dictionary<short, PortalDesc> portals;


    public IDictionary<short, XElement> TypeToElement { get; private set; }

    public IDictionary<short, string> TypeToId { get; private set; }
    public IDictionary<string, short> IdToType { get; private set; }

    public IDictionary<short, TileDesc> Tiles { get; private set; }
    public IDictionary<short, Item> Items { get; private set; }
    public IDictionary<short, ObjectDesc> ObjectDescs { get; private set; }
    public IDictionary<short, PortalDesc> Portals { get; private set; }

    int updateCount = 0;
    int prevUpdateCount = -1;
    XElement addition;
    string[] addXml;

    void UpdateXml()
    {
        if (prevUpdateCount != updateCount)
        {
            addXml = new string[] { addition.ToString() };
            prevUpdateCount = updateCount;
        }
    }

    public string[] AdditionXml
    {
        get
        {
            UpdateXml();
            return addXml;
        }
    }


    //some storage
    Dictionary<object, object> datas;
    public object GetData(object obj)
    {
        return datas[obj];
    }
    public void SetData(object key, object val)
    {
        datas[key] = val;
    }
}
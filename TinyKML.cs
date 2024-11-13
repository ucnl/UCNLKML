using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace UCNLKML
{
    public enum BoolEnum : int
    {
        unused = 0,
        used = 1
    }

    public enum AltitudeMode
    {
        absolute,
        relativeToGround
    }

    public class KMLLocation
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }

        public KMLLocation()
            : this(0, 0, 0)
        {
        }

        public KMLLocation(double longitude, double latitude, double altitude)
        {
            Longitude = longitude;
            Latitude = latitude;
            Altitude = altitude;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F08},{1:F08},{2:F08}", Longitude, Latitude, Altitude);
        }

        public static List<KMLLocation> Parse(string src)
        {
            List<KMLLocation> result = new List<KMLLocation>();
            var c_splits = src.Split(new string[] { " ", "\r\n", "\t", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < c_splits.Length; i++)
            {
                var l_splits = c_splits[i].Split(",".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);

                double lon = double.NaN;
                double lat = double.NaN;
                double alt = 0;

                if (l_splits.Length > 1)
                {
                    lon = double.Parse(l_splits[0], CultureInfo.InvariantCulture);
                    lat = double.Parse(l_splits[1], CultureInfo.InvariantCulture);

                    if (l_splits.Length > 2)
                        alt = double.Parse(l_splits[2], CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new ArgumentException(string.Format("Error: missing parameters in string: {1}", c_splits[i]));
                }

                result.Add(new KMLLocation(lon, lat, alt));
            }

            return result;
        }
    }

    public abstract class KMLPlacemarkItem : IList<KMLLocation>
    {
        public bool Extrude { get; set; }
        public bool Tessellate { get; set; }
        protected List<KMLLocation> coordinates;

        public string CoordinatesToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            for (int i = 0; i < coordinates.Count - 1; i++)
                sb.AppendFormat("{0} ", coordinates[i].ToString());
            sb.AppendFormat("{0}", coordinates[coordinates.Count - 1].ToString());
            sb.AppendLine();

            return sb.ToString();
        }

        #region IList

        public int IndexOf(KMLLocation item)
        {
            return coordinates.IndexOf(item);
        }

        public void Insert(int index, KMLLocation item)
        {
            coordinates.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            coordinates.RemoveAt(index);
        }

        public KMLLocation this[int index]
        {
            get
            {
                return coordinates[index];
            }
            set
            {
                coordinates[index] = value;
            }
        }

        public void Add(KMLLocation item)
        {
            coordinates.Add(item);
        }

        public void Clear()
        {
            coordinates.Clear();
        }

        public bool Contains(KMLLocation item)
        {
            return coordinates.Contains(item);
        }

        public void CopyTo(KMLLocation[] array, int arrayIndex)
        {
            coordinates.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return coordinates.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KMLLocation item)
        {
            return coordinates.Remove(item);
        }

        public IEnumerator<KMLLocation> GetEnumerator()
        {
            return coordinates.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (var item in coordinates)
                yield return item;
        }

        #endregion
    }

    public class KMLPoint : KMLPlacemarkItem
    {
        #region Properties

        public KMLLocation Coordinate
        {
            get
            {
                return base.coordinates[0];
            }
            set
            {
                base.coordinates[0] = value;
            }
        }

        #endregion

        #region Constructor

        public KMLPoint(double lon, double lat, double alt, bool isExtrude, bool isTessellate)
            : this(new KMLLocation(lon, lat, alt), isExtrude, isTessellate)
        {
        }

        public KMLPoint(KMLLocation loc, bool isExtrude, bool isTessellate)
        {
            base.Extrude = isExtrude;
            base.Tessellate = isTessellate;
            base.coordinates = new List<KMLLocation>();
            base.coordinates.Add(loc);
        }

        #endregion
    }

    public class KMLLineString : KMLPlacemarkItem
    {
        #region Constructor

        public KMLLineString()
            : this(false, true)
        {
        }

        public KMLLineString(KMLLocation[] points)
            : this(false, true, points)
        {
        }

        public KMLLineString(bool extrude, bool tessellate)
        {
            base.coordinates = new List<KMLLocation>();
            base.Extrude = extrude;
            base.Tessellate = tessellate;
        }

        public KMLLineString(bool extrude, bool tessellate, KMLLocation[] points)
        {
            base.coordinates = new List<KMLLocation>(points);
            base.Extrude = extrude;
            base.Tessellate = tessellate;
        }

        #endregion
    }

    public class KMLPlacemark
    {
        #region Properties

        public string Name { get; set; }
        public string Description { get; set; }
        public KMLPlacemarkItem PlacemarkItem { get; set; }

        public KMLPlacemark(string name, string description, bool extrude, bool tessellate, KMLLocation point)
        {
            Name = name;
            Description = description;
            PlacemarkItem = new KMLPoint(point, extrude, tessellate);
        }

        public KMLPlacemark(string name, string description, KMLLocation[] points)
        {
            Name = name;
            Description = description;
            PlacemarkItem = new KMLLineString(points);
        }

        public KMLPlacemark(string name, string description, KMLPlacemarkItem item)
        {
            Name = name;
            Description = description;
            PlacemarkItem = item;
        }

        #endregion
    }

    public class KMLData : IList<KMLPlacemark>
    {
        #region Properties

        public string Name { get; set; }
        public string Description { get; set; }

        List<KMLPlacemark> placemarks;

        #endregion

        #region Constructor

        public KMLData()
            : this("", "")
        {
        }

        public KMLData(string name, string description)
        {
            Name = name;
            Description = description;
            placemarks = new List<KMLPlacemark>();
        }

        #endregion

        #region IList

        public int IndexOf(KMLPlacemark item)
        {
            return placemarks.IndexOf(item);
        }

        public void Insert(int index, KMLPlacemark item)
        {
            placemarks.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            placemarks.RemoveAt(index);
        }

        public KMLPlacemark this[int index]
        {
            get
            {
                return placemarks[index];
            }
            set
            {
                placemarks[index] = value;
            }
        }

        public void Add(KMLPlacemark item)
        {
            placemarks.Add(item);
        }

        public void Clear()
        {
            placemarks.Clear();
        }

        public bool Contains(KMLPlacemark item)
        {
            return placemarks.Contains(item);
        }

        public void CopyTo(KMLPlacemark[] array, int arrayIndex)
        {
            placemarks.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return placemarks.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KMLPlacemark item)
        {
            return placemarks.Remove(item);
        }

        public IEnumerator<KMLPlacemark> GetEnumerator()
        {
            return placemarks.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (var item in placemarks)
                yield return item;
        }

        #endregion
    }

    public static class TinyKML
    {
        #region Methods

        #region Public

        public static KMLData Read(string fileName)
        {
            KMLData result = new KMLData();

            using (XmlReader reader = XmlReader.Create(fileName))
            {

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name.ToUpper())
                        {
                            case "NAME":
                                {
                                    result.Name = TinyKML.ReadString(reader);
                                    break;
                                }
                            case "DESCRIPTION":
                                {
                                    result.Name = TinyKML.ReadString(reader);
                                    break;
                                }
                            case "PLACEMARK":
                                {
                                    result.Add(TinyKML.ReadPlacemark(reader));
                                    break;
                                }
                        }
                    }
                }
            }

            return result;
        }

        public static void Write(KMLData data, string fileName)
        {
            using (XmlWriter writer = XmlWriter.Create(fileName, new XmlWriterSettings { CloseOutput = true, Indent = true }))
            {
                writer.WriteStartDocument(false);
                writer.WriteStartElement("kml", "http://www.opengis.net/kml/2.2");
                writer.WriteStartElement("Document");

                if (!string.IsNullOrEmpty(data.Name)) writer.WriteElementString("name", data.Name);
                if (!string.IsNullOrEmpty(data.Description)) writer.WriteElementString("description", data.Description);

                foreach (var item in data)
                    Write("Placemark", item, writer);

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        #endregion

        #region Private

        #region Write

        private static void Write(string tagName, KMLPlacemark placemark, XmlWriter writer)
        {
            writer.WriteStartElement("Placemark");

            if (!string.IsNullOrEmpty(placemark.Name)) writer.WriteElementString("name", placemark.Name);
            if (!string.IsNullOrEmpty(placemark.Description)) writer.WriteElementString("description", placemark.Description);

            if (placemark.PlacemarkItem is KMLPoint)
                writer.WriteStartElement("Point");
            else if (placemark.PlacemarkItem is KMLLineString)
                writer.WriteStartElement("LineString");
           
            writer.WriteElementString("extrude", (Convert.ToInt32(placemark.PlacemarkItem.Extrude)).ToString());
            writer.WriteElementString("tessellate", (Convert.ToInt32(placemark.PlacemarkItem.Tessellate)).ToString());
            writer.WriteElementString("coordinates", placemark.PlacemarkItem.CoordinatesToString());

            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        #endregion

        #region Read

        private static string ReadString(XmlReader reader)
        {
            if (reader.IsEmptyElement) throw new FormatException(reader.Name);

            string elementName = reader.Name;
            string result = string.Empty;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Text:
                        result = reader.Value;
                        break;

                    case XmlNodeType.EndElement:
                        return result;

                    case XmlNodeType.Element:
                        throw new FormatException(elementName);
                }
            }

            throw new FormatException(elementName);
        }

        private static int ReadInt(XmlReader reader)
        {
            string value = ReadString(reader);
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static KMLPlacemarkItem ReadPlacemarkItem(XmlReader reader)
        {
            bool isExtrude = false;
            bool isTessallate = true;
            List<KMLLocation> coordinates = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name.ToUpper())
                    {
                        case "EXTRUDE":
                            {
                                isExtrude = Convert.ToBoolean(TinyKML.ReadInt(reader));
                                break;
                            }
                        case "TESSALLATE":
                            {
                                isTessallate = Convert.ToBoolean(TinyKML.ReadInt(reader));
                                break;
                            }
                        case "COORDINATES":
                            {
                                coordinates = KMLLocation.Parse(TinyKML.ReadString(reader));

                                if (coordinates == null)
                                    throw new FormatException();

                                if (coordinates.Count == 1)
                                    return new KMLPoint(coordinates[0], isExtrude, isTessallate);
                                else
                                    return new KMLLineString(isExtrude, isTessallate, coordinates.ToArray());
                            }
                    }
                }
            }

            throw new FormatException();
        }

        private static KMLPlacemark ReadPlacemark(XmlReader reader)
        {
            string name = string.Empty;
            string description = string.Empty;

            KMLPlacemarkItem item = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name.ToUpper())
                    {
                        case "NAME":
                            {
                                name = TinyKML.ReadString(reader);
                                break;
                            }
                        case "DESCRIPTION":
                            {
                                description = TinyKML.ReadString(reader);
                                break;
                            }
                        case "POINT":
                            {
                                item = (KMLPoint)TinyKML.ReadPlacemarkItem(reader);

                                if (item != null)
                                    return new KMLPlacemark(name, description, item);
                                else
                                    throw new FormatException();
                            }
                        case "LINESTRING":
                            {
                                item = (KMLLineString)TinyKML.ReadPlacemarkItem(reader);

                                if (item != null)
                                    return new KMLPlacemark(name, description, item);
                                else
                                    throw new FormatException();
                            }
                    }
                }
            }

            throw new FormatException();
        }

        #endregion

        #endregion

        #endregion
    }
}

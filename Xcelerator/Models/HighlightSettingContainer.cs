using System.Xml.Serialization;

namespace Xcelerator.Models
{
    /// <summary>
    /// Container for XML deserialization of highlight settings
    /// </summary>
    [XmlRoot("HighlightSettingContainer")]
    public class HighlightSettingContainer
    {
        [XmlArray("Items")]
        [XmlArrayItem("HighlightSetting")]
        public List<HighlightSettingXml> Items { get; set; } = new List<HighlightSettingXml>();
    }

    /// <summary>
    /// XML representation of a highlight setting with signed integer colors
    /// </summary>
    public class HighlightSettingXml
    {
        public int BackColor { get; set; }
        public int BorderColor { get; set; }
        public int MarkerColor { get; set; }
        public int Flags { get; set; }

        /// <summary>
        /// Converts this XML representation to a HighlightSetting with Color objects
        /// </summary>
        public HighlightSetting ToHighlightSetting()
        {
            return new HighlightSetting
            {
                BackColor = HighlightSetting.IntToColor(BackColor),
                BorderColor = HighlightSetting.IntToColor(BorderColor),
                MarkerColor = HighlightSetting.IntToColor(MarkerColor),
                Flags = Flags,
                IsSelected = false
            };
        }
    }
}

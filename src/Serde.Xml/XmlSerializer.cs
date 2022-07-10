
using System.Text;

namespace Serde.Xml;

public sealed partial class XmlSerializer
{
    public static string Serialize<T>(T s) where T : ISerialize
    {
        using var stream = new MemoryStream();
        using (var serializer = new XmlSerializer(stream))
        {
            s.Serialize(serializer);
            stream.Flush();
        }
        return Encoding.UTF8.GetString(stream.GetBuffer());
    }
}
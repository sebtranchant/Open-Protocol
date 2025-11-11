
namespace OpenProtocol.OpenProtocolTypes
{

public static class OpMessage
{
        private const string Nul = "\0";
    
    public static string Build<TEnum>(TEnum messageId, short revision, string data)
        where TEnum : Enum

        {
            short totalLength = (short)(data.Length + 20); // 20 bytes for header and terminator
        
            return $"{totalLength:D4}{Convert.ToInt16(messageId).ToString("D4")}{revision.ToString("D3")}         {data}";
        }
 
}
}
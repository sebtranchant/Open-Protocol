
namespace OpenProtocol.OpenProtocolTypes
{

public static class OpMessage
{
    private const string Nul = "\0";

    public static string Build(string length, string mid, string rev, string data)
    {
        return $"{length}{mid}{rev}         {data}{Nul}";
    }
}
}
namespace Convey.LoadBalancing.Fabio
{
    public interface IFabioOptionsBuilder
    {
        IFabioOptionsBuilder Enable(bool enabled);
        IFabioOptionsBuilder WithUrl(string url);
        IFabioOptionsBuilder WithService(string service);
        IFabioOptionsBuilder WithRequestRetries(int requestRetries);
        FabioOptions Build();
    }
}
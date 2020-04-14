namespace com.b_velop.Slipways.Web.Contracts
{
    public interface ISecretProvider
    {
        string GetSecret(string key);
    }
}
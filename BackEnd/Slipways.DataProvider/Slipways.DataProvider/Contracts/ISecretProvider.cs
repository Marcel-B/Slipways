namespace com.b_velop.Slipways.DataProvider.Contracts
{
    public interface ISecretProvider
    {
        string GetSecret(string key);
    }
}
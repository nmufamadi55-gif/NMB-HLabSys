namespace NMB_HLabSys.Services
{
    public interface IPasswordGenerator
    {
        string GenerateTemporaryPassword();
        bool IsPasswordValid(string password, out List<string> errors);
    }
}
public interface ICustomerAdapter
{
    Task<Guid> GetOrCreateCustomerAsync(string name, string phone, string email);
}
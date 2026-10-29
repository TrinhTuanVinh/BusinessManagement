
public class Customer : BaseEntity
{
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public Customer(string fullName, string email, string? phone = null, string? address = null)
    {
        FullName = NormalizeName(fullName);
        Email = NormalizeEmail(email);
        Phone = phone?.Trim();
        Address = address?.Trim();
        IsActive = true;
    }
    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Customer is inactive already!");
        }
        IsActive = false;
        Touch();
    }
    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Customer is active already");
        }
        IsActive = true;
        Touch();
    }
    public void UpdateContact(string fullName = "", string email = "", string phone = "", string address = "", bool isActive = false)
    {
        FullName = NormalizeName(fullName);
        Email = NormalizeEmail(email);
        Phone = phone?.Trim();
        Address = address?.Trim();
        Touch();
    }
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Fullname is required");
        return name.Trim();
    }
    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required");
        email = email.Trim().ToLower();
        if (!email.Contains('@'))
        {
            throw new DomainException("Email must has @ character");
        }
        return email;
    }
}
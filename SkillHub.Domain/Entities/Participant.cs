using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTime RegistrationDate { get; private set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; } = true;

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    protected Participant() { }

    public Participant(string fullName, string email, string? phoneNumber = null, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        ValidateAndSet(fullName, email, phoneNumber);
        RegistrationDate = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("Participant full name cannot be empty.");

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void ValidateAndSet(string fullName, string email, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("Participant full name cannot be empty.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainValidationException("Participant email must be a valid email address.");

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim();
    }
}

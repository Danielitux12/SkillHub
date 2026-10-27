using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Instructor
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<Workshop> Workshops { get; private set; } = new List<Workshop>();

    protected Instructor() { }

    public Instructor(string fullName, string email, string specialty, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        ValidateAndSet(fullName, email, specialty);
    }

    public void UpdateProfile(string fullName, string email, string specialty)
    {
        ValidateAndSet(fullName, email, specialty);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private void ValidateAndSet(string fullName, string email, string specialty)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("Instructor full name cannot be empty.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainValidationException("Instructor email must be a valid email address.");

        if (string.IsNullOrWhiteSpace(specialty))
            throw new DomainValidationException("Instructor specialty cannot be empty.");

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Specialty = specialty.Trim();
    }
}

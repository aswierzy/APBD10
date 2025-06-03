using System.ComponentModel.DataAnnotations;

namespace EntityFramework;

public class Account
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[^\d].*", ErrorMessage = "Username cannot start with a number.")]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    [MinLength(12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
        ErrorMessage = "Password must be at least 12 characters and include lowercase, uppercase, number, and symbol.")]
    public string Password { get; set; } = null!;

    [Required]
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required]
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
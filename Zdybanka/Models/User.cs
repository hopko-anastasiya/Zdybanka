using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public enum UserRole
{
    [Display(Name = "Адміністратор")]
    Admin,

    [Display(Name = "Менеджер організації")]
    OrganizationManager,

    [Display(Name = "Користувач")]
    User
}

public partial class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Поле ім'я обов'язкове")]
    [Display(Name = "Повне ім'я")]
    public string Fullname { get; set; } = null!;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Поле Email є обов'язковим")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Display(Name = "Хеш пароля")]
    [Required(ErrorMessage = "Хеш пароля є обов'язковим")]
    public string PasswordHash { get; set; } = string.Empty;

    [Display(Name = "Роль")]
    [Required(ErrorMessage = "Роль є обов'язковою")]
    public UserRole Role { get; set; } = UserRole.User;

    [Display(Name = "Дата створення")]
    public DateTime? Createdat { get; set; }

    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public virtual ICollection<Userfavorite> Userfavorites { get; set; } = new List<Userfavorite>();
}

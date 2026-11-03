using System.ComponentModel.DataAnnotations;

namespace CraftTrip.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class HostApplyViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên xưởng / cơ sở sản xuất.")]
    public string StudioName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hoặc nhập loại hình nghề truyền thống.")]
    public string CraftType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng giới thiệu đôi nét về truyền thống và kinh nghiệm của bạn.")]
    [StringLength(1500, MinimumLength = 20, ErrorMessage = "Giới thiệu cơ sở cần từ 20 đến 1500 ký tự.")]
    public string Bio { get; set; } = string.Empty;

    public Guid? VillageId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ cơ sở sản xuất.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số CCCD để xác thực danh tính đối tác.")]
    public string IdCardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản ngân hàng.")]
    public string BankAccountName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số tài khoản ngân hàng.")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng nhận thanh toán.")]
    public string BankName { get; set; } = string.Empty;
}

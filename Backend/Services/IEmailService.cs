public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string username);
    Task SendForgotPasswordEmailAsync(string toEmail, string resetLink);
}
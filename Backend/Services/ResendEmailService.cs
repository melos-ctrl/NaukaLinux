using Resend;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;

    public ResendEmailService(IResend resend)
    {
        _resend = resend;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string username)
    {
        var message = new EmailMessage
        {
            From = "NaukaLinux <powiadomienia@naukalinux.pl>", 
            To = { toEmail },
            Subject = "Witaj w NaukaLinux! Twoje konto jest gotowe.",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2>Cześć {username}!</h2>
                    <p>Cieszymy się, że dołączyłeś do NaukaLinux.</p>
                    <p>Twoje wirtualne środowisko jest już na Ciebie gotowe. Zaloguj się, aby rozpocząć pierwszą lekcję.</p>
                    <br/>
                    <p>Powodzenia,<br/>Zespół NaukaLinux</p>
                </div>"
        };

        await _resend.EmailSendAsync(message);
    }

    public async Task SendEmailVerificationEmailAsync(string toEmail, string username, string verificationLink)
    {
        var safeUsername = System.Net.WebUtility.HtmlEncode(username);
        var message = new EmailMessage
        {
            From = "NaukaLinux <powiadomienia@naukalinux.pl>",
            To = { toEmail },
            Subject = "Potwierdź adres e-mail - NaukaLinux",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2>Cześć {safeUsername}!</h2>
                    <p>Potwierdź swój adres e-mail, aby aktywować konto NaukaLinux.</p>
                    <p><a href='{verificationLink}'>Potwierdź adres e-mail</a></p>
                    <p>Link wygaśnie za 24 godziny. Jeśli nie zakładałeś konta, zignoruj tę wiadomość.</p>
                </div>"
        };

        await _resend.EmailSendAsync(message);
    }

    public async Task SendForgotPasswordEmailAsync(string toEmail, string resetLink)
{
    var message = new EmailMessage
    {
        From = "NaukaLinux <powiadomienia@naukalinux.pl>", 
        To = { toEmail },
        Subject = "Resetowanie hasła - NaukaLinux",
        HtmlBody = $@"
            <div style='font-family: Arial; padding: 20px;'>
                <h2>Resetowanie hasła </h2>
                <p>Otrzymaliśmy prośbę o zresetowanie hasła dla Twojego konta.</p>
                <p>Kliknij w poniższy link, aby ustawić nowe hasło. Link wygaśnie za 15 minut.</p>
                <a href='{resetLink}' style='display:inline-block; padding:10px 20px; background:#e74c3c; color:#fff; text-decoration:none; border-radius:5px;'>Zresetuj hasło</a>
                <p><small>Jeśli to nie Ty prosiłeś o reset, zignoruj tę wiadomość.</small></p>
            </div>"
    };

    await _resend.EmailSendAsync(message);
}

}
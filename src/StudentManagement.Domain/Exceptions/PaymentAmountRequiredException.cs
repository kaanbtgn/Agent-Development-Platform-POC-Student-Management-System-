namespace StudentManagement.Domain.Exceptions;

/// <summary>
/// Belirtilen dönem için mevcut bir ödeme kaydı yokken tutar (amount) belirtilmeden
/// ödeme oluşturulmaya çalışıldığında fırlatılır.
/// </summary>
public sealed class PaymentAmountRequiredException : DomainException
{
    public PaymentAmountRequiredException(Guid studentId, int year, int month)
        : base($"'{studentId}' için {year}/{month} döneminde mevcut bir ödeme kaydı yok; " +
               "yeni kayıt oluşturmak için tutar (amount) belirtilmelidir.") { }
}

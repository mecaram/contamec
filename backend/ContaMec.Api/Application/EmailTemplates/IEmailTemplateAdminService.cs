using ContaMec.Api.Application.EmailTemplates.Dto;

namespace ContaMec.Api.Application.EmailTemplates;

/// <summary>ABM de la tabla EmailTemplates. El envío usa <see cref="Notifications.IEmailTemplateService"/>.</summary>
public interface IEmailTemplateAdminService
{
    Task<List<EmailTemplateResponse>> SearchAsync(EmailTemplateSearchRequest request);
    Task<EmailTemplateResponse?> GetByIdAsync(int id);
    Task<EmailTemplateResponse> CreateAsync(EmailTemplateSaveRequest request);
    Task<bool> UpdateAsync(int id, EmailTemplateSaveRequest request);
    Task<bool> DeleteAsync(int id);
}

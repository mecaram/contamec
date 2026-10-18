using ContaMec.Api.Application.CompanyRegistration.Dto;

namespace ContaMec.Api.Application.CompanyRegistration;

public interface ICompanyRegistrationService
{
    Task RegisterAsync(CompanyRegistrationRequest request);
}

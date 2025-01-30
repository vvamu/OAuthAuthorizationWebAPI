using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;

namespace OAuthAuthorizationWebAPI.Application;

public interface IApplicationUserService
{
    public Task<ApplicationUser> AuthenticateAsync(LoginViewModel model);
    public Task<LoginViewModel> CreateAsync(LoginViewModel model);
    public Task<IEnumerable<LoginViewModel>> GetAllAsync();

    public Task<ApplicationUser> GetAsync(Guid id);

}

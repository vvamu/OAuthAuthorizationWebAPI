using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OAuthAuthorizationWebAPI.Persistence;

namespace OAuthAuthorizationWebAPI.Application;
public class ApplicationUserService : IApplicationUserService
{
    private readonly ApplicationDbContext _context;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationUserService(ApplicationDbContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _signInManager = signInManager;
        _userManager = userManager;
    }
    public async Task<ApplicationUser> AuthenticateAsync(LoginViewModel model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == model.Login.Trim());
        if (user == null)
        {
            throw new Exception("User does not exist with this login");
        }
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            throw new Exception("Password is incorrect");
        }
        return new ApplicationUser() { Id = user.Id, Login = model.Login, PasswordHash = user.PasswordHash };
    }
    public async Task<LoginViewModel> CreateAsync(LoginViewModel model)
    {
        model.CheckValid();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == model.Login.Trim());
        if (user != null)
        {
            throw new Exception("User with this login already exists.");
        }

        user = new ApplicationUser { Login = model.Login.Trim(), UserName = model.Login.Trim() };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var error = "";
            result.Errors.ToList().ForEach(x => error += x.Description + " ");
            throw new Exception(error);
        }

        return new LoginViewModel() { Login = model.Login, Password = user.PasswordHash };

    }

    public async Task<IEnumerable<LoginViewModel>> GetAllAsync()
    {
        var users = await _context.Users.Select(x => new LoginViewModel() { Login = x.Login, Password = x.PasswordHash }).ToListAsync();
        return users.AsEnumerable();

    }

    public async Task<ApplicationUser> GetAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        return user ?? throw new Exception("User not found");
    }


}

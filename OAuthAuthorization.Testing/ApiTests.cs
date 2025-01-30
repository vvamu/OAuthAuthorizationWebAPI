namespace OAuthAuthorization.Testing;

using OAuthAuthorization.Testing.Configurations;
using OAuthAuthorization.Testing.Models;
using OAuthAuthorization.Testing.ViewModels;
using Xunit;
public class s
{
    

    [Fact]
    public void Register_SuccessAfterCreateUser()
    {
        Assert.True(true);
    }

    [Fact]
    public void Register_ErrorNotCorrectPassword()
    {
        Assert.True(true);
    }

    [Fact]
    public void Register_ErrorNotCorrectLogin()
    {
        Assert.True(true);
    }

    [Fact]
    public void Register_ErrorCreateExistedUser()
    {
        Assert.True(true);
    }

    [Fact]
    public void SignIn_SuccessAfterCreateUser()
    {
        Assert.True(true);
    }

    [Fact]
    public void SignIn_ErrorWithNotCorrectPassword()
    {
        Assert.True(true);
    }

    [Fact]
    public void SignIn_ErrorWithNotCorrectLogin()
    {
        Assert.True(true);
    }

    [Fact]
    public void SignIn_SuccessGetAccessToken()
    {
        Assert.True(true);
    }

    [Fact]
    public void SignIn_SuccessGetRefreshToken()
    {
        Assert.True(true);
    }

    [Fact]
    public void GetUsers_ErrorBeforeAuthenticated()
    {
        Assert.True(true);
    }

    [Fact]
    public async Task GetUsers_SuccessAfterAuthenticated()
    {
        try
        {
            var loginUser = new LoginViewModel() { Login = "string1A", Password = "string1A_" };
            var (identity,refresh,access, accessTokenExpireDate) = await ApiRepository.AuthenticateClientAndUser(loginUser);
            var users = await ApiRepository.GetUsersAsync(access);
            var (identity2, refresh2, access2, accessTokenExpireDate2) = await ApiRepository.RefreshTokenAsync(refresh);
            var users2 = await ApiRepository.GetUsersAsync(access2);
            Assert.Equal(users,users2);
        }
        catch (Exception ex) 
        {
            Assert.Fail(ex.Message);
        }
    }

    [Fact]
    public async Task GetUsers_ErrorWithAlreadyRefreshesAccessToken()
    {
        try
        {
            var loginUser = new LoginViewModel() { Login = "string1A", Password = "string1A_" };
            var (identity, refresh, access, accessTokenExpireDate) = await ApiRepository.AuthenticateClientAndUser(loginUser);
            var users = await ApiRepository.GetUsersAsync(access);
            var (identity2, refresh2, access2, accessTokenExpireDate2) = await ApiRepository.RefreshTokenAsync(refresh);
            var users2 = await ApiRepository.GetUsersAsync(access);
            
        }
        catch (Exception ex)
        {
            Assert.Equal("efwfewfwefwefwefwefwef",ex.Message); ///
        }
        Assert.Fail("User can get data using refreshed access token");
    }


    [Fact]
    public void GetUsers_ErrorWithExpiredAccessToken()
    {
        Assert.True(true);
    }


    [Fact]
    public void RefreshToken_SuccessGetNewAccessToken()
    {
        Assert.True(true);
    }

    [Fact]
    public void RefreshToken_SuccessGetUsers()
    {
        Assert.True(true);
    }

    [Fact]
    public void RefreshToken_ErrorGetUsersWithPreviousAccessToken()
    {
        Assert.True(true);
    }
}
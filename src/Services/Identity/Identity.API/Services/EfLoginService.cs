using System.Threading.Tasks;
using Identity.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Identity.API.Services
{
    public class EfLoginService : ILoginService<ApplicationUser>
    {   

        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;


        public  EfLoginService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager){
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public async Task<ApplicationUser> FindByUsername(string user)
        {
            return await _userManager.FindByEmailAsync(user);
        }

        public  Task SignIn(ApplicationUser user)
        {
            return _signInManager.SignInAsync(user,true);
        }

        public Task SignInAsync(ApplicationUser user, AuthenticationProperties properties, string authenticationMethod = null)
        {
            return _signInManager.SignInAsync(user, properties, authenticationMethod);
        }

        public async Task<bool> ValidateCredentials(ApplicationUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }
    }
}
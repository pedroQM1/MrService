using System;
using System.Threading.Tasks;
using IdentityServer4;
using Identity.API.Models;
using Identity.API.Services;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MrService.Services.Identity.Identity.API.Models.AccountViewModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Identity.API.Controllers{


    public class AccountController : Controller {

        private readonly ILoginService<ApplicationUser> _loginService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clienteStorage;
        private readonly ILogger _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            ILoginService<ApplicationUser> loginService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            ILogger<AccountController> logger,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration
        ){
            _logger = logger;
            _loginService = loginService;
            _interaction = interaction;
            _clienteStorage  = clientStore;
            _userManager = userManager;
            _configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl){
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if(context?.IdP == null)
                    throw new NotImplementedException("External login is not implemented!");

            var vm = await BuildLoginViewModelAsync(returnUrl, context);

            ViewData["ReturnUrl"] = returnUrl;

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel){
            if(ModelState.IsValid){

                var user = await _loginService.FindByUsername(loginViewModel.Email);
                if((await _loginService.ValidateCredentials(user,loginViewModel.Password))){

                    var tokeLifeTime = _configuration.GetValue("TokenLifetimeMinutes",120);
                    
                    var props = new AuthenticationProperties{

                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(tokeLifeTime),
                        AllowRefresh = true,
                        RedirectUri = loginViewModel.ReturnUrl  
                    };

                    if(loginViewModel.RememberMe){

                        var permanentTokenLifetime = _configuration.GetValue("PermanentTokenLifetimeDays", 365);
                        props.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(permanentTokenLifetime);
                        props.IsPersistent = true;
                    }

                    await _loginService.SignInAsync(user,props);

                    if(_interaction.IsValidReturnUrl(loginViewModel.ReturnUrl)){
                        return Redirect(loginViewModel.ReturnUrl);
                    }
                    Redirect("~/");   
                }
                ModelState.AddModelError("","Invalid userName or password");
            }

            var vm = await BuildLoginViewModelAsync(loginViewModel);
            ViewData["ReturnUrl"] = loginViewModel.ReturnUrl;
            return View(vm);
        }
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl,AuthorizationRequest context){
            var allowLocal = true;
            if(context?.Client.ClientId != null){
                var cliente = await _clienteStorage.FindEnabledClientByIdAsync(context.Client.ClientId);
                if(cliente != null){
                    allowLocal = cliente.EnableLocalLogin;
                }
            }  

            return new LoginViewModel{
              ReturnUrl = returnUrl,
              Email = context?.LoginHint  
            };  
        }
        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model){
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl,context);
            vm.Email = model.Email;
            vm.RememberMe = model.RememberMe;
            return vm;
        }
        [HttpGet]
        public async Task<ActionResult> Logout(string logoutId){
            if(User.Identity.IsAuthenticated == false){
                return await Logout(new LogoutViewModel{LogoutId = logoutId});
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if(context?.ShowSignoutPrompt == false){
                return await Logout(new LogoutViewModel{LogoutId = logoutId});
            }


            return View(new LogoutViewModel{
                LogoutId = logoutId
            });
            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Logout(LogoutViewModel model){

            var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            if(idp != null  && idp != IdentityServerConstants.LocalIdentityProvider){
                if(model.LogoutId is null){
                    model.LogoutId = await _interaction.CreateLogoutContextAsync();
                }
                string url = "/Account/Logout?logoutId=" + model.LogoutId;
                 try
                {

                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = url
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LOGOUT ERROR: {ExceptionMessage}", ex.Message);
                }
            }
             await HttpContext.SignOutAsync();

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            return Redirect(logout?.PostLogoutRedirectUri);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model,string returnUrl = null){
            ViewData["ReturnUrl"] = returnUrl;
            if(ModelState.IsValid){
                var user  = new ApplicationUser(){
                    Email = model.Email,
                    UserName = model.Email,
                };
                var result = await _userManager.CreateAsync(user,model.Password);
                if(result.Errors.Any()){
                    AddError(result);
                    return View(model);
                }
            }    
            if(returnUrl != null){
                if(HttpContext.User.Identity.IsAuthenticated)
                    return Redirect(returnUrl);
                else 
                    if(ModelState.IsValid)
                        return RedirectToAction("login","account",new {returnUrl = returnUrl});
                else 
                    return View(model);
            }
            return RedirectToAction("index","home");       
        }
        private void AddError(IdentityResult result){
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty,error.Description);
            }
        }
        [HttpGet]
        public IActionResult Redirecting()
        {
            return View();
        }

       
    }
}
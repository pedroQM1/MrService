using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Identity.API.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Options;
using Identity.API.Services;
using MrService.Services.Identity.Identity.API.Models;

namespace Identity.API.Controllers
{
    public class HomeController : Controller
    {   

        private readonly IIdentityServerInteractionService _interaction;
        private readonly IOptionsSnapshot<AppSettings> _setting;
        private readonly ILogger<HomeController> _logger;
        private readonly IRedirectService _redirectSvc;

        public HomeController(
            IIdentityServerInteractionService interaction,
            IOptionsSnapshot<AppSettings> setting,
            ILogger<HomeController> logger,
            IRedirectService redirectSvc
        ){
            _interaction = interaction;
            _setting = setting;
            _logger = logger;
            _redirectSvc = redirectSvc;
        }

        public IActionResult Index(string returnUrl)
        {
            return View();
        }
        public IActionResult  ReturnToOriginalApplication(string returnUrl){
            if (returnUrl != null)
                return Redirect(_redirectSvc.ExtractRedirectUriFromReturnUrl(returnUrl));
            else
                return RedirectToAction("Index", "Home");   
        }
       
         public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;
            }

            return View("Error", vm);
        }
    }
}

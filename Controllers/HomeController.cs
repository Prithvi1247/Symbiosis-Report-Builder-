using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BetaSnapReporting.Models;
using BetaSnapReporting.ViewModels.Home;

namespace BetaSnapReporting.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var viewModel = new DashboardViewModel();
        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
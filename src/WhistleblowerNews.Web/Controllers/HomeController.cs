using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Web.Models;

namespace WhistleblowerNews.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }
}

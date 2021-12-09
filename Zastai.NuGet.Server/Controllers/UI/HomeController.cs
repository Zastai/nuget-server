using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Models;

namespace Zastai.NuGet.Server.Controllers.UI;

/// <summary>The controller for the main home page.</summary>
public class HomeController : UIController<HomeController> {

  /// <summary>Creates a new home page controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public HomeController(ILogger<HomeController> logger) : base(logger) {
  }

  /// <summary>Shows the main home page.</summary>
  /// <returns>The main home page.</returns>
  [Route("")]
  public IActionResult Index() {
    return this.View();
  }

  /// <summary>Shows the privacy policy page.</summary>
  /// <returns>The privacy policy page.</returns>
  [Route("privacy")]
  public IActionResult Privacy() {
    return this.View();
  }

  /// <summary>Shows the error page.</summary>
  /// <returns>The error page.</returns>
  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  [Route("error")]
  public IActionResult Error() {
    return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
  }

}

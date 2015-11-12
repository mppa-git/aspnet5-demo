using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Shell.Models;
using Shell.Middlewares;
using Microsoft.AspNet.Http.Features;

namespace Shell.Controllers
{
    public class HomeController : Controller
    {
        public TestDbContext TestDbContext { get; private set; }

        public HomeController(TestDbContext db)
        {
            this.TestDbContext = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            var row = new Test() { Name = "test" };
            this.TestDbContext.Add(row);
            this.TestDbContext.SaveChanges();

            var tenant = this.HttpContext.Features.Get<ITenantFeature>();

            ViewData["Message"] = string.Format(
                "Your {0} application description page.  {1} tests run.",
                tenant.Tenant.Id,
                this.TestDbContext.Tests.Count());

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}

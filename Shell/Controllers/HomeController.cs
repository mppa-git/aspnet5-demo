using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Shell.Models;

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
            ViewData["Message"] = "Your application description page.";

            var row = new Test() { Name = "test" };
            this.TestDbContext.Add(row);
            this.TestDbContext.SaveChanges();

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

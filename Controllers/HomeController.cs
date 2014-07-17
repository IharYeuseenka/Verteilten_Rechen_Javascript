using DistributeHashing.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DistributeHashing.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataService _dataService;

        public HomeController()
        {
            _dataService = DataService.Init();
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetTask() // отправляет данные клиенту в формате JSON
        {
            var taskData = _dataService.GetEntities();

            return taskData == null
                ? Json(new { success = false }) // если данные небыли получены сообщаем об этом клиенту 
                : Json(new { data = taskData }); // если все хорошо, оттправляем клиенту данные для обработки
        }

        [HttpPost]
        public JsonResult Save(IEnumerable<EntityModel> data) // сохранение обработанных данных
        {
            var result = _dataService.SaveEntities(data);

            return Json(new { success = result }); //сообщаем клиенту что данные сохранены
        }
    }
}

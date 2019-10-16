using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using CTMS.ViewModel;
using CTMS.Common;

namespace CTMS.Controllers
{
    public class RecursivePendingTrainingController : Controller
    {
        #region MyRecursivePendingTraining
        [Filters.MenuAccess()]
        public ActionResult ViewMyRecursivePendingTraining()
        {
            ViewRecursivePendingTrainingModel objViewRecursivePendingTrainingModel = new ViewRecursivePendingTrainingModel();
            RecursivePendingTrainingViewModel objRecursivePendingTrainingViewModel = new RecursivePendingTrainingViewModel();

            objViewRecursivePendingTrainingModel.CurrentPage = 1;
            objViewRecursivePendingTrainingModel.PageSize = CommonUtils.PageSize;

            objViewRecursivePendingTrainingModel.RoleID = Convert.ToInt32(Session["RoleId"]);

            objViewRecursivePendingTrainingModel.Message = "";
            objViewRecursivePendingTrainingModel.MessageType = "";

            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                objViewRecursivePendingTrainingModel.Message = Convert.ToString(Session["Message"]);
                objViewRecursivePendingTrainingModel.MessageType = Convert.ToString(Session["MessageType"]);
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
            objViewRecursivePendingTrainingModel.TotalPages = 0;
            objRecursivePendingTrainingViewModel.GetAllRecursivePendingTraining(objViewRecursivePendingTrainingModel);
            return View(objViewRecursivePendingTrainingModel);
        }
        [Filters.Authorized()]
        [HttpPost]
        public ActionResult ViewMyRecursivePendingTraining(ViewRecursivePendingTrainingModel objViewRecursivePendingTrainingModel)
        {

            RecursivePendingTrainingViewModel objRecursivePendingTrainingViewModel = new RecursivePendingTrainingViewModel();
            // objViewTrainersModel.Message = objMasterViewModel.MessageType = String.Empty;
            objRecursivePendingTrainingViewModel.GetAllRecursivePendingTraining(objViewRecursivePendingTrainingModel);
            return PartialView("MyRecursivePendingTrainingList", objViewRecursivePendingTrainingModel);
        }
        #endregion
    }
}

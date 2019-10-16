using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Common;
using CTMS.ViewModel;
using CTMS.Models;

namespace CTMS.Controllers
{
    public class ReoccurrenceController : Controller
    {


        #region Properties
        public int createdBy
        {
            get
            {
                int _userID = 0;
                if (Session["UserId"] != null)
                    int.TryParse(Convert.ToString(Session["UserId"]), out _userID);
                return _userID;
            }
        }
        #endregion

        CommonUtils objCommonUtilError = new CommonUtils();
        //
        // GET: /Reoccurrence/
        #region AssignReoccurrence
        /// <summary>
        /// Assign Reoccurrence to Training based on Training ID
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <returns></returns>
        public ActionResult AssignReoccurrence(int tID)
        {
            ReoccurrenceDetailModel objAssignReoccurrenceModel = new ReoccurrenceDetailModel();
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            if (Session["Message"] != null && Session["MessageType"] != null)
            {
                Session["Message"] = null;
                Session["MessageType"] = null;
            }
            if (Session["msgContent"] != null && Session["msgContentType"] != null)
            {
                objAssignReoccurrenceModel.Message = Convert.ToString(Session["msgContent"]);
                objAssignReoccurrenceModel.MessageType = Convert.ToString(Session["msgContentType"]);
                Session["msgContent"] = null;
                Session["msgContentType"] = null;

            }

            try
            {
                objAssignReoccurrenceModel.TrainingData = objTrainingViewModel.GetTrainingByTrainingID(tID);
                if (objAssignReoccurrenceModel.TrainingData == null)
                {
                    objAssignReoccurrenceModel.TrainingData = new SaveTrainingModel();
                }
                objAssignReoccurrenceModel.ReoccurenceList = objTrainingViewModel.GetAssignedReoccurrence(objAssignReoccurrenceModel).ToList();

                FillJobRole(objAssignReoccurrenceModel.TrainingData.TrainingID);

                if (objAssignReoccurrenceModel.TrainingData.PublishCount > 0)
                {
                    objAssignReoccurrenceModel.Message = Resources.TrainingResource.msgTrainingPublishNotification;
                    objAssignReoccurrenceModel.MessageType = MessageType.Notice.ToString().ToLower();
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignReoccurrence Get", ex);
            }
            return View(objAssignReoccurrenceModel);
        }

        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult AssignReoccurrence(ReoccurrenceDetailModel objAssignReoccurrenceListModel)
        {
            try
            {
                TrainingViewModel objTrainingViewModel = new TrainingViewModel();                

                if (objAssignReoccurrenceListModel.ActionType != null)
                {
                    switch (objAssignReoccurrenceListModel.ActionType)
                    {
                        case "AddReoccurrence":                            
                            objAssignReoccurrenceListModel.IsActive = true;
                            objAssignReoccurrenceListModel.CreatedBy = createdBy;
                            objAssignReoccurrenceListModel.CreatedOn = DateTime.Now;
                            objTrainingViewModel.InsertUpdateReoccurrence(objAssignReoccurrenceListModel);
                            break;
                        case "deleteReoccurrence":
                            objTrainingViewModel.DeleteAssignedReoccurrence(objAssignReoccurrenceListModel);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Training", "AssignReoccurrence Post", ex);
            }
            Session["msgContent"] = objAssignReoccurrenceListModel.Message;
            Session["msgContentType"] = objAssignReoccurrenceListModel.MessageType;
            return RedirectToAction("AssignReoccurrence", "Reoccurrence", new { tID = objAssignReoccurrenceListModel.TrainingData.TrainingID });
        }
        #endregion

        public void FillJobRole(int trainingId)
        {
            TrainingViewModel objTrainingViewModel = new TrainingViewModel();
            IList<ReoccurrenceModel> lstFunction = objTrainingViewModel.GetReoccurrenceRoleByTrainingID(trainingId, true);

            ViewBag.JobRoleList = new SelectList(lstFunction, "RoleID", "RoleName", 0);
        }

    }
}

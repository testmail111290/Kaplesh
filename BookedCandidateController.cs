/*
* @author  : Soumen Bhowmick
* @version : 0.0.0.1
* @since   : 29th May 2014 
* 
* Modification History :
* Date of Modification		Modified By			Changes made 
* -------------------------------------------------------------------------------------------------------
* 
*                                              
*/
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
	
    public class BookedCandidateController : Controller
    {
		CommonUtils objCommonUtilError = new CommonUtils();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ScheduledTrainingId"></param>
		/// <param name="CallingModule">From which module it is called, 'PM' for Program manager, 'M' for Manager  </param>
		/// <param name="Message"></param>
		/// <param name="MessageType"></param>
		/// <returns></returns>
		public ActionResult ViewBookedCandidate(int ScheduledTrainingId, string CallingModule, string Message="", string MessageType="")
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();

			ViewScheduledTrainingDetails objViewScheduledTrainingDetails = new ViewScheduledTrainingDetails();
			objViewScheduledTrainingDetails.CurrentPage = 1;
			objViewScheduledTrainingDetails.PageSize = CommonUtils.PageSize;
			objViewScheduledTrainingDetails.TotalPages = 0;

			objBookedCandidateViewModel.GetTrainingDetailsByScheduledTrainingID(ScheduledTrainingId, objViewScheduledTrainingDetails);

            DateTime scheduledBeforeStartDate = objViewScheduledTrainingDetails.ScheduleTraining.StartDateTime.AddHours(-CommonUtils.RestrictEditBeforeHours);
           if (!String.IsNullOrEmpty(objViewScheduledTrainingDetails.ScheduleTraining.ProgramStatusName) && (objViewScheduledTrainingDetails.ScheduleTraining.ProgramStatusName.ToLower() == "in progress" || objViewScheduledTrainingDetails.ScheduleTraining.ProgramStatusName.ToLower() == "completed"))
            {
                objViewScheduledTrainingDetails.ScheduleTraining.isEditable = false;
                objViewScheduledTrainingDetails.Message = String.Format(Resources.TrainingResource.msgBookedCandidateNotEdit, objViewScheduledTrainingDetails.ScheduleTraining.ProgramStatusName);
                objViewScheduledTrainingDetails.MessageType = CTMS.Common.MessageType.Notice.ToString().ToLower();
            } 
            else if (DateTime.Now >= scheduledBeforeStartDate)
            {
                objViewScheduledTrainingDetails.ScheduleTraining.isEditable = false;
                //objViewScheduledTrainingDetails.Message = String.Format(Resources.TrainingResource.UpdateRestrictionMessage, CommonUtils.RestrictEditBeforeHours.ToString());
                //objViewScheduledTrainingDetails.MessageType = CTMS.Common.MessageType.Notice.ToString().ToLower();
                objViewScheduledTrainingDetails.Message = Message;
                objViewScheduledTrainingDetails.MessageType = MessageType;

            }
            
            else
            {
                objViewScheduledTrainingDetails.ScheduleTraining.isEditable = true;
                objViewScheduledTrainingDetails.Message = Message;
                objViewScheduledTrainingDetails.MessageType = MessageType;
            }
			
			ViewBag.CallingModule = CallingModule;
			Session["CallingModule"] = CallingModule;
			if (TempData["TrainingID"] != null)
			{
				ViewBag.TrainingId = (int)TempData["TrainingID"];
				TempData.Keep("TrainingID");
			}
			return View(objViewScheduledTrainingDetails);
		}

		[HttpPost]
        [Filters.Authorized()]
		public ActionResult ViewBookedCandidate(ViewScheduledTrainingDetails objViewScheduledTrainingDetails)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			
			objBookedCandidateViewModel.GetTrainingDetailsByScheduledTrainingID(objViewScheduledTrainingDetails.ScheduleTraining.ScheduledTrainingID, objViewScheduledTrainingDetails);

			return PartialView("_BookedCandidateListPartial", objViewScheduledTrainingDetails);
			
		}

		
		public ActionResult AddNewCandidate(int id)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			ViewAddCandidateModel objViewAddCandidateModel = new ViewAddCandidateModel();
			int UserId, RoleId;
			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			objViewAddCandidateModel.ScheduledTrainingId = id;
			objViewAddCandidateModel.UserType = 1;

			objViewAddCandidateModel.CurrentPage = 1;
			objViewAddCandidateModel.PageSize = CommonUtils.PageSize;
			objViewAddCandidateModel.TotalPages = 0;
			objBookedCandidateViewModel.GetVLMSCandidateList(UserId, RoleId,id, objViewAddCandidateModel);
			return PartialView("_AddNewCandidatePartial", objViewAddCandidateModel);
		}

		public ActionResult DeleteBookedCandidate(int BookedCandidateId, int ScheduledTrainingID)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			int errorCode;
			string errorMessage, messageType,redirectUrl, strCallingModule = string.Empty;
			objBookedCandidateViewModel.DeleteCandidate(BookedCandidateId, out errorCode, out errorMessage);
			
			if (errorCode.Equals(0))
				messageType = MessageType.Success.ToString().ToLower();
			else
				messageType = MessageType.Error.ToString().ToLower();

			if (Session["CallingModule"] != null)
				strCallingModule = Session["CallingModule"].ToString();

			if (TempData["TrainingID"] != null)
				TempData.Keep("TrainingID");

			redirectUrl = "ViewBookedCandidate?ScheduledTrainingId=" + ScheduledTrainingID.ToString() + "&CallingModule=" + strCallingModule + "&Message=" + errorMessage + "&MessageType=" + messageType;


			Session["CallingModule"] = null;
			// Url.Action("ViewBookedCandidate", "BookedCandidate", new { ScheduledTrainingId = objViewAddCandidateModel.ScheduledTrainingId });

			return Json(redirectUrl, JsonRequestBehavior.AllowGet);

		}
		[HttpPost]
        [Filters.Authorized()]
		public ActionResult SearchVLMSCandidateList(ViewAddCandidateModel objViewAddCandidateModel)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			int UserId, RoleId;
			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);

			objBookedCandidateViewModel.GetVLMSCandidateList(UserId, RoleId, objViewAddCandidateModel.ScheduledTrainingId, objViewAddCandidateModel);

            if (objViewAddCandidateModel != null && objViewAddCandidateModel.VLMSCandidateList != null)
            {
                foreach (var candidate in objViewAddCandidateModel.VLMSCandidateList)
                {
                    if (!String.IsNullOrEmpty(objViewAddCandidateModel.SelectedCandidateList))
                    {
                        if (objViewAddCandidateModel.SelectedCandidateList.Split(',').ToList().Contains(candidate.CandidateId.ToString()))
                        {
                            candidate.IsSelected = true;
                        }
                    }
                }
            }
			return PartialView("_VLMSCandidateListPartial", objViewAddCandidateModel);
		}

		public ActionResult AddCandidatesToSchedule(ViewAddCandidateModel objViewAddCandidateModel)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			int UserId, RoleId,errorCode;
			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);
			string errorMessage;
			string redirectUrl = string.Empty;
			string CandidateIds = string.Empty;
            if (objViewAddCandidateModel.VLMSCandidateList != null && objViewAddCandidateModel.VLMSCandidateList.Count > 0)
                //CandidateIds=String.Join(",", objViewAddCandidateModel.VLMSCandidateList.Where(p=>p.IsSelected==true).Select(p=>p.CandidateId));
                CandidateIds = objViewAddCandidateModel.SelectedCandidateList;
			string CallingModule = string.Empty;

			if (Session["CallingModule"] != null)
				CallingModule = Session["CallingModule"].ToString();

			if (TempData["TrainingID"] != null)
			{
				ViewBag.TrainingId = (int)TempData["TrainingID"];
				TempData.Keep("TrainingID");
			}
			objBookedCandidateViewModel.AddNewCandidate(UserId
															, objViewAddCandidateModel.UserType
															, objViewAddCandidateModel.ScheduledTrainingId
															, CandidateIds
															, objViewAddCandidateModel.NonVLMSCandidateDetails.CandidateName
															, objViewAddCandidateModel.NonVLMSCandidateDetails.EmailId
															, objViewAddCandidateModel.NonVLMSCandidateDetails.ContactNo
															, objViewAddCandidateModel.NonVLMSCandidateDetails.Company
															, objViewAddCandidateModel.NonVLMSCandidateDetails.DomainName
															, out errorCode, out errorMessage);
			
			if (errorCode.Equals(0))
			{
				
				TempData["Message"] = errorMessage;
				TempData["MessageType"] = MessageType.Success.ToString().ToLower();
			}
			else
			{
				TempData["Message"] = errorMessage;
				TempData["MessageType"] = MessageType.Error.ToString().ToLower();
				
			}

			redirectUrl = "ViewBookedCandidate?ScheduledTrainingId=" + objViewAddCandidateModel.ScheduledTrainingId.ToString() + "&CallingModule=" + CallingModule + "&Message=" + TempData["Message"] + "&MessageType=" + TempData["MessageType"];


			Session["CallingModule"] = null;
			// Url.Action("ViewBookedCandidate", "BookedCandidate", new { ScheduledTrainingId = objViewAddCandidateModel.ScheduledTrainingId });
			
			return Json(new { url = redirectUrl, message = TempData["Message"], messageType = TempData["MessageType"] }, JsonRequestBehavior.AllowGet);
			
		}

		public ActionResult GetCandidateName()
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			List<NonVLMSCandidate> lstNonVLMSCandidate = new List<NonVLMSCandidate>();
			
			lstNonVLMSCandidate = objBookedCandidateViewModel.GetNonVLMSCandidateList().ToList<NonVLMSCandidate>();
			List<string> lstCandidate = lstNonVLMSCandidate.Select(p => (p.CandidateName +"-"+p.EmailId)).ToList<string>() ;
			
			return Json(lstCandidate, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetCandidateDetails(string Name, string Email)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			List<NonVLMSCandidate> lstNonVLMSCandidate = new List<NonVLMSCandidate>();
			string ContactNo, Company, DomainName;
			ContactNo = Company = DomainName = string.Empty;
			lstNonVLMSCandidate = objBookedCandidateViewModel.GetNonVLMSCandidateList().Where(p=>p.CandidateName==Name && p.EmailId==Email).ToList<NonVLMSCandidate>();
			if (lstNonVLMSCandidate != null && lstNonVLMSCandidate.Count > 0)
			{
				ContactNo = lstNonVLMSCandidate[0].ContactNo;
				Company = lstNonVLMSCandidate[0].Company;
				DomainName = lstNonVLMSCandidate[0].DomainName;
			}

			return Json(new { contactno = ContactNo, company = Company, domain = DomainName }, JsonRequestBehavior.AllowGet);
		}

		public JsonResult checkemailexists(string EmailId, string CandidateName)
		{
			BookedCandidateViewModel objBookedCandidateViewModel = new BookedCandidateViewModel();
			List<NonVLMSCandidate> lstNonVLMSCandidate = new List<NonVLMSCandidate>();
			string Name;
			Name = string.Empty;
			if (string.IsNullOrEmpty(EmailId) || string.IsNullOrEmpty(CandidateName))
			{
				return Json(true, JsonRequestBehavior.AllowGet);
			}
			else
			{

				lstNonVLMSCandidate = objBookedCandidateViewModel.GetNonVLMSCandidateList().Where(p => p.EmailId == EmailId).ToList<NonVLMSCandidate>();
				if (lstNonVLMSCandidate != null && lstNonVLMSCandidate.Count > 0)
				{
					Name = lstNonVLMSCandidate[0].CandidateName;
				}
				if (CandidateName == Name || Name=="")
					return Json(new { response = true  }, JsonRequestBehavior.AllowGet);
				else
					return Json(new { response = false }, JsonRequestBehavior.AllowGet);
			}
		}
    }
}

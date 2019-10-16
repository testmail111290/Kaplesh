/*
* @author  : Soumen Bhowmick
* @version : 0.0.0.1
* @since   : 23rd May 2014 
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
    public class PendingApprovalController : Controller
    {
		CommonUtils objCommonUtilError = new CommonUtils();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
        [Filters.MenuAccess()]
		public ActionResult ViewPendingApproval(string Message = "", string MessageType = "")
		{
			int UserId, RoleId;
			ViewPendingApprovalModel objViewPendingApprovalModel = new ViewPendingApprovalModel();
			PendingApprovalViewModel objPendingApprovalViewModel = new PendingApprovalViewModel();

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);
			BindStatusDDL();

			objViewPendingApprovalModel.Message = Message;
			objViewPendingApprovalModel.MessageType = MessageType;
			
            

			objViewPendingApprovalModel.CurrentPage = 1;
			objViewPendingApprovalModel.PageSize = CommonUtils.PageSize;
			objViewPendingApprovalModel.TotalPages = 0;
			//objViewPendingApprovalModel.PendingApprovals = new List<PendingApprovalModel>();
			
			objPendingApprovalViewModel.GetAllPendingApproval(UserId, RoleId, objViewPendingApprovalModel);

			return View(objViewPendingApprovalModel);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpPost]
        [Filters.Authorized()]
       public ActionResult ViewPendingApproval(ViewPendingApprovalModel objViewPendingApprovalModel)
		{
			PendingApprovalViewModel objPendingApprovalViewModel = new PendingApprovalViewModel();
			int UserId, RoleId;

			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);
			BindStatusDDL();
			objPendingApprovalViewModel.GetAllPendingApproval(UserId, RoleId, objViewPendingApprovalModel);

			return PartialView("_PendingApprovalGrid", objViewPendingApprovalModel);
			//return View(objViewPendingApprovalModel);
		}

		[HttpPost]
        [Filters.Authorized()]
		public ActionResult SubmitApproval(ViewPendingApprovalModel objViewPendingApprovalModel)
		{
			PendingApprovalViewModel objPendingApprovalViewModel = new PendingApprovalViewModel();
			int UserId, RoleId, errorCode;
			string errorMessage;
			int.TryParse(Session["RoleId"].ToString(), out RoleId);
			int.TryParse(Session["UserId"].ToString(), out UserId);
			string redirectUrl = string.Empty;
			try
			{
				objPendingApprovalViewModel.SaveAllPendingApproval(objViewPendingApprovalModel.PendingApprovals, UserId, out errorCode, out errorMessage);

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

				//string strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + errorMessage + "</div>";
				//@ViewBag.RetMessage = strRetMessage;
			}
			catch (Exception ex)
			{
				Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "PendingApproval", "SubmitApproval", ex);
			}

			redirectUrl = "ViewPendingApproval?Message=" + TempData["Message"] + "&MessageType=" + TempData["MessageType"];
			return Json(new { url = redirectUrl, message = TempData["Message"], messageType = TempData["MessageType"] }, JsonRequestBehavior.AllowGet);
			//return RedirectToAction("ViewPendingApproval");

		}


        #region OnlineTrainingApproval
        /// <summary>
        /// Get Pending Approval for online Training
        /// </summary>
        /// <returns></returns>
         [Filters.MenuAccess()]
        public ActionResult ViewOnlinePendingApproval()
        {
            int UserId = 0, RoleId = 0;
            ViewPendingApprovalModel objViewPendingApprovalModel = new ViewPendingApprovalModel();
            PendingApprovalViewModel objPendingApprovalViewModel = new PendingApprovalViewModel();

            try
            {
               
                int.TryParse(Session["RoleId"].ToString(), out RoleId);
                int.TryParse(Session["UserId"].ToString(), out UserId);
                BindStatusDDL();

                objViewPendingApprovalModel.CurrentPage = 1;
                objViewPendingApprovalModel.PageSize = CommonUtils.PageSize;
                objViewPendingApprovalModel.TotalPages = 0;
                objViewPendingApprovalModel.Message = objViewPendingApprovalModel.MessageType = string.Empty;
                //objViewPendingApprovalModel.PendingApprovals = new List<PendingApprovalModel>();

                objPendingApprovalViewModel.GetOnlinePendingApproval(UserId, RoleId, objViewPendingApprovalModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "PendingApproval", "ViewOnlinePendingApproval GET", ex);
            }

            return View(objViewPendingApprovalModel);
        }

        /// <summary>
        /// Search Pending Approval and Save Booking Status of Online Training
        /// </summary>
        /// <param name="objViewPendingApprovalModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        public ActionResult ViewOnlinePendingApproval(ViewPendingApprovalModel objViewPendingApprovalModel)
        {
            PendingApprovalViewModel objPendingApprovalViewModel = new PendingApprovalViewModel();
            int UserId = 0, RoleId = 0;

            int.TryParse(Session["RoleId"].ToString(), out RoleId);
            int.TryParse(Session["UserId"].ToString(), out UserId);
            try
            {
                BindStatusDDL();
                objViewPendingApprovalModel.Message = objViewPendingApprovalModel.MessageType = string.Empty;
                if (!String.IsNullOrEmpty(objViewPendingApprovalModel.ActionType))
                {
                    switch (objViewPendingApprovalModel.ActionType.ToLower())
                    {
                        case "search":
                            break;
                        case "submitstatus":
                            objPendingApprovalViewModel.SaveAllOnlinePendingApproval(objViewPendingApprovalModel, UserId);
                            break;
                    }
                }
                objPendingApprovalViewModel.GetOnlinePendingApproval(UserId, RoleId, objViewPendingApprovalModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(UserId.ToString(), "PendingApproval", "ViewOnlinePendingApproval POST", ex);
            }

            return PartialView("_PendingOnlineApprovalGrid", objViewPendingApprovalModel);
            //return View(objViewPendingApprovalModel);
        }
        #endregion
        private void BindStatusDDL()
		{
			CommonViewModel objCommonViewModel = new CommonViewModel();
			List<ResourceModel> lstResources;
			lstResources= objCommonViewModel.GetResourceByResourceName("BookingStatus").ToList<ResourceModel>();
			ViewBag.BookingStatus = lstResources;//new SelectList(lstResources, "ResourceID", "ResourceName", null);
		}
    }
}

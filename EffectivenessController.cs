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
    public partial class MasterController : Controller
    {
        #region [Properties]

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
        public int roleId
        {
            get
            {
                int _roleID = 0;
                if (Session["RoleId"] != null)
                    int.TryParse(Convert.ToString(Session["RoleId"]), out _roleID);
                return _roleID;
            }
        }
        #endregion

        MasterViewModel objMasterViewModel = new MasterViewModel();

        #region View Effectiveness
        //
        // GET: /EffectiveNess/
        /// <summary>
        /// View Effectiveness list
        /// </summary>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult ViewEffectiveness()
        {
            ViewEffectivenessModel objViewEffectivenessModel = new ViewEffectivenessModel();
            try
            {
                if (Session["EffectivenessSuccess"] != null && Convert.ToString(Session["EffectivenessSuccess"]) != "")
                {
                    objViewEffectivenessModel.Message = Convert.ToString(Session["EffectivenessSuccess"]);
                    objViewEffectivenessModel.MessageType = MessageType.Success.ToString().ToLower();
                    Session["EffectivenessSuccess"] = null;

                }
                objViewEffectivenessModel.CurrentPage = 1;
                objViewEffectivenessModel.PageSize = CommonUtils.PageSize;

                objViewEffectivenessModel.TotalPages = 0;
                objMasterViewModel.GetAllEffectiveness(objViewEffectivenessModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "ViewEffectiveness Get", ex);
            }
            return View(objViewEffectivenessModel);
        }

        /// <summary>
        /// Search Effectiveness by Qusetion and paging and sorting on Effectiveness List
        /// </summary>
        /// <param name="objViewEffectivenessModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult ViewEffectiveness(ViewEffectivenessModel objViewEffectivenessModel)
        {
            try
            {
                objViewEffectivenessModel.Message = objViewEffectivenessModel.MessageType = String.Empty;

                if (objViewEffectivenessModel.ActionType == "delete")
                {
                    objMasterViewModel.DeleteEffectiveness(objViewEffectivenessModel);
                }
                objMasterViewModel.GetAllEffectiveness(objViewEffectivenessModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "ViewEffectiveness Post", ex);
            }
            return PartialView("_EffectivenessList", objViewEffectivenessModel);
        }

        #endregion

        #region ADD/EDIT Effectiveness
        /// <summary>
        /// Add / Edit Effeftiveness
        /// </summary>
        /// <param name="efID"></param>
        /// <returns></returns>
        [Filters.MenuAccess()]
        public ActionResult SaveEffectiveness(int? efID = null)
        {
            EffectivenessQuestionDetail objEffectivenessQuestionDetail = new EffectivenessQuestionDetail();
            int effectivenessQuestionID;
            try
            {
                int.TryParse(Convert.ToString(efID), out effectivenessQuestionID);
                objEffectivenessQuestionDetail.EffectivenessQuestionID = effectivenessQuestionID;
                if (objEffectivenessQuestionDetail.EffectivenessQuestionID > 0)
                {
                    objEffectivenessQuestionDetail = objMasterViewModel.GetEffectivenessDetailID(objEffectivenessQuestionDetail.EffectivenessQuestionID);
                }
                else
                {
                    objEffectivenessQuestionDetail.EffectivenessAnswers = new List<EffectivenessAnswerModel>();
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveEffectiveness Get", ex);
            }
            return View(objEffectivenessQuestionDetail);
        }

        /// <summary>
        /// Save Effectivenss with Answers 
        /// </summary>
        /// <param name="objEffectivenessQuestionDetail"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult SaveEffectiveness(EffectivenessQuestionDetail objEffectivenessQuestionDetail)
        {
            EffectivenessAnswerModel objEffectivenessAnswerModel = new EffectivenessAnswerModel();
            objEffectivenessQuestionDetail.Message = string.Empty;
            objEffectivenessQuestionDetail.MessageType = string.Empty;
            if (objEffectivenessQuestionDetail.EffectivenessAnswers == null)
            {
                objEffectivenessQuestionDetail.EffectivenessAnswers = new List<EffectivenessAnswerModel>();
            }
            try
            {
                if (!String.IsNullOrEmpty(objEffectivenessQuestionDetail.ActionType))
                {
                    switch (objEffectivenessQuestionDetail.ActionType)
                    {
                        case "addnewitem":
                            objEffectivenessQuestionDetail.lastInsertedId = objEffectivenessQuestionDetail.lastInsertedId - 1;
                            objEffectivenessAnswerModel.AnswerDescription = objEffectivenessQuestionDetail.NewAnswerDescription;
                            objEffectivenessAnswerModel.Weightage = objEffectivenessQuestionDetail.NewAnswerWeightage;
                            objEffectivenessAnswerModel.EffectivenessAnswerID = objEffectivenessQuestionDetail.lastInsertedId;
                            objEffectivenessAnswerModel.IsActive = true;
                            objEffectivenessQuestionDetail.EffectivenessAnswers.Add(objEffectivenessAnswerModel);

                            break;
                        case "deleteitem":
                            objEffectivenessAnswerModel = objEffectivenessQuestionDetail.EffectivenessAnswers.Where(o => o.EffectivenessAnswerID == objEffectivenessQuestionDetail.DeletedId).SingleOrDefault();
                            if (objEffectivenessAnswerModel.EffectivenessAnswerID > 0)
                            {
                                objEffectivenessAnswerModel.IsActive = false;
                            }
                            else
                            {
                                objEffectivenessQuestionDetail.EffectivenessAnswers.Remove(objEffectivenessAnswerModel);
                            }
                            break;

                        case "saveeffectiveness":
                            string strSuccessMessage;

                            objEffectivenessQuestionDetail.IsActive = true;
                            objEffectivenessQuestionDetail.CreatedOn = DateTime.Now;
                            objEffectivenessQuestionDetail.CreatedBy = createdBy;
                            if (objEffectivenessQuestionDetail.EffectivenessQuestionID <= 0)
                            {
                                strSuccessMessage = Resources.EffectivenessResource.msgEffectivenessSaveSuccess;
                            }
                            else
                            {
                                strSuccessMessage = Resources.EffectivenessResource.msgEffectivenessUpdatesuccess;
                            }

                            string strEffectivenssAnswersXml = CommonUtils.GetBulkXML(objEffectivenessQuestionDetail.EffectivenessAnswers);

                            objEffectivenessQuestionDetail = objMasterViewModel.InsertUpdateEffectiveness(objEffectivenessQuestionDetail, strEffectivenssAnswersXml);

                            if (objEffectivenessQuestionDetail.ErrorCode.Equals(0))
                            {
                                Session["EffectivenessSuccess"] = strSuccessMessage;
                                objEffectivenessQuestionDetail.MessageType = MessageType.Success.ToString().ToLower();

                                objEffectivenessQuestionDetail.Message = strSuccessMessage;
                                string redirectUrl = Url.Action("ViewEffectiveness", "Master");
                                return Json(new { url = redirectUrl, message = objEffectivenessQuestionDetail.Message, messageType = objEffectivenessQuestionDetail.MessageType });
                            }
                            else
                            {
                                objEffectivenessQuestionDetail.Message = HttpUtility.JavaScriptStringEncode(Resources.EffectivenessResource.msgEffectivenessSaveError);
                                objEffectivenessQuestionDetail.MessageType = MessageType.Error.ToString().ToLower();

                            }
                            break;
                    }

                }

                return PartialView("_EffectivenessAnswerList", objEffectivenessQuestionDetail);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Master", "SaveEffectiveness Post", ex);
                var result = new { message = Resources.TrainingResource.msgSaveErrorMessage, messageType = MessageType.Error.ToString().ToLower() };
                return PartialView("_EffectivenessAnswerList", objEffectivenessQuestionDetail);
            }
        }
        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Common;
using CTMS.Models;
using CTMS.ViewModel;

namespace CTMS.Controllers
{
    public class NewsController : Controller
    {
       CommonUtils objCommonUtilError = new CommonUtils();

        public int CreatedBy
        {
            get
            {
                int _userID = 0;
                if (Session["UserId"] != null)
                    int.TryParse(Convert.ToString(Session["UserId"]), out _userID);
                return _userID;
            }
        }

        /// <summary>
        /// Show All News
        /// </summary>
        [Filters.MenuAccess()]
        public ActionResult ViewNews()
        {
            ViewNewsModel objViewNewsModel = new ViewNewsModel();
            NewsViewModel objNewsViewModel = new NewsViewModel();

            try
            {
                if (Session["Message"] != null && Session["MessageType"] != null && Session["MessageType"].ToString().ToLower() == "success")
                {
                    objViewNewsModel.Message = Convert.ToString(Session["Message"]);
                    objViewNewsModel.MessageType = Convert.ToString(Session["MessageType"]);
                    Session["Message"] = null;
                    Session["MessageType"] = null;

                }
                 objViewNewsModel.CurrentPage = 1;
                objViewNewsModel.PageSize = CommonUtils.PageSize;
                objViewNewsModel.TotalPages = 0;

                objNewsViewModel.GetNews(objViewNewsModel);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(CreatedBy.ToString(), "News", "ViewNews Get", ex);
            }
            return View(objViewNewsModel);
        }

        /// <summary>
        /// Show All News basis of filter and sorting criteria
        /// </summary>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        public ActionResult ViewNews(ViewNewsModel objViewNewsModel)
        {
            try
            {
                objViewNewsModel.Message = objViewNewsModel.MessageType = String.Empty;
                NewsViewModel objNewsViewModel = new NewsViewModel();
                NewsModel objNews = new NewsModel();

                if (objViewNewsModel.ActionType == "search")
                {
                    objViewNewsModel.CurrentPage = 1;
                    objViewNewsModel.PageSize = CommonUtils.PageSize;
                    objViewNewsModel.TotalPages = 0;
                }

                if (objViewNewsModel.ActionType == "changeNewsBroadcastStatus")
                {
                    objViewNewsModel.CurrentPage = 1;
                    objViewNewsModel.PageSize = CommonUtils.PageSize;
                    objViewNewsModel.TotalPages = 0;
                    objViewNewsModel.IsShowBroadcast = !objViewNewsModel.IsShowBroadcast;
                }
              
                if (objViewNewsModel.ActionType == "submitBroadcast")
                {
                 // var selectedTrainings = string.Join(",", objViewNewsModel.News.Where(st => st.IsSelectedNews == true).Select(st => st.NewsID.ToString()));
                    objNews = objNewsViewModel.InsertBroadcast(objViewNewsModel.SelectedList);

                    if (objNews.MessageType == "success")
                    {
                        
                        objViewNewsModel.Message = Resources.NewsResource.msgInsertBroadcastNews;
                        objViewNewsModel.MessageType = MessageType.Success.ToString().ToLower();
                        objViewNewsModel.SelectedList = null;
                        objViewNewsModel.UnSelectedList = null;
                        objViewNewsModel.TotalSelectedCount = 0;
                    }
                    else
                    {
                       objViewNewsModel.Message = Resources.NewsResource.msgErrorBroadcastNews;
                        objViewNewsModel.MessageType = MessageType.Error.ToString().ToLower();
                    }

 
                }

                if (objViewNewsModel.ActionType == "Delete")
                {
                    string redirectUrl = string.Empty;
                    
                    // objNews = objMasterViewModel.getNewsDetailById(NewsID);
                    objNews.NewsID = objViewNewsModel.NewsDetailID;
                    objNews.IsPublished = false;
                    objNews.OperationType = "D";
                    objNews.IsActive = true;
                    objNews.CreatedBy = CreatedBy;
                    objNews = objNewsViewModel.InsertUpdateDeleteNews(objNews);

                    objViewNewsModel.CurrentPage = 1;
                    objViewNewsModel.PageSize = CommonUtils.PageSize;
                    objViewNewsModel.TotalPages = 0;
                    if (objNews.MessageType == "success")
                    {
                        objViewNewsModel.Message = Resources.NewsResource.msgDeleteNewsSuccess;
                        objViewNewsModel.MessageType = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        objViewNewsModel.Message = Resources.NewsResource.msgDeleteNewsError;
                        objViewNewsModel.MessageType = MessageType.Error.ToString().ToLower();
                    }

                }

                objNewsViewModel.GetNews(objViewNewsModel);

                if (objViewNewsModel.ActionType == "pagechange")
                {
                    if (objViewNewsModel.SelectedList != null)
                    {

                        foreach (var news in objViewNewsModel.News)
                        {
                            if (!String.IsNullOrEmpty(objViewNewsModel.SelectedList))
                            {
                                if (objViewNewsModel.SelectedList.Split(',').ToList().Contains(news.NewsID.ToString()))
                                {
                                    news.IsSelectedNews = true;
                                }
                            }
                            if (!String.IsNullOrEmpty(objViewNewsModel.UnSelectedList))
                            {
                                if (objViewNewsModel.UnSelectedList.Split(',').ToList().Contains(news.NewsID.ToString()))
                                {
                                    news.IsSelectedNews = false;
                                }
                            }
                        }
                    }
 
                }

            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(CreatedBy.ToString(), "News", "ViewNews Post", ex);
            }

            return PartialView("_NewsList", objViewNewsModel);

        }



        /// <summary>
        /// Get selected News information basis on News id for edit
        /// </summary>
        [Filters.MenuAccess()]
        public ActionResult SaveNews(int? NewsID)
        {
            NewsModel objNews = new NewsModel();

            try
            {

                if (NewsID.HasValue)
                {
                    NewsViewModel objNewsViewModel = new NewsViewModel();
                    objNews = objNewsViewModel.getNewsDetailById(NewsID.Value);
                }
                if (objNews.IsPublished)
                {
                    objNews.Message = Resources.NewsResource.msgNotEidtBroadcast;
                    objNews.MessageType = MessageType.Notice.ToString().ToLower();
                 }
               // objNews.IsPublished = false;
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(CreatedBy.ToString(), "News", "SaveNews Get", ex);
            }
            return View(objNews);
        }


        /// <summary>
        /// Save News information
        /// </summary>
        /// 
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveNews(NewsModel objNews)
        {
            NewsViewModel objNewsViewModel = new NewsViewModel();

            string redirectUrl = string.Empty;

            try
            {

                if (objNews.OperationType == "B")
                {
                    objNews = objNewsViewModel.InsertBroadcast(objNews.NewsID.ToString());
                    if (objNews.MessageType == "success")
                    {
                        Session["Message"] = Resources.NewsResource.msgInsertBroadcastNews;
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                       
                    }
                    else
                    {
                        Session["Message"] = Resources.NewsResource.msgErrorBroadcastNews;
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }


                }
                else
                {
                    if (objNews.NewsID > 0)
                        objNews.OperationType = "U";
                    else
                        objNews.OperationType = "I";



                    //objNews.IsPublished = false;
                    objNews.IsActive = true;
                    objNews.CreatedBy = CreatedBy;

                    objNews = objNewsViewModel.InsertUpdateDeleteNews(objNews);

                    if (objNews.OperationType == "I")
                    {
                        if (objNews.ErrorCode.Equals(0) || objNews.ErrorCode.Equals(1))
                        {
                            Session["Message"] = Resources.NewsResource.msgSaveSucess;
                            Session["MessageType"] = MessageType.Success.ToString().ToLower();
                        }
                        else
                        {
                            Session["Message"] = Resources.NewsResource.msgSaveError;
                            Session["MessageType"] = MessageType.Error.ToString().ToLower();
                        }
                    }
                    else
                    {
                        if (objNews.ErrorCode.Equals(0) || objNews.ErrorCode.Equals(1))
                        {
                            Session["Message"] = Resources.NewsResource.msgUpdateNewsSuccess;
                            Session["MessageType"] = MessageType.Success.ToString().ToLower();
                        }
                        else
                        {
                            Session["Message"] = Resources.NewsResource.msgUpdateNewsError;
                            Session["MessageType"] = MessageType.Error.ToString().ToLower();
                        }
                    }

                }
                redirectUrl = Url.Action("ViewNews");
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(CreatedBy.ToString(), "News", "SaveTraining Post", ex);
            }

            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });
        }


        /// <summary>
        /// Check Duplication of news title
        /// </summary>
        /// <param name="FieldName" and "FieldValue"></param>
        public ActionResult CheckDuplicateFields(string NewsTitle)
        {
            NewsViewModel objNewsViewModel = new NewsViewModel();
            try
            {
                bool IsValid;
                IsValid = objNewsViewModel.checkDuplicateValues(NewsTitle);
                return Json(IsValid);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(CreatedBy.ToString(), "News", "CheckDuplicateFields", ex);
                return Json(ex);
            }


        }
     

    }
}

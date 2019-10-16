using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using CTMS.DataModels;
using CTMS.ViewModel;
using CTMS.Common;
using System.IO;
//using System.IO.Compression;
using System.Configuration;
using System.Collections.Specialized;
using System.Transactions;
using Ionic.Zip;
using System.Web.UI.WebControls;
using CTMS.Resources;
/*----------------------------------
 Created By :- Vishal Gupta
 Date:- 05 May  2014
 Desc. :- Topic Controller
 ---------------------------------*/
namespace CTMS.Controllers
{
    public class TopicController : Controller
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


        //  string _Message = string.Empty;
        public string TopicMessage
        {
            get
            {
                if (!string.IsNullOrEmpty(Convert.ToString(Session["TopicMessage"])))
                {
                    return Session["TopicMessage"].ToString();

                }

                return string.Empty;

            }
            set
            {
                Session["TopicMessage"] = value;
            }
        }
        // string _MessageType = string.Empty;
        public string TopicMessageType
        {
            get
            {
                if (!string.IsNullOrEmpty(Convert.ToString(Session["TopicMessageType"])))
                {
                    return Session["TopicMessageType"].ToString();

                }

                return string.Empty;

            }
            set
            {


                Session["TopicMessageType"] = value;

            }
        }


        /// <summary>
        /// Return message 
        /// </summary>
        //public object strRetMessage { get; set; }

        /// <summary>
        /// Message 
        /// </summary>
        //public IFormatProvider strMessage { get; set; }

        //common utils class for common operation.
        CommonUtils objCommonUtilError = new CommonUtils();

        //Object of Topic View Model where all CRUD operation perform. 
        TopicViewModel objTopicViewModel = new TopicViewModel();

        // Name of default file.
        string DefaultFileinZip = ConfigurationManager.AppSettings["DefaultFileinZip"].ToString();

        // Path of  folder to store zip extract file. 
        string TrainingMaterialDirectory = ConfigurationManager.AppSettings["TrainingMaterialDirectory"].ToString();

        // Path of Temp folder to store zip file. 
        string TempMaterialDirectory = ConfigurationManager.AppSettings["TempMaterialDirectory"].ToString();

        #endregion

        #region [Get Topic Method]
        /// <summary>
        /// Add topic detail get method 
        /// </summary>
        /// <param name="TrainingId">Int</param>
        /// <param name="Assessment">Boolean</param>
        /// <returns>Object of TrainingTopic</returns>
        [Filters.MenuAccess()]
        public ActionResult AddTopic(int TrainingId, Boolean? Assessment = null)
        {
            if (Assessment == null)
            {
                Assessment = false;
            }
            TrainingTopic objTrainingTopic = new TrainingTopic();

            try
            {
                if (Session["Message"] != null && Session["MessageType"] != null)
                {
                    Session["Message"] = null;
                    Session["MessageType"] = null;
                }
                objTrainingTopic.CurrentPage = 1;
                objTrainingTopic.PageSize = CommonUtils.PageSize;

                objTrainingTopic.TotalPages = 0;
                objTrainingTopic = objTopicViewModel.GetTopicbyTrainingID(TrainingId, objTrainingTopic);
                if (objTrainingTopic.PublishCount > 0)
                {
                    TopicMessage = Resources.TrainingResource.msgTrainingPublishNotification;
                    TopicMessageType = MessageType.Notice.ToString().ToLower();
                }
                objTrainingTopic.Assessment = (bool)Assessment;
                GetSequence(objTrainingTopic);
            }
            catch (Exception ex)
            {
                /// TempData["Message"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "AddTopic", ex);
                objTrainingTopic.Message = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "AddTopic Get", ex);
                objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();

            }
            objTrainingTopic.Message = TopicMessage;
            objTrainingTopic.MessageType = TopicMessageType;
            TopicMessage = null;
            TopicMessageType = null;


            //Get topic file extension
            ViewBag.PPTExtension = CommonUtils.PPTExtension;
            ViewBag.pdfExtension = CommonUtils.PDFExtension;
            ViewBag.ZipFlashExtension = CommonUtils.ZipFlashExtension;
            ViewBag.VideoExtension = CommonUtils.VideoExtension;

            //Set Max size of topic files.
            ViewBag.PPTMaxFileSize = CommonUtils.PPTMaxFileSize;
            ViewBag.pdfMaxFileSize = CommonUtils.PDFMaxFileSize;
            ViewBag.ZipFlashMaxFileSize = CommonUtils.ZipMaxFileSize;
            ViewBag.VideoMaxFileSize = CommonUtils.VideoMaxFileSize;

            if (Session["MaterialFilePath"] != null && Convert.ToString(Session["MaterialFilePath"]) != "")
            {
                objTrainingTopic.ViewMaterialPath = Session["MaterialFilePath"].ToString();
                objTrainingTopic.ActionType = "viewMaterial";
                Session["MaterialFilePath"] = null;
            }
            return View("Topic", objTrainingTopic);
        }


        #endregion

        #region [Post Topic Method]

        /// <summary>
        /// Save Topic detail 
        /// </summary>
        /// <param name="objTrainingTopic">Object of TrainingTopic Model</param>
        /// <param name="UploadMaterial">Object of Http Posted file </param>
        /// <param name="form">Object of form collection</param>
        /// <returns>object of TrainingTopic model</returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]

        public ActionResult AddTopic(TrainingTopic objTrainingTopic, HttpPostedFileBase UploadMaterial)//, FormCollection form)
        {
            GetSequence(objTrainingTopic);
            // string strRetMessage = string.Empty;
            TopicViewModel objTopicViewModel = new TopicViewModel();
            try
            {
                if (objTrainingTopic != null && objTrainingTopic.ActionType != null && objTrainingTopic.ActionType.ToLower() == "sortlist" || objTrainingTopic.ActionType.ToLower() == "pagechange")
                {
                    ModelState.Remove("TopicName");
                    ModelState.Remove("Sequence");
                    ModelState.Remove("TopicDescription");
                    objTrainingTopic = objTopicViewModel.GetTopicbyTrainingID(objTrainingTopic.TrainingID, objTrainingTopic);
                    return View("Topic", objTrainingTopic);
                }
                if (objTrainingTopic.SelectedMaterialType != null && objTrainingTopic.ActionType != null && objTrainingTopic.ActionType.ToLower() == "save")
                {
                    objTrainingTopic = objTopicViewModel.CheckDupTopicSequence(objTrainingTopic);
                    if (objTrainingTopic.ErrorCode > 0)
                    {
                        // TempData["Message"] = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>"; ;
                        objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                        objTrainingTopic.Message = objTrainingTopic.ErrorMessage;

                        return View("Topic", objTrainingTopic);
                    }


                    objTrainingTopic.MaterialTypeID = (from m in objTrainingTopic.lstMaterialTypeModel
                                                       where m.MaterialTypeName.ToLower() == objTrainingTopic.SelectedMaterialType.ToLower()
                                                       select m.MaterialTypeID).FirstOrDefault();
                    objTrainingTopic.MaterialTypeName = objTrainingTopic.SelectedMaterialType;

                    if (UploadMaterial != null && UploadMaterial.ContentLength > 0)
                    {


                        //Check file is Flash 
                        #region [Upload Flash File]
                        if (objTrainingTopic.SelectedMaterialType.ToLower() == "flash")
                        {
                            //check zip or flash file size.
                            if (UploadMaterial.ContentLength > CommonUtils.ZipMaxFileSize)
                            {
                                objTrainingTopic.Message = string.Format(TopicResource.msgMaxFileSize, CommonUtils.ZipMaxFileSize / 1048576);
                                objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();

                                return View("Topic", objTrainingTopic);

                            }

                            if (Path.GetExtension(UploadMaterial.FileName).ToLower() == ".zip")
                            {
                               /// Session["FileStream"] = UploadMaterial.InputStream;
                               
                                    using (ZipFile archive = ZipFile.Read(UploadMaterial.InputStream))
                                    {
                                        if (ModelState.IsValid)
                                        {
                                           
                                            objTrainingTopic.FilePath = Path.Combine(Server.MapPath(TempMaterialDirectory), objTrainingTopic.TrainingID.ToString(), objTrainingTopic.Sequence.ToString());
                                            if (!CommonUtils.CheckFileExist(objTrainingTopic.FilePath))
                                                CommonUtils.CreateDirectory(objTrainingTopic.FilePath);
                                            objTrainingTopic.FilePath = Path.Combine(objTrainingTopic.FilePath, Path.GetFileName(UploadMaterial.FileName));
                                            UploadMaterial.SaveAs(objTrainingTopic.FilePath);
                                            //Save Material for Class Room
                                            if (!objTrainingTopic.IsMobileType)
                                            {
                                                if (archive.Entries.Any(x => x.FileName.Contains(DefaultFileinZip)))
                                                {
                                                    ViewBag.selectFile = "";
                                                    //if default file is found in zip file.

                                                    objTrainingTopic.SelectedFile = objTrainingTopic.SelectedFile == null ? DefaultFileinZip : objTrainingTopic.SelectedFile;
                                                    UploadFile(objTrainingTopic, "zip", UploadMaterial);
                                                    // TempData["Message"] = strRetMessage;
                                                    TopicMessageType = objTrainingTopic.MessageType;
                                                    TopicMessage = objTrainingTopic.Message;
                                                    string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                                                    return Redirect(url);

                                                }
                                                else
                                                {
                                                    ViewBag.selectFile = "Yes";

                                                    objTrainingTopic.lstFileDetail = new List<FileDetail>();
                                                    objTrainingTopic.lstFileDetail = (from z in archive.Entries.Where(x => x.FileName.EndsWith(CommonUtils.DefaultExtensionInZip))
                                                                                      select new FileDetail
                                                                                      {
                                                                                          FileName = Path.GetFileName(z.FileName),
                                                                                          FileSize = z.CompressedSize,
                                                                                          FilePath = objTrainingTopic.FilePath
                                                                                      }).ToList();

                                                }
                                            }
                                            else
                                            {
                                                //save material for Online Training
                                                ViewBag.selectFile = "Yes";

                                                objTrainingTopic.lstFileDetail = new List<FileDetail>();
                                                objTrainingTopic.lstFileDetail = (from z in archive.Entries.Where(x => x.FileName.EndsWith(CommonUtils.DefaultExtensionInVideo))
                                                                                  select new FileDetail
                                                                                  {
                                                                                      FileName = Path.GetFileName(z.FileName),
                                                                                      FileSize = z.CompressedSize,
                                                                                      FilePath = objTrainingTopic.FilePath
                                                                                  }).ToList();
                                            }
                                        }
                                    }
                                
                                
                            }
                            else if (Path.GetExtension(UploadMaterial.FileName).ToLower() == ".flv")
                            {
                                if (ModelState.IsValid)
                                {
                                    UploadFile(objTrainingTopic, "ppt", UploadMaterial);
                                    //TempData["Message"] = strRetMessage;
                                    TopicMessageType = objTrainingTopic.MessageType;
                                    TopicMessage = objTrainingTopic.Message;
                                    string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                                    return Redirect(url);
                                }
                            }
                            
                        }
                        #endregion
                        //check file is ppt.
                        #region [Upload PPT File]
                        else if (objTrainingTopic.SelectedMaterialType.ToLower() == "ppt")
                        {
                            if (ModelState.IsValid)
                            {
                                //check PPT file size.
                                if (UploadMaterial.ContentLength > CommonUtils.PPTMaxFileSize)
                                {
                                    objTrainingTopic.Message = string.Format(TopicResource.msgMaxFileSize, CommonUtils.ZipMaxFileSize / 1048576);
                                    objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();

                                    return View("Topic", objTrainingTopic);

                                }
                                UploadFile(objTrainingTopic, "ppt", UploadMaterial);
                                //  TempData["Message"] = strRetMessage;
                                TopicMessageType = objTrainingTopic.MessageType;
                                TopicMessage = objTrainingTopic.Message;
                                string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                                return Redirect(url);
                            }
                        }
                        #endregion

                        //check file is pdf.
                        #region [Upload PDF File]
                        else if (objTrainingTopic.SelectedMaterialType.ToLower() == "pdf")
                        {
                            if (ModelState.IsValid)
                            {
                                //check PDF file size.
                                if (UploadMaterial.ContentLength > CommonUtils.PDFMaxFileSize )
                                {
                                    objTrainingTopic.Message = string.Format(TopicResource.msgMaxFileSize, CommonUtils.ZipMaxFileSize / 1048576);
                                    objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                                    return View("Topic", objTrainingTopic);

                                }
                                UploadFile(objTrainingTopic, "pdf", UploadMaterial);
                                //  TempData["Message"] = strRetMessage;
                                TopicMessageType = objTrainingTopic.MessageType;
                                TopicMessage = objTrainingTopic.Message;
                                string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                                return Redirect(url);
                            }
                        }
                        #endregion

                        #region [Upload Mp4 File]
                        else if (objTrainingTopic.SelectedMaterialType.ToLower() == "video")
                        {
                            if (ModelState.IsValid)
                            {
                                string ftype = string.Empty;
                                //check PDF file size.
                                if (UploadMaterial.ContentLength > CommonUtils.VideoMaxFileSize)
                                {
                                    objTrainingTopic.Message = string.Format(TopicResource.msgMaxFileSize, CommonUtils.VideoMaxFileSize / 1048576);
                                    objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                                    return View("Topic", objTrainingTopic);

                                }
                                if (Path.GetExtension(UploadMaterial.FileName).ToLower() == ".mp4")
                                {
                                    ftype = "mp4";
                                }
                                else
                                {
                                    ftype = "3gp";
                                }
                                UploadFile(objTrainingTopic, ftype, UploadMaterial);
                                //  TempData["Message"] = strRetMessage;
                                TopicMessageType = objTrainingTopic.MessageType;
                                TopicMessage = objTrainingTopic.Message;
                                string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                                return Redirect(url);
                            }
                        }
                        #endregion

                    }
                    //check if video url
                    #region [Add Video URL]
                    else if (objTrainingTopic.SelectedMaterialType.ToLower() == "video url")
                    {
                        if (ModelState.IsValid)
                        {

                            objTrainingTopic.OperationType = "A";
                            objTrainingTopic.CreatedBy = createdBy;
                            objTrainingTopic.IsActive = true;
                            objTrainingTopic.CreatedOn = DateTime.Now;
                            objTrainingTopic.MaterialURL = objTrainingTopic.txtMaterial;
                            objTrainingTopic = Save(objTrainingTopic);
                            //string strMessage = string.Empty;
                            // strMessage = UserMessage.SUCCESS_MESSAGE_CREATE;

                            if (objTrainingTopic.ErrorCode.Equals(0))
                            {
                                // TempData["Message"] = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(strMessage, "Topic") + "</div>";
                                objTrainingTopic.Message = string.Format(UserMessage.SUCCESS_MESSAGE_CREATE, "Topic");
                                objTrainingTopic.MessageType = MessageType.Success.ToString().ToLower();
                                TopicMessageType = objTrainingTopic.MessageType;
                                TopicMessage = objTrainingTopic.Message;
                                string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                                return Redirect(url);
                            }
                            else
                            {
                                //TempData["Message"] = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>";
                                objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                                objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                                TopicMessageType = objTrainingTopic.MessageType;
                                TopicMessage = objTrainingTopic.Message;
                            }
                        }
                    }
                    #endregion
                }
                //Check Action type is File Selection
                #region [File Selection = "File selection"]
                if (objTrainingTopic != null && objTrainingTopic.ActionType != null && objTrainingTopic.ActionType.ToLower() == "fileselection")
                {
                    if (ModelState.IsValid)
                    {

                        objTrainingTopic.MaterialTypeName = (from m in objTrainingTopic.lstMaterialTypeModel
                                                             where m.MaterialTypeID == objTrainingTopic.MaterialTypeID
                                                             select m.MaterialTypeName).FirstOrDefault();
                        objTrainingTopic.MaterialTypeID = objTrainingTopic.MaterialTypeID;
                        UploadFile(objTrainingTopic, "zip", UploadMaterial);

                        // string strMessage = string.Empty;
                        //  strMessage = UserMessage.SUCCESS_MESSAGE_CREATE;

                        if (objTrainingTopic.ErrorCode.Equals(0))
                        {
                            // TempData["Message"] = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(strMessage, "Topic") + "</div>";
                            objTrainingTopic.Message = string.Format(UserMessage.SUCCESS_MESSAGE_CREATE, "Topic");
                            objTrainingTopic.MessageType = MessageType.Success.ToString().ToLower();
                            TopicMessageType = objTrainingTopic.MessageType;
                            TopicMessage = objTrainingTopic.Message;
                            string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                            return Redirect(url);
                        }
                        else
                        {
                            // TempData["Message"] = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>";
                            objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                            objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                            TopicMessageType = objTrainingTopic.MessageType;
                            TopicMessage = objTrainingTopic.Message;
                        }
                    }
                }
                #endregion
                //No any file upload direct Topic save
                #region [No any file upload]
                if (string.IsNullOrEmpty(objTrainingTopic.SelectedMaterialType) && objTrainingTopic != null && objTrainingTopic.ActionType.ToLower() == "save")
                {
                    if (ModelState.IsValid)
                    {

                        objTrainingTopic = objTopicViewModel.CheckDupTopicSequence(objTrainingTopic);
                        if (objTrainingTopic.ErrorCode > 0)
                        {
                            //  TempData["Message"] = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>"; ;
                            objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                            objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                            return View("Topic", objTrainingTopic);
                        }
                        objTrainingTopic.OperationType = "A";
                        objTrainingTopic.CreatedBy = createdBy;
                        objTrainingTopic.IsActive = true;
                        objTrainingTopic.CreatedOn = DateTime.Now;
                        objTrainingTopic = Save(objTrainingTopic);
                        //   string strMessage = string.Empty;
                        //  strMessage = UserMessage.SUCCESS_MESSAGE_CREATE;

                        if (objTrainingTopic.ErrorCode.Equals(0))
                        {
                            //TempData["Message"] = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(strMessage, "Topic") + "</div>";
                            objTrainingTopic.Message = string.Format(UserMessage.SUCCESS_MESSAGE_CREATE, "Topic");
                            objTrainingTopic.MessageType = MessageType.Success.ToString().ToLower();
                            TopicMessageType = objTrainingTopic.MessageType;
                            TopicMessage = objTrainingTopic.Message;
                            string url = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                            return Redirect(url);
                        }
                        else
                        {
                            // TempData["Message"] = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>";
                            objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                            objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();

                        }
                    }
                }
                #endregion

            }
            catch (ZipException ex)
            {
                objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "ViewTopics Post", ex);
                objTrainingTopic.Message = TopicResource.msgZipException;
                objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
            }
            catch (Exception ex)
            {
                //TempData["Message"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "ViewTopics", ex);
                objTrainingTopic.Message = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "ViewTopics Post", ex);
                objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
            }

            ViewBag.PPTExtension = CommonUtils.PPTExtension;
            ViewBag.pdfExtension = CommonUtils.PDFExtension;
            ViewBag.ZipFlashExtension = CommonUtils.ZipFlashExtension;
            ViewBag.VideoExtension = CommonUtils.VideoExtension;

            ViewBag.PPTMaxFileSize = CommonUtils.PPTMaxFileSize;
            ViewBag.pdfMaxFileSize = CommonUtils.PDFMaxFileSize;
            ViewBag.ZipFlashMaxFileSize = CommonUtils.ZipMaxFileSize;
            ViewBag.VideoMaxFileSize = CommonUtils.VideoMaxFileSize;
            return View("Topic", objTrainingTopic);
        }

        #endregion

        #region [Upload & Save ]
        /// <summary>
        /// Upload file and save into database 
        /// </summary>
        /// <param name="objTrainingTopic">Object of TrainingTopic</param>
        /// <param name="fileType">Define which type of the upload ex. ppt, zip etc.</param>
        /// <param name="UploadMaterial">Object of HttpPostedFileBase</param>
        /// <param name="strRetMessage">Success/Error message</param>
        /// <returns>Object of TrainingTopic</returns>
        public TrainingTopic UploadFile(TrainingTopic objTrainingTopic, string fileType, HttpPostedFileBase UploadMaterial)
        {
            //   strRetMessage = string.Empty;
            string directoryPath = "";

            using (TransactionScope scope = new TransactionScope())
            {
                try
                {

                    //    string strMessage = string.Empty;
                    // strMessage = UserMessage.SUCCESS_MESSAGE_CREATE;

                    //Save topic detail 
                    objTrainingTopic.OperationType = "A";
                    objTrainingTopic.CreatedBy = createdBy;
                    objTrainingTopic.IsActive = true;
                    objTrainingTopic.CreatedOn = DateTime.Now;
                    objTrainingTopic = Save(objTrainingTopic);

                    if (objTrainingTopic.ErrorCode.Equals(0))
                    {
                        directoryPath = (TrainingMaterialDirectory + objTrainingTopic.TrainingID + "/" + objTrainingTopic.TopicID + "/");

                        if (fileType == "zip")
                        {

                            //code using ionic.zip
                            using (ZipFile archive = ZipFile.Read(objTrainingTopic.FilePath))
                            {
                                var fpath = (from z in archive.Entries.Where(x => x.FileName.EndsWith(objTrainingTopic.SelectedFile))
                                             select z.FileName).FirstOrDefault();

                                objTrainingTopic.MaterialURL = directoryPath + fpath;
                                directoryPath = Server.MapPath(directoryPath);
                                archive.ExtractAll(directoryPath, ExtractExistingFileAction.DoNotOverwrite);
                            }

                        }
                        else
                        {
                            if (objTrainingTopic.ErrorCode.Equals(0))
                            {
                                var filepath = UploadMaterial.FileName;
                                objTrainingTopic.MaterialURL = directoryPath + Path.GetFileName(UploadMaterial.FileName);
                                directoryPath = Server.MapPath(directoryPath);
                                //Upload zip file in folder 
                                if (!CommonUtils.CheckFileExist(directoryPath))
                                    CommonUtils.CreateDirectory(directoryPath);
                                UploadMaterial.SaveAs(Path.Combine(directoryPath, Path.GetFileName(UploadMaterial.FileName)));
                            }
                        }

                        //Update topic detail 
                        objTrainingTopic.OperationType = "E";
                        objTrainingTopic.CreatedBy = createdBy;
                        objTrainingTopic.CreatedOn = DateTime.Now;
                        objTrainingTopic.IsActive = true;
                        objTrainingTopic = Save(objTrainingTopic);
                        if (objTrainingTopic.ErrorCode.Equals(0))
                        {
                            //    strRetMessage = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(strMessage, "Topic") + "</div>";
                            objTrainingTopic.MessageType = MessageType.Success.ToString().ToLower();
                            objTrainingTopic.Message = string.Format(UserMessage.SUCCESS_MESSAGE_CREATE, "Topic");
                        }
                        else
                        {
                            //strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>";
                            objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                            objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                        }
                    }
                    else
                    {
                        //  strRetMessage = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>";
                        objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                        objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                    }
                    scope.Complete();

                }
                catch (Exception ex)
                {
                    if (CommonUtils.CheckFileExist(directoryPath))
                        CommonUtils.DeleteDirectory(directoryPath);
                    throw (ex);
                }
                finally
                {
                    try
                    {
                        if (CommonUtils.CheckFileExist(Path.GetDirectoryName(objTrainingTopic.FilePath)))
                            CommonUtils.DeleteDirectory(Path.GetDirectoryName(objTrainingTopic.FilePath));
                    }
                    catch { }
                }

            }
            return objTrainingTopic;
        }
        #endregion

        #region [Save]
        /// <summary>
        /// Save, Update and Delete Topic detail.
        /// </summary>
        /// <param name="objTrainingTopic">Object of TrainingTopic Model</param>
        /// <returns>Object of TrainingTopic Model.</returns>
        public TrainingTopic Save(TrainingTopic objTrainingTopic)
        {
            objTrainingTopic.TopicOrderNo = objTrainingTopic.Sequence;
            objTrainingTopic.MaterialURL = objTrainingTopic.MaterialURL == null ? objTrainingTopic.SelectedFile : objTrainingTopic.MaterialURL;
            objTrainingTopic = objTopicViewModel.InsertDelTopicDetail(objTrainingTopic);
            return objTrainingTopic;
        }
        #endregion

        #region [Delete]
        /// <summary>
        /// Delete topic detail 
        /// </summary>
        /// <param name="TrainingId">Id of training </param>
        /// <param name="TopicId">Id of topic</param>
        /// <returns></returns>
        public ActionResult DeleteTopic(int TrainingId, int TopicId, Boolean Assessment)
        {
            TrainingTopic objTrainingTopic = new TrainingTopic();
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    objTrainingTopic.CreatedBy = createdBy;
                    objTrainingTopic.IsActive = false;
                    objTrainingTopic.CreatedOn = DateTime.Now;
                    objTrainingTopic.TopicID = TopicId;
                    objTrainingTopic.TrainingID = TrainingId;
                    objTrainingTopic.Assessment = Assessment;
                    objTrainingTopic.OperationType = "D";
                    objTrainingTopic = Save(objTrainingTopic);
                    if (objTrainingTopic.ErrorCode.Equals(0))
                    {
                        string directoryPath = Server.MapPath(TrainingMaterialDirectory + objTrainingTopic.TrainingID + "/" + objTrainingTopic.TopicID + "/");
                        if (CommonUtils.CheckFileExist(directoryPath))
                            CommonUtils.DeleteDirectory(directoryPath);
                        scope.Complete();
                        // TempData["Message"] = "<div class='" + MessageType.Success.ToString().ToLower() + "'>" + string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Topic") + "</div>";
                        objTrainingTopic.MessageType = MessageType.Success.ToString().ToLower();
                        objTrainingTopic.Message = string.Format(UserMessage.SUCCESS_MESSAGE_DELETE, "Topic");
                        TopicMessageType = objTrainingTopic.MessageType;
                        TopicMessage = objTrainingTopic.Message;
                        string redirectUrl = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + objTrainingTopic.TrainingID.ToString() + "&" + "Assessment=" + objTrainingTopic.Assessment.ToString();
                        return Json(new { url = redirectUrl });
                    }
                    else
                    {
                        //TempData["Message"] = "<div class='" + MessageType.Error.ToString().ToLower() + "'>" + HttpUtility.JavaScriptStringEncode(objTrainingTopic.ErrorMessage) + "</div>";
                        objTrainingTopic.Message = objTrainingTopic.ErrorMessage;
                        objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                    }
                }
                catch (Exception ex)
                {

                    //TempData["Message"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "ViewTopics", ex);
                    objTrainingTopic.MessageType = MessageType.Error.ToString().ToLower();
                    objTrainingTopic.Message = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "DeleteTopic", ex);
                }

            }

            return View("Topic", objTrainingTopic);
        }
        #endregion

        #region [ViewVideoMaterail]
        public ActionResult ViewVideoMaterail(int TrainingId, Boolean Assessment, string filePath)
        {
            if(!String.IsNullOrEmpty(filePath))
            {
                Session["MaterialFilePath"] = filePath;
            }
            string redirectUrl = Request.UrlReferrer.AbsolutePath + "?TrainingId=" + TrainingId + "&" + "Assessment=" + Assessment;
            return Json(new { url = redirectUrl });
        }
        #endregion
        /// <summary>
        /// Get Sequence list 
        /// </summary>
        /// <param name="objTrainingTopic"></param>
        private void GetSequence(TrainingTopic objTrainingTopic)
        {
            List<int> lstinterger = new List<int>();
            lstinterger.AddRange(Enumerable.Range(1, 20));
            if (objTrainingTopic != null && objTrainingTopic.lstTopicModel != null && objTrainingTopic.lstTopicModel.Count() > 0)
            {
                int count = objTrainingTopic.lstTopicModel.Count();
                if (lstinterger.Count() <= count)
                {
                    lstinterger.AddRange(Enumerable.Range(5, count + 5));
                }
            }
            List<Sequence> lstSequence = new List<Sequence>();
            lstSequence = new List<Sequence> {
                new Sequence { Text = "--Select--", Value = null }};
            for (int i = 0; i < lstinterger.Count(); i++)
            {
                lstSequence.Add(new Sequence { Text = lstinterger[i].ToString(), Value = lstinterger[i] });
                ;
            }


            ViewBag.Sequence = new SelectList(lstSequence, "Value", "Text", null);

        }
        /// <summary>
        /// Check duplicate Topic name and sequence
        /// </summary>
        /// <param name="TrainingID"></param>
        /// <param name="Sequence"></param>
        /// <param name="TopicName"></param>
        /// <returns></returns>
        [HttpPost]
        [Filters.Authorized()]
        [ValidateInput(false)]

        public ActionResult CheckDuplicate(int TrainingID, int Sequence, string TopicName)
        {

            TrainingTopic objtopic = new TrainingTopic(); try
            {
                objtopic.TrainingID = TrainingID;
                objtopic.Sequence = Sequence;
                objtopic.TopicName = TopicName;
                objtopic = objTopicViewModel.CheckDupTopicSequence(objtopic);
            }
            catch (Exception ex)
            {
                //  TempData["Message"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "CheckDuplicate", ex);
                objtopic.MessageType = MessageType.Error.ToString().ToLower();
                objtopic.Message = objCommonUtilError.ErrorLog(createdBy.ToString(), "Topic", "CheckDuplicate", ex);
            }
            return Json(new { ErrorCode = objtopic.ErrorCode, ErrorMessage = objtopic.ErrorMessage }, JsonRequestBehavior.AllowGet);
        }


    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CTMS.Models;
using CTMS.ViewModel;
using CTMS.Common;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Data;
using System.Data.Entity;
using System.Configuration;
using System.IO;
using System.Web.UI;
using CTMS.DataModels;
using System.Data.OleDb;

namespace CTMS.Controllers
{
    public class UploadAssesementController : Controller
    {
        //
        // GET: /UploadAssesement/

        CommonUtils objCommonUtilError = new CommonUtils();

        //[Filters.MenuAccess()]
        public ActionResult UploadAssesement()
        {

            UploadAssessementModel objUploadAssessementModel = new UploadAssessementModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            objUploadAssessementModel.CurrentPage = 1;
            objUploadAssessementModel.PageSize = CommonUtils.PageSize;
            objUploadAssessementModel.TotalPages = 0;

            return View(objUploadAssessementModel);
        }

        [HttpPost]
        public ActionResult UploadAssesement(UploadAssessementModel objUploadAssessementModel)
        {


            int createdBy = Convert.ToInt32(Session["UserId"]);
            DataTable dtValidationReturn = new DataTable();
            CommonUtils objCommonUtils = new CommonUtils();
            

            try
            {

                HttpFileCollectionBase files = Request.Files;
                HttpPostedFileBase Uploadfile = null;

                ///  Redirect from edit page

                //if (files.Count > 0)
                //{
                //    Uploadfile = files[0];

                //    if (Uploadfile.ContentLength == 0)
                //    {
                //        objUploadAssessementModel = (UploadAssessementModel)Session["objUploadAssessementModel"];
                //        return View("UploadAssesement", objUploadAssessementModel);
                //    }
                //}


                if (files.Count > 0)
                {
                    Uploadfile = files[0];

                    if (Uploadfile.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(Uploadfile.FileName);
                        string extension = Path.GetExtension(Uploadfile.FileName);

                        if (!string.IsNullOrEmpty(fileName))
                        {

                            if (extension.Equals(".xlsx"))
                            {

                                string filePath = Server.MapPath(@"\UploadExcel\" + createdBy + "_" + DateTime.Now.ToString().Replace(':', '_') + "_" + Uploadfile.FileName.Split('\\').Last());

                                Uploadfile.SaveAs(filePath);

                                DataTable dtExcelDump = new DataTable();
                                DataTable dtExcel = new DataTable();
                                dtExcel.TableName = "MyExcelData";
                                string SourceConstr = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + filePath + "';Extended Properties= 'Excel 8.0;HDR=Yes;IMEX=1'";
                                OleDbConnection con = new OleDbConnection(SourceConstr);
                                string query = "Select * from [DATA-UPLOAD$]";
                                OleDbDataAdapter data = new OleDbDataAdapter(query, con);
                                data.Fill(dtExcelDump);


                                // dtExcel = RemoveNullColumnFromDataTable(dtExcelDump);


                                dtExcel = dtExcelDump.Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is System.DBNull || string.Compare((field as string).Trim(), string.Empty) == 0)).CopyToDataTable();


                                if (fnCheckForDataCollumsIsAvialableInDataTable("Business Function", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Business Function' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Topic / Subject", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Topic / Subject' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Question", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Question' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer 1", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer 1' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Is Answer 1 ?", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Is Answer 1 ?' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer 2", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer 2' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Is Answer 2 ?", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Is Answer 2 ?' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer 3", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer 3' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Is Answer 3 ?", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Is Answer 3 ?' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer 4", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer 4' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Is Answer 4 ?", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Is Answer 4 ?' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer 5", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer 5' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Is Answer 5 ?", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Is Answer 5 ?' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer 6", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer 6' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Is Answer 6 ?", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Is Answer 6 ?' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Answer Type", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Answer Type' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                                else if (fnCheckForDataCollumsIsAvialableInDataTable("Question Type", dtExcel) == false)
                                {
                                    Session["Message"] = "No 'Question Type' Column name found in the excel sheet !!";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }



                                if (dtExcel.Columns.Count == 17)
                                {
                                    //changing the data table names corrsonding to the database fileds.
                                    dtExcel.Columns[0].ColumnName = "Business Function";
                                    dtExcel.Columns[1].ColumnName = "Topic / Subject";
                                    dtExcel.Columns[2].ColumnName = "Question";
                                    dtExcel.Columns[3].ColumnName = "Answer 1";
                                    dtExcel.Columns[4].ColumnName = "Is Answer 1 ?";
                                    dtExcel.Columns[5].ColumnName = "Answer 2";
                                    dtExcel.Columns[6].ColumnName = "Is Answer 2 ?";
                                    dtExcel.Columns[7].ColumnName = "Answer 3";
                                    dtExcel.Columns[8].ColumnName = "Is Answer 3 ?";
                                    dtExcel.Columns[9].ColumnName = "Answer 4";
                                    dtExcel.Columns[10].ColumnName = "Is Answer 4 ?";
                                    dtExcel.Columns[11].ColumnName = "Answer 5";
                                    dtExcel.Columns[12].ColumnName = "Is Answer 5 ?";
                                    dtExcel.Columns[13].ColumnName = "Answer 6";
                                    dtExcel.Columns[14].ColumnName = "Is Answer 6 ?";
                                    dtExcel.Columns[15].ColumnName = "Answer Type";
                                    dtExcel.Columns[16].ColumnName = "Question Type";


                                    DataTable dtClonedExcel = dtExcel.Clone();
                                    dtClonedExcel.Columns[0].DataType = typeof(string);
                                    dtClonedExcel.Columns[1].DataType = typeof(string);
                                    dtClonedExcel.Columns[2].DataType = typeof(string);
                                    dtClonedExcel.Columns[3].DataType = typeof(string);
                                    dtClonedExcel.Columns[4].DataType = typeof(string);
                                    dtClonedExcel.Columns[5].DataType = typeof(string);
                                    dtClonedExcel.Columns[6].DataType = typeof(string);
                                    dtClonedExcel.Columns[7].DataType = typeof(string);
                                    dtClonedExcel.Columns[8].DataType = typeof(string);
                                    dtClonedExcel.Columns[9].DataType = typeof(string);
                                    dtClonedExcel.Columns[10].DataType = typeof(string);
                                    dtClonedExcel.Columns[11].DataType = typeof(string);
                                    dtClonedExcel.Columns[12].DataType = typeof(string);
                                    dtClonedExcel.Columns[13].DataType = typeof(string);
                                    dtClonedExcel.Columns[14].DataType = typeof(string);
                                    dtClonedExcel.Columns[15].DataType = typeof(string);
                                    dtClonedExcel.Columns[16].DataType = typeof(string);


                                    foreach (DataRow row in dtExcel.Rows)
                                    {

                                        foreach (DataColumn col in dtExcel.Columns)
                                        {
                                            row[col] = row[col].ToString().Trim();
                                        }

                                        dtClonedExcel.ImportRow(row);

                                    }


                                    objUploadAssessementModel = SaveToDataBase(dtClonedExcel);

                                  
                                   // dtValidationReturn = objCommonUtils.ConvertToDataTable(objUploadAssessementModel.TempExcelAssesementDataModels
                                   //, new string[] { "ExcelId", "RowID", "Category", "AssessmentTopic", "QuestionDescription", "Answer1", "IsAnswer1", "Answer2", "IsAnswer2", "Answer3", "IsAnswer3", "Answer4", "IsAnswer4", "Answer5", "IsAnswer5", "Answer6", "IsAnswer6", "IsSingleAnswer", "QuestionTypeName", "ErrMsg" });

                                    

                                    dtValidationReturn = (DataTable)Session["dtExcel"];

                                    Session["objUploadAssessementModel"] = objUploadAssessementModel;

                                    if (dtValidationReturn.Rows.Count > 0)
                                    {
                                       
                                        Session["Message"] = "Invalid data in the sheet, please click on View UnUploaded Data to proceed...";
                                        Session["MessageType"] = MessageType.Error.ToString().ToLower();

                                        objUploadAssessementModel.Message = Convert.ToString (Session["Message"]);
                                        objUploadAssessementModel.MessageType = Convert.ToString(Session["MessageType"]);
                                        Session["Message"] = null;
                                        Session["MessageType"] = null;

                                        return View("UploadAssesement", objUploadAssessementModel);

                                    }
                                    else
                                    {
                                        
                                        Session["Message"] = "Saved successfully";
                                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                                        Session["objUploadAssessementModel"] = null;
                                        Session["dtExcel"] = null;

                                        return RedirectToAction("UploadAssesement");

                                    }

                                }
                                else
                                {
                                    Session["Message"] = "Incorrect template for Assesement upload";
                                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                    return RedirectToAction("UploadAssesement");
                                }
                            }
                            else
                            {
                                Session["Message"] = "Please upload a valid excel document";
                                Session["MessageType"] = MessageType.Error.ToString().ToLower();
                                return RedirectToAction("UploadAssesement");
                            }
                        }
                    }
                    else
                    {
                        Session["Message"] = "Please select a file";
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                        return RedirectToAction("UploadAssesement");
                    }
                }
                else
                {
                    Session["Message"] = "Please select a file";
                    Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    return RedirectToAction("UploadAssesement");
                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Upload Assesement", "UploadAssesement POst", ex);
            }
            return RedirectToAction("UploadAssesement");
        }


        public ActionResult CancelAssesement()
        {
            Session["Message"] = null;
            Session["MessageType"] = null;
            Session["objUploadAssessementModel"] = null;
            Session["dtExcel"] = null;

            return RedirectToAction("UploadAssesement");
        }

        public ActionResult UnUploadAssesement()
        {

            UploadAssessementModel objUploadAssessementModel = new UploadAssessementModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();

            //if (Session["Message"] != null && Session["MessageType"] != null)
            //{
            //    objUploadAssessementModel.Message = Convert.ToString(Session["Message"]);
            //    objUploadAssessementModel.MessageType = Convert.ToString(Session["MessageType"]);
            //    Session["Message"] = null;
            //    Session["MessageType"] = null;
            //}

            objUploadAssessementModel = (UploadAssessementModel)Session["objUploadAssessementModel"];
            //Session["objUploadAssessementModel"] = objUploadAssessementModel;

            if (objUploadAssessementModel.Message != null)
            {
                if (objUploadAssessementModel.Message.Contains("Invalid data in the sheet, please click on View UnUploaded Data to proceed..."))
                {
                    objUploadAssessementModel.Message = "";
                    objUploadAssessementModel.MessageType = "";

                }
            }

            return View("UnUploadAssesement", objUploadAssessementModel);

        }


        [HttpPost]
        public ActionResult UnUploadAssesement(UploadAssessementModel objUploadAssessementModel)
        {
            MasterViewModel objMasterViewModel = new MasterViewModel();
            UploadAssessementModel objUploadAssessementModelNew = new UploadAssessementModel();


            if (objUploadAssessementModel.TempExcelAssesementDataModels.Count  == 0)
            {
                objUploadAssessementModelNew =  (UploadAssessementModel)Session["objUploadAssessementModel"];
                objUploadAssessementModel.TempExcelAssesementDataModels = objUploadAssessementModelNew.TempExcelAssesementDataModels;
            }

            objUploadAssessementModel = objMasterViewModel.SearchAssesementUpl(objUploadAssessementModel);
            Session["objUploadAssessementModel"] = objUploadAssessementModel;
            return PartialView("UploadAssesementList", objUploadAssessementModel);
        }



        private bool fnCheckForDataCollumsIsAvialableInDataTable(string _dtColumnName, DataTable _dtexcel)
        {
            bool _isAvialble = false;
            if (_dtexcel != null && _dtexcel.Columns.Count != 0)
            {
                for (int i = 0; i <= _dtexcel.Columns.Count - 1; i++)
                {
                    if (_dtColumnName == _dtexcel.Columns[i].ColumnName.Trim())
                    {
                        _isAvialble = true;
                        break;
                    }
                }
            }
            return _isAvialble;
        }


        public static DataTable RemoveNullColumnFromDataTable(DataTable dt)
        {
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (dt.Rows[i][1] == DBNull.Value && dt.Rows[i][2] == DBNull.Value && dt.Rows[i][3] == DBNull.Value && dt.Rows[i][4] == DBNull.Value)
                    dt.Rows[i].Delete();
            }
            dt.AcceptChanges();
            return dt;
        }


        protected UploadAssessementModel SaveToDataBase(DataTable dtAssesement)
        {

            int createdBy = Convert.ToInt32(Session["UserId"]);
            MasterViewModel objMasterViewModel = new MasterViewModel();
            UploadAssessementModel objUploadAssessementModel = new UploadAssessementModel();

            try
            {
                objUploadAssessementModel = objMasterViewModel.UploadAssesement(dtAssesement, Convert.ToInt32(Session["UserId"]), CommonUtils.PageSize);
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Upload Assesement", "SaveToDataBase POst", ex);
            }
            return objUploadAssessementModel;
        }





        public ActionResult EditAssesementUpload(string rowid)
        {

            TempExcelAssesementDataModel objTempExcelAssesementDataModel = new TempExcelAssesementDataModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();


            ViewBag.BusinessFunction = new SelectList(new[]
                                          {
                                              new {ID="",Name=""},
                                              new {ID="OSP",Name="OSP"},
                                              new{ID="ISP",Name="ISP"},
                                              new{ID="OSP,ISP",Name="OSP,ISP"},
                                          },
                        "ID", "Name", "");

            ViewBag.IsAnswer = new SelectList(new[]
                                          {
                                              new {ID="",Name=""},
                                              new {ID="Yes",Name="Yes"},
                                              new{ID="No",Name="No"},
                                          },
                         "ID", "Name", "");


            ViewBag.AnswerType = new SelectList(new[]
                                          {
                                              new {ID="Single Answer",Name="Single Answer"},
                                              new{ID="Multi Answer",Name="Multi Answer"},
                                          },
                       "ID", "Name", "Single Answer");


            objTempExcelAssesementDataModel = objMasterViewModel.GetAssesementUploadByID(rowid);


            ViewBag.QuestionType = new SelectList((from c in objMasterViewModel.GetAllQuestionType()
                                                   select new { c.QuestionTypeName }).ToList(), "QuestionTypeName", "QuestionTypeName", objTempExcelAssesementDataModel.QuestionTypeName);


            return View("_EditUplAssesement", objTempExcelAssesementDataModel);

        }


        [HttpPost]
        //[Filters.Authorized()]
        //[ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        public ActionResult EditAssesementUpload(TempExcelAssesementDataModel objTempExcelAssesementDataModel)
        {
            int createdBy = Convert.ToInt32(Session["UserId"]);
            string strMessage = string.Empty;
            string strRetMessage = string.Empty;
            string redirectUrl = string.Empty;
            UploadAssessementModel objUploadAssessementModel = new UploadAssessementModel();
            MasterViewModel objMasterViewModel = new MasterViewModel();
            DataTable dtExcel = new DataTable();

            try
            {
             
                dtExcel = (DataTable)Session["dtExcel"];

                if (ModelState.IsValid)
                {

                    objUploadAssessementModel = (UploadAssessementModel)Session["objUploadAssessementModel"];
                    
                    foreach (TempExcelAssesementDataModel item in objUploadAssessementModel.TempExcelAssesementDataModels)
                    {
                        if (objTempExcelAssesementDataModel.RowID == item.RowID)
                        {


                            item.ExcelId = objTempExcelAssesementDataModel.ExcelId;
                            item.RowID = objTempExcelAssesementDataModel.RowID;

                            item.Category = objTempExcelAssesementDataModel.Category;
                            item.AssessmentTopic = objTempExcelAssesementDataModel.AssessmentTopic;
                            item.QuestionDescription = objTempExcelAssesementDataModel.QuestionDescription;

                            item.Answer1 = objTempExcelAssesementDataModel.Answer1;
                            item.Answer2 = objTempExcelAssesementDataModel.Answer2;
                            item.Answer3 = objTempExcelAssesementDataModel.Answer3;
                            item.Answer4 = objTempExcelAssesementDataModel.Answer4;
                            item.Answer5 = objTempExcelAssesementDataModel.Answer5;
                            item.Answer6 = objTempExcelAssesementDataModel.Answer6;

                            item.IsAnswer1 = objTempExcelAssesementDataModel.IsAnswer1;
                            item.IsAnswer2 = objTempExcelAssesementDataModel.IsAnswer2;
                            item.IsAnswer3 = objTempExcelAssesementDataModel.IsAnswer3;
                            item.IsAnswer4 = objTempExcelAssesementDataModel.IsAnswer4;
                            item.IsAnswer5 = objTempExcelAssesementDataModel.IsAnswer5;
                            item.IsAnswer6 = objTempExcelAssesementDataModel.IsAnswer6;


                            item.IsSingleAnswer = objTempExcelAssesementDataModel.IsSingleAnswer;
                            item.QuestionTypeName = objTempExcelAssesementDataModel.QuestionTypeName;
                            item.ErrMsg = objTempExcelAssesementDataModel.ErrMsg;
                            item.TotalCount = objTempExcelAssesementDataModel.TotalCount;

                            strMessage = objMasterViewModel.UpdateAssesementUpl(item);


                            foreach (DataRow row in dtExcel.Rows)
                            {
                                if (row["RowID"].ToString() == objTempExcelAssesementDataModel.RowID)
                                {
                                    
                                    //row["ExcelId"] = objTempExcelAssesementDataModel.ExcelId;
                                    //row["RowID"] = objTempExcelAssesementDataModel.RowID;

                                    row["Category"] = objTempExcelAssesementDataModel.Category;
                                    row["AssessmentTopic"] = objTempExcelAssesementDataModel.AssessmentTopic;
                                    row["QuestionDescription"] = objTempExcelAssesementDataModel.QuestionDescription;

                                    row["Answer1"] = objTempExcelAssesementDataModel.Answer1;
                                    row["Answer2"] = objTempExcelAssesementDataModel.Answer2;
                                    row["Answer3"] = objTempExcelAssesementDataModel.Answer3;
                                    row["Answer4"] = objTempExcelAssesementDataModel.Answer4;
                                    row["Answer5"] = objTempExcelAssesementDataModel.Answer5;
                                    row["Answer6"] = objTempExcelAssesementDataModel.Answer6;

                                    row["IsAnswer1"] = objTempExcelAssesementDataModel.IsAnswer1;
                                    row["IsAnswer2"] = objTempExcelAssesementDataModel.IsAnswer2;
                                    row["IsAnswer3"] = objTempExcelAssesementDataModel.IsAnswer3;
                                    row["IsAnswer4"] = objTempExcelAssesementDataModel.IsAnswer4;
                                    row["IsAnswer5"] = objTempExcelAssesementDataModel.IsAnswer5;
                                    row["IsAnswer6"] = objTempExcelAssesementDataModel.IsAnswer6;


                                    row["IsSingleAnswer"] = objTempExcelAssesementDataModel.IsSingleAnswer;
                                    row["QuestionTypeName"] = objTempExcelAssesementDataModel.QuestionTypeName;
                                   // row["ErrMsg"] = objTempExcelAssesementDataModel.ErrMsg;
                                   // row["TotalCount"] = objTempExcelAssesementDataModel.TotalCount;

                                }
                            }
                        }
                    }

                    //

                   
                    if (strMessage == "Success")
                    {
                        strMessage = UserMessage.SUCCESS_MESSAGE_EDIT;
                        Session["Message"] = string.Format(strMessage, "UnUploaded Assesement");
                        Session["MessageType"] = MessageType.Success.ToString().ToLower();
                    }
                    else
                    {
                        Session["Message"] = "Error while saving";
                        Session["MessageType"] = MessageType.Error.ToString().ToLower();
                    }

                    objUploadAssessementModel.Message = Convert.ToString(Session["Message"]);
                    objUploadAssessementModel.MessageType = Convert.ToString(Session["MessageType"]);



                    
                    Session["dtExcel"] = dtExcel;
                    Session["objUploadAssessementModel"] = objUploadAssessementModel;

                }
            }
            catch (Exception ex)
            {
                Session["ExceptionMsg"] = objCommonUtilError.ErrorLog(createdBy.ToString(), "Assesement Upload", "EditAssesementUpload Post", ex);
            }

        

            redirectUrl = Url.Action("UnUploadAssesement");

            return Json(new { url = redirectUrl, message = Session["Message"], messageType = Session["MessageType"] });
            //return View("UnUploadAssesement", objUploadAssessementModel);
        }




        public ActionResult SaveAssesementUpl()
        {

            DataTable dtExcel = new DataTable();
            DataTable dtValidationReturn = new DataTable();
            CommonUtils objCommonUtils = new CommonUtils();
            UploadAssessementModel objUploadAssessementModel = new UploadAssessementModel();
           

            dtExcel = (DataTable)Session["dtExcel"];

            dtExcel.Columns.Remove("ErrMsg");
            dtExcel.Columns.Remove("ExcelId");
            dtExcel.Columns.Remove("RowID");
            
            objUploadAssessementModel = SaveToDataBase(dtExcel);

            dtValidationReturn = objCommonUtils.ConvertToDataTable(objUploadAssessementModel.TempExcelAssesementDataModels
                                 , new string[] { "ExcelId", "RowID", "Category", "AssessmentTopic", "QuestionDescription", "Answer1", "IsAnswer1", "Answer2", "IsAnswer2", "Answer3", "IsAnswer3", "Answer4", "IsAnswer4", "Answer5", "IsAnswer5", "Answer6", "IsAnswer6", "IsSingleAnswer", "QuestionTypeName", "ErrMsg" });

           
            Session["objUploadAssessementModel"] = objUploadAssessementModel;
            //Session["dtExcel"] = dtValidationReturn;

            if (dtValidationReturn.Rows.Count > 0)
            {
                Session["Message"] = "Invalid data, please edit the Grid data";
                Session["MessageType"] = MessageType.Error.ToString().ToLower();

                return RedirectToAction("UnUploadAssesement", objUploadAssessementModel);
            }
            else
            {
                Session["Message"] = "Saved successfully";
                Session["MessageType"] = MessageType.Success.ToString().ToLower();
                Session["objUploadAssessementModel"] = null;
                Session["dtExcel"] = null;

                return RedirectToAction("UploadAssesement");
            }
        }


        public FileResult DownloadTemplate()
        {
            string fileName = Server.MapPath(@"\ExcelTemplate\AssesementUploadTemplate.xlsx");
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return new FilePathResult(fileName, contentType);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CTMS.Models;
using CTMS.DataModels;
using CTMS.Common;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Data;
namespace CTMS.ViewModel
{
	public class BookedCandidateViewModel
	{

		public void GetTrainingDetailsByScheduledTrainingID(int scheduledTrainingID, ViewScheduledTrainingDetails objViewScheduledTrainingDetails)
		{
			spGetTrainingDetailsByScheduleId_Result objGetTrainingDetailsByScheduleId_Result;
			List<spGetBookedCandidteList_Result> objLstGetBookedCandidteList_Result;
			List<BookedCandidateGridModel> lstBookedCandidateGridModel;
			spGetProgramDetailsByProgramId_Result objGetProgramDetailsByProgramId_Result;

			ScheduledTrainingDetail objScheduledTrainingDetail;
			using (var objCTMSEntities = new CTMSEntities())
			{
				objGetTrainingDetailsByScheduleId_Result = new spGetTrainingDetailsByScheduleId_Result();
				//objGetTrainingDetailsByScheduleId_Result = objCTMSEntities.spGetTrainingDetailsByScheduleId(scheduledTrainingID).SingleOrDefault<spGetTrainingDetailsByScheduleId_Result>();

				objGetProgramDetailsByProgramId_Result = objCTMSEntities.spGetProgramDetailsByProgramId(scheduledTrainingID).SingleOrDefault<spGetProgramDetailsByProgramId_Result>();

				objLstGetBookedCandidteList_Result = objCTMSEntities.spGetBookedCandidteList(	scheduledTrainingID
																								, objViewScheduledTrainingDetails.CurrentPage
																								, objViewScheduledTrainingDetails.PageSize
																								, objViewScheduledTrainingDetails.SortBy
																								, objViewScheduledTrainingDetails.SortOrder
																						).ToList<spGetBookedCandidteList_Result>();

			}


			objScheduledTrainingDetail = new ScheduledTrainingDetail();
			if (objGetProgramDetailsByProgramId_Result != null)
			{
				objScheduledTrainingDetail.ScheduledTrainingID = (int)objGetProgramDetailsByProgramId_Result.ProgramID;
				//objScheduledTrainingDetail.TrainerID = (int)objGetProgramDetailsByProgramId_Result.TrainingID;
				objScheduledTrainingDetail.TrainingName = objGetProgramDetailsByProgramId_Result.ProgramName;
				//objScheduledTrainingDetail.TrainingTypeName = objGetProgramDetailsByProgramId_Result.TrainingTypeName;
				objScheduledTrainingDetail.TrainingDuration = (int)objGetProgramDetailsByProgramId_Result.ProgramDuration;
				objScheduledTrainingDetail.StartDateTime = (DateTime)objGetProgramDetailsByProgramId_Result.ProgramStartDateTime;
				objScheduledTrainingDetail.EndDateTime = (DateTime)objGetProgramDetailsByProgramId_Result.ProgramEndDateTime;
				objScheduledTrainingDetail.MaxAllowed = (int)objGetProgramDetailsByProgramId_Result.MaxAllowed;
				objScheduledTrainingDetail.MaintenancePointName = objGetProgramDetailsByProgramId_Result.Venue;
				objScheduledTrainingDetail.BookingDone = (int)objGetProgramDetailsByProgramId_Result.BookingDone;
				objScheduledTrainingDetail.FreePlace = (int)objGetProgramDetailsByProgramId_Result.FreePlace;
                objScheduledTrainingDetail.WaitListCount = (int)objGetProgramDetailsByProgramId_Result.WaitListCount;
                objScheduledTrainingDetail.ProgramStatusName = objGetProgramDetailsByProgramId_Result.ProgramStatusName;
				//objScheduledTrainingDetail.Function = objGetProgramDetailsByProgramId_Result.Function;
				//objScheduledTrainingDetail.Category = objGetProgramDetailsByProgramId_Result.Category;
				//objScheduledTrainingDetail.City = objGetProgramDetailsByProgramId_Result.City;
			}
			//if (objGetTrainingDetailsByScheduleId_Result != null)
			//{
			//    objScheduledTrainingDetail.ScheduledTrainingID = (int)objGetTrainingDetailsByScheduleId_Result.ScheduledTrainingID;
			//    objScheduledTrainingDetail.TrainerID = (int)objGetTrainingDetailsByScheduleId_Result.TrainingID;
			//    objScheduledTrainingDetail.TrainingName = objGetTrainingDetailsByScheduleId_Result.TrainingName;
			//    objScheduledTrainingDetail.TrainingTypeName = objGetTrainingDetailsByScheduleId_Result.TrainingTypeName;
			//    objScheduledTrainingDetail.TrainingDuration = (int)objGetTrainingDetailsByScheduleId_Result.Duration;
			//    objScheduledTrainingDetail.StartDateTime = (DateTime)objGetTrainingDetailsByScheduleId_Result.StartDateTime;
			//    objScheduledTrainingDetail.EndDateTime = (DateTime)objGetTrainingDetailsByScheduleId_Result.EndDateTime;
			//    objScheduledTrainingDetail.MaxAllowed = (int)objGetTrainingDetailsByScheduleId_Result.MaxAllowed;
			//    objScheduledTrainingDetail.MaintenancePointName = objGetTrainingDetailsByScheduleId_Result.MaintainancePointName;
			//    objScheduledTrainingDetail.BookingDone = (int)objGetTrainingDetailsByScheduleId_Result.BookingDone;
			//    objScheduledTrainingDetail.FreePlace = (int)objGetTrainingDetailsByScheduleId_Result.FreePlace;
			//    objScheduledTrainingDetail.Function = objGetTrainingDetailsByScheduleId_Result.Function;
			//    objScheduledTrainingDetail.Category = objGetTrainingDetailsByScheduleId_Result.Category;
			//    objScheduledTrainingDetail.City = objGetTrainingDetailsByScheduleId_Result.City;
			//}

			lstBookedCandidateGridModel = new List<BookedCandidateGridModel>();
			if (objLstGetBookedCandidteList_Result != null && objLstGetBookedCandidteList_Result.Count > 0)
			{
				foreach (spGetBookedCandidteList_Result item in objLstGetBookedCandidteList_Result)
				{
					BookedCandidateGridModel objBookedCandidateGridModel = new BookedCandidateGridModel();
					objBookedCandidateGridModel = CommonUtils.GetComplexTypeToEntity<BookedCandidateGridModel>(item);
					lstBookedCandidateGridModel.Add(objBookedCandidateGridModel);
				}
			}


			objViewScheduledTrainingDetails.ScheduleTraining= objScheduledTrainingDetail;
			objViewScheduledTrainingDetails.BookedCandidateList = lstBookedCandidateGridModel;


			if (objViewScheduledTrainingDetails != null && objViewScheduledTrainingDetails.BookedCandidateList != null && objViewScheduledTrainingDetails.BookedCandidateList.Count > 0)
			{
				int totalRecord = (int)objViewScheduledTrainingDetails.BookedCandidateList[0].TotalCount;
				if (decimal.Remainder(totalRecord, objViewScheduledTrainingDetails.PageSize) > 0)
					objViewScheduledTrainingDetails.TotalPages = (totalRecord / objViewScheduledTrainingDetails.PageSize + 1);
				else
					objViewScheduledTrainingDetails.TotalPages = totalRecord / objViewScheduledTrainingDetails.PageSize;
			}
			else
			{
				objViewScheduledTrainingDetails.TotalPages = 0;
			}
		}

		public IList<NonVLMSCandidate> GetNonVLMSCandidateList(string CandidateName=null, string EmailAddress=null )
		{
			List<NonVLMSCandidate> lstNonVLMSCandidate = new List<NonVLMSCandidate>();
			NonVLMSCandidate objNonVLMSCandidate = new NonVLMSCandidate();
			List<spGetNonVLMSCandidateList_Result> lstNonVLMSCandidateList_Result = new List<spGetNonVLMSCandidateList_Result>();
			using (var objCTMSEntities = new CTMSEntities())
			{
				lstNonVLMSCandidateList_Result = objCTMSEntities.spGetNonVLMSCandidateList(CandidateName, EmailAddress).ToList<spGetNonVLMSCandidateList_Result>();
			}

			if (lstNonVLMSCandidateList_Result != null && lstNonVLMSCandidateList_Result.Count > 0)
			{
				foreach (spGetNonVLMSCandidateList_Result item in lstNonVLMSCandidateList_Result)
				{
					objNonVLMSCandidate = CommonUtils.GetComplexTypeToEntity<NonVLMSCandidate>(item);
					lstNonVLMSCandidate.Add(objNonVLMSCandidate);
				}
			}
			return lstNonVLMSCandidate;
		}

		public void GetVLMSCandidateList(int UserId, int RoleId,int ScheduledTrainingId, ViewAddCandidateModel objViewAddCandidateModel)
		{

			List<spGetVLMSCandidatesForBooking_Result> objLstVLMSCandidatesForBooking_Result;
			List<VLMSCandidate> lstVLMSCandidate;
			VLMSCandidate objVLMSCandidate;
			using (var objCTMSEntities = new CTMSEntities())
			{
				objLstVLMSCandidatesForBooking_Result = objCTMSEntities.spGetVLMSCandidatesForBooking(	  UserId
																										, RoleId
																										, ScheduledTrainingId
																										, objViewAddCandidateModel.FilterName
																										, objViewAddCandidateModel.CurrentPage
																										, objViewAddCandidateModel.PageSize
																										, objViewAddCandidateModel.SortBy
																										, objViewAddCandidateModel.SortOrder
																									 ).ToList<spGetVLMSCandidatesForBooking_Result>();

			}

			lstVLMSCandidate = new List<VLMSCandidate>();
			if (objLstVLMSCandidatesForBooking_Result!=null && objLstVLMSCandidatesForBooking_Result.Count> 0)
			{
				foreach (spGetVLMSCandidatesForBooking_Result item in objLstVLMSCandidatesForBooking_Result)
				{
					objVLMSCandidate = new VLMSCandidate();

					objVLMSCandidate.CandidateId = item.CandidateId;
					objVLMSCandidate.CandidateName = item.CandidateName;
					objVLMSCandidate.IsSelected =Convert.ToBoolean(item.IsSelected);
					objVLMSCandidate.TotalCount =(int) item.TotalCount;
					objVLMSCandidate.MaintenancePointName = item.MaintenancePointName;
					objVLMSCandidate.FunctionName = item.FunctionName;
					objVLMSCandidate.CategoryName = item.CategoryName;
					//objVLMSCandidate = CommonUtils.GetComplexTypeToEntity<VLMSCandidate>(item);
					lstVLMSCandidate.Add(objVLMSCandidate);
				}
			}

			

			objViewAddCandidateModel.VLMSCandidateList = lstVLMSCandidate;
			if (objViewAddCandidateModel != null && objViewAddCandidateModel.VLMSCandidateList != null && objViewAddCandidateModel.VLMSCandidateList.Count > 0)
			{
				int totalRecord = (int)objViewAddCandidateModel.VLMSCandidateList[0].TotalCount;
				if (decimal.Remainder(totalRecord, objViewAddCandidateModel.PageSize) > 0)
					objViewAddCandidateModel.TotalPages = (totalRecord / objViewAddCandidateModel.PageSize + 1);
				else
					objViewAddCandidateModel.TotalPages = totalRecord / objViewAddCandidateModel.PageSize;
			}
			else
			{
				objViewAddCandidateModel.TotalPages = 0;
			}
		}

		 
		public void AddNewCandidate(int UserId, int CandidateType, int ScheduledTrainingId, string CandidateIds, string CandidateName, string EmailId, string ContactNo, string Company, string DomainName,  out int errorCode, out string errorMessage)
		{
			errorCode = 1;
			errorMessage = String.Empty;
            decimal WaitListPercentage = CommonUtils.WaitListPercentage;
			using (var objCTMSEntities = new CTMSEntities())
			{
				ObjectParameter opErrorCode = new ObjectParameter("ErrorCode", typeof(Int32));
				ObjectParameter opErrorMessage = new ObjectParameter("ErrorMessage", typeof(string));
				objCTMSEntities.spAddNewCandidate(UserId, CandidateType, ScheduledTrainingId, CandidateIds, CandidateName, EmailId, ContactNo, Company, DomainName,WaitListPercentage,opErrorCode,opErrorMessage);

				errorCode = Convert.ToInt32(opErrorCode.Value);
				errorMessage = Convert.ToString(opErrorMessage.Value);
			}
		}


		public void DeleteCandidate(int BookedCandidateId,out int errorCode, out string errorMessage)
		{
			errorCode = 0;
			errorMessage = String.Empty;
			using (var objCTMSEntities = new CTMSEntities())
			{
				ObjectParameter opErrorCode = new ObjectParameter("ErrorCode", typeof(Int32));
				ObjectParameter opErrorMessage = new ObjectParameter("ErrorMessage", typeof(string));
				objCTMSEntities.spDeleteBookedCandidate(BookedCandidateId, opErrorCode, opErrorMessage);
				errorCode = Convert.ToInt32(opErrorCode.Value);
				errorMessage = Convert.ToString(opErrorMessage.Value);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheduledTrainingID"></param>
		/// <param name="objViewScheduledTrainingDetails"></param>
		public void GetParticipantFeedback(int scheduledTrainingID,int TrainingId, int RoleId,ViewScheduledTrainingDetails objViewScheduledTrainingDetails)
		{
			spGetTrainingDetailsByScheduleId_Result objGetTrainingDetailsByScheduleId_Result;
			List<spGetBookedCandidteList_Result> objLstGetBookedCandidteList_Result;
			List<BookedCandidateGridModel> lstBookedCandidateGridModel;
			List<spGetParticipantFeedback_Result> lstParticipantFeedback_Result;
			ParticipantFeedbackModel objParticipantFeedbackModel;
			List<ParticipantFeedbackModel> lstParticipantFeedbackModel;
			ScheduledTrainingDetail objScheduledTrainingDetail;
			using (var objCTMSEntities = new CTMSEntities())
			{
				objGetTrainingDetailsByScheduleId_Result = new spGetTrainingDetailsByScheduleId_Result();
				objGetTrainingDetailsByScheduleId_Result = objCTMSEntities.spGetTrainingDetailsByScheduleId(scheduledTrainingID, TrainingId).SingleOrDefault<spGetTrainingDetailsByScheduleId_Result>();



				objLstGetBookedCandidteList_Result = objCTMSEntities.spGetBookedCandidteList(scheduledTrainingID
																								, objViewScheduledTrainingDetails.CurrentPage
																								, objViewScheduledTrainingDetails.PageSize
																								, objViewScheduledTrainingDetails.SortBy
																								, objViewScheduledTrainingDetails.SortOrder
																						).ToList<spGetBookedCandidteList_Result>();


				objScheduledTrainingDetail = new ScheduledTrainingDetail();
				if (objGetTrainingDetailsByScheduleId_Result != null)
				{
					objScheduledTrainingDetail.TrainingID = (int)objGetTrainingDetailsByScheduleId_Result.TrainingID;
					objScheduledTrainingDetail.ScheduledTrainingID = (int)objGetTrainingDetailsByScheduleId_Result.ScheduledTrainingID;
					objScheduledTrainingDetail.TrainerID = (int)objGetTrainingDetailsByScheduleId_Result.TrainingID;
					objScheduledTrainingDetail.TrainingName = objGetTrainingDetailsByScheduleId_Result.TrainingName;
					objScheduledTrainingDetail.TrainingTypeName = objGetTrainingDetailsByScheduleId_Result.TrainingTypeName;
					objScheduledTrainingDetail.TrainingDuration = (int)objGetTrainingDetailsByScheduleId_Result.Duration;
					objScheduledTrainingDetail.StartDateTime = (DateTime)objGetTrainingDetailsByScheduleId_Result.StartDateTime;
					objScheduledTrainingDetail.EndDateTime = (DateTime)objGetTrainingDetailsByScheduleId_Result.EndDateTime;
					objScheduledTrainingDetail.MaxAllowed = (int)objGetTrainingDetailsByScheduleId_Result.MaxAllowed;
					objScheduledTrainingDetail.MaintenancePointName = objGetTrainingDetailsByScheduleId_Result.MaintainancePointName;
					objScheduledTrainingDetail.BookingDone = (int)objGetTrainingDetailsByScheduleId_Result.BookingDone;
					objScheduledTrainingDetail.FreePlace = (int)objGetTrainingDetailsByScheduleId_Result.FreePlace;
					objScheduledTrainingDetail.Function = objGetTrainingDetailsByScheduleId_Result.Function;
					objScheduledTrainingDetail.Category = objGetTrainingDetailsByScheduleId_Result.Category;
					objScheduledTrainingDetail.City = objGetTrainingDetailsByScheduleId_Result.City;
				}

				lstBookedCandidateGridModel = new List<BookedCandidateGridModel>();
				if (objLstGetBookedCandidteList_Result != null && objLstGetBookedCandidteList_Result.Count > 0)
				{
                    objLstGetBookedCandidteList_Result = objLstGetBookedCandidteList_Result.Where(b => b.BookingStatusName.ToLower() == "booked").ToList();
					foreach (spGetBookedCandidteList_Result item in objLstGetBookedCandidteList_Result)
					{
						BookedCandidateGridModel objBookedCandidateGridModel = new BookedCandidateGridModel();
						objBookedCandidateGridModel = CommonUtils.GetComplexTypeToEntity<BookedCandidateGridModel>(item);
						

						lstParticipantFeedback_Result = new List<spGetParticipantFeedback_Result>();
						lstParticipantFeedback_Result = objCTMSEntities.spGetParticipantFeedback(objBookedCandidateGridModel.BookedCandidateID, objBookedCandidateGridModel.ScheduledTrainingID, RoleId, TrainingId).ToList<spGetParticipantFeedback_Result>();
						

						lstParticipantFeedbackModel=new List<ParticipantFeedbackModel>();
						if (lstParticipantFeedback_Result != null && lstParticipantFeedback_Result.Count > 0)
						{
							foreach (spGetParticipantFeedback_Result Feedback in lstParticipantFeedback_Result)
							{
								objParticipantFeedbackModel=new ParticipantFeedbackModel();
								objParticipantFeedbackModel=CommonUtils.GetComplexTypeToEntity<ParticipantFeedbackModel>(Feedback);
								lstParticipantFeedbackModel.Add(objParticipantFeedbackModel);
							}
						}

						objBookedCandidateGridModel.Feedback = lstParticipantFeedbackModel;
						lstBookedCandidateGridModel.Add(objBookedCandidateGridModel);

						lstParticipantFeedback_Result = null;
						lstParticipantFeedbackModel = null;
					}
				}

			}


			


			objViewScheduledTrainingDetails.ScheduleTraining = objScheduledTrainingDetail;
			objViewScheduledTrainingDetails.BookedCandidateList = lstBookedCandidateGridModel;


			if (objViewScheduledTrainingDetails != null && objViewScheduledTrainingDetails.BookedCandidateList != null && objViewScheduledTrainingDetails.BookedCandidateList.Count > 0)
			{
				int totalRecord = (int)objViewScheduledTrainingDetails.BookedCandidateList[0].TotalCount;
				if (decimal.Remainder(totalRecord, objViewScheduledTrainingDetails.PageSize) > 0)
					objViewScheduledTrainingDetails.TotalPages = (totalRecord / objViewScheduledTrainingDetails.PageSize + 1);
				else
					objViewScheduledTrainingDetails.TotalPages = totalRecord / objViewScheduledTrainingDetails.PageSize;
			}
			else
			{
				objViewScheduledTrainingDetails.TotalPages = 0;
			}
		}


		public void SaveCandidateFeedback(List<SubmitFeedbackModel> lstParticipantFeedback, int TrainingID, bool IsSubmitted, int UserId, out int errorCode, out string errorMessage)
		{
			errorCode = 1;
			errorMessage = String.Empty;
			CommonUtils objCommonUtils = new CommonUtils();
			using (var objCTMSEntities = new CTMSEntities())
			{
				SqlParameter pParticipantFeedback = new SqlParameter("@ttyFeedback", SqlDbType.Structured);
				pParticipantFeedback.Value = objCommonUtils.ConvertToDataTable(lstParticipantFeedback, new string[] { "ScheduledTrainingID", "BookedCandidateId", "FeedbackId", "FeedbackPonit" });
				pParticipantFeedback.TypeName = "ttyFeedback";

				SqlParameter pIsSubmitted = new SqlParameter("@IsSubmitted", SqlDbType.Bit);
				pIsSubmitted.Value = IsSubmitted;

				SqlParameter pUserId = new SqlParameter("@UserId", SqlDbType.Int);
				pUserId.Value = UserId;

				SqlParameter pTrainingID = new SqlParameter("@TrainingID", SqlDbType.Int);
				pTrainingID.Value = TrainingID;

				SqlParameter pErrorCode = new SqlParameter("@ERRORCODE", SqlDbType.Int);
				pErrorCode.Value = errorCode;
				pErrorCode.Direction = ParameterDirection.Output;

				SqlParameter pErrorMessage = new SqlParameter("@ERRORMESSAGE", SqlDbType.VarChar, 200);
				pErrorMessage.Value = errorMessage;
				pErrorMessage.Direction = ParameterDirection.Output;

				objCTMSEntities.ExecuteStoreCommand("exec spSaveCandidateFeedback @UserId,@IsSubmitted,@ttyFeedback,@TrainingID,@ERRORCODE OUTPUT,@ERRORMESSAGE OUTPUT"
					, new object[] { pUserId, pIsSubmitted, pParticipantFeedback, pTrainingID, pErrorCode, pErrorMessage });

				errorCode = Convert.ToInt32(pErrorCode.Value);
				errorMessage = Convert.ToString(pErrorMessage.Value);
			}
		}


        /// <summary>
        /// Get Participant Online Feedback
        /// </summary>
        /// <param name="objOnlineFeedbackModel"></param>
        public OnlineFeedbackModel GetParticipantOnlineFeedback(OnlineFeedbackModel objOnlineFeedbackModel)
        {
            int bookedCandidateID, userId;
            bookedCandidateID = objOnlineFeedbackModel.BookedCandidateID;
            userId = objOnlineFeedbackModel.CreatedBy;
            List<spGetOnlineParticipantFeedback_Result> lstOnlineParticipantFeedback_Result;
            ParticipantFeedbackModel objParticipantFeedbackModel;
            List<ParticipantFeedbackModel> lstParticipantFeedbackModel;
           
            using (var objCTMSEntities = new CTMSEntities())
            {
                
                        lstOnlineParticipantFeedback_Result = new List<spGetOnlineParticipantFeedback_Result>();
                        lstOnlineParticipantFeedback_Result = objCTMSEntities.spGetOnlineParticipantFeedback(objOnlineFeedbackModel.BookedCandidateID,objOnlineFeedbackModel.CreatedBy).ToList<spGetOnlineParticipantFeedback_Result>();


                        lstParticipantFeedbackModel = new List<ParticipantFeedbackModel>();
                        if (lstOnlineParticipantFeedback_Result != null && lstOnlineParticipantFeedback_Result.Count > 0)
                        {
                            objOnlineFeedbackModel = CommonUtils.GetComplexTypeToEntity<OnlineFeedbackModel>(lstOnlineParticipantFeedback_Result[0]);
                            foreach (spGetOnlineParticipantFeedback_Result Feedback in lstOnlineParticipantFeedback_Result)
                            {
                                objParticipantFeedbackModel = new ParticipantFeedbackModel();
                                objParticipantFeedbackModel = CommonUtils.GetComplexTypeToEntity<ParticipantFeedbackModel>(Feedback);
                                lstParticipantFeedbackModel.Add(objParticipantFeedbackModel);
                            }
                        }
             }

            objOnlineFeedbackModel.Feedbacks = lstParticipantFeedbackModel;
            objOnlineFeedbackModel.BookedCandidateID = bookedCandidateID;
            objOnlineFeedbackModel.CreatedBy = userId;
            return objOnlineFeedbackModel;
         }

        /// <summary>
        /// Save Candidate Online Feedback
        /// </summary>
        /// <param name="lstParticipantFeedback"></param>
        /// <param name="TrainingID"></param>
        /// <param name="IsSubmitted"></param>
        /// <param name="UserId"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        public void SaveCandidateOnlineFeedback(List<SubmitFeedbackModel> lstParticipantFeedback, int TrainingID, bool IsSubmitted, int UserId, out int errorCode, out string errorMessage)
        {
            errorCode = 1;
            errorMessage = String.Empty;
            CommonUtils objCommonUtils = new CommonUtils();
            using (var objCTMSEntities = new CTMSEntities())
            {
                SqlParameter pParticipantFeedback = new SqlParameter("@ttyFeedback", SqlDbType.Structured);
                pParticipantFeedback.Value = objCommonUtils.ConvertToDataTable(lstParticipantFeedback, new string[] { "ScheduledTrainingID", "BookedCandidateId", "FeedbackId", "FeedbackPonit" });
                pParticipantFeedback.TypeName = "ttyFeedback";

                SqlParameter pIsSubmitted = new SqlParameter("@IsSubmitted", SqlDbType.Bit);
                pIsSubmitted.Value = IsSubmitted;

                SqlParameter pUserId = new SqlParameter("@UserId", SqlDbType.Int);
                pUserId.Value = UserId;

                SqlParameter pTrainingID = new SqlParameter("@TrainingID", SqlDbType.Int);
                pTrainingID.Value = TrainingID;

                SqlParameter pErrorCode = new SqlParameter("@ERRORCODE", SqlDbType.Int);
                pErrorCode.Value = errorCode;
                pErrorCode.Direction = ParameterDirection.Output;

                SqlParameter pErrorMessage = new SqlParameter("@ERRORMESSAGE", SqlDbType.VarChar, 200);
                pErrorMessage.Value = errorMessage;
                pErrorMessage.Direction = ParameterDirection.Output;

                objCTMSEntities.ExecuteStoreCommand("exec spSaveCandidateOnlineFeedback @UserId,@IsSubmitted,@ttyFeedback,@TrainingID,@ERRORCODE OUTPUT,@ERRORMESSAGE OUTPUT"
                    , new object[] { pUserId, pIsSubmitted, pParticipantFeedback, pTrainingID, pErrorCode, pErrorMessage });

                errorCode = Convert.ToInt32(pErrorCode.Value);
                errorMessage = Convert.ToString(pErrorMessage.Value);
            }
        }
	}
}

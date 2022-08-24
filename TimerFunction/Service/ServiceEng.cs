using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NotificationFunction.Entitys;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.Bot.Builder;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using Persistence.Repositories;
using Domain;
using System.Buffers.Text;
using System.Globalization;
using AutoMapper;
using NotificationFunction.Service.QueueService;

namespace NotificationFunction.Service
{
    public class ServiceEng : IService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogsRepository _logsRepository;
        private readonly IQueueService _queueService;
        private readonly IMapper _mapper;

        public ServiceEng(IMapper mapper,IConfiguration configuration, ILogsRepository logsRepository,IQueueService queueService)
        {
            _mapper = mapper;
            _configuration = configuration;
            _logsRepository = logsRepository;
            _queueService = queueService;
        }
        public async Task<string> GetNotificationShifts()
        {
            var shiftiInterval = _configuration.GetValue<int>("ShiftiInterval");

            #region to test local
            //for test local
            DateTime currentDateTime = DateTime.Parse("2022-08-10T08:30:00",
                 CultureInfo.InvariantCulture,
                 DateTimeStyles.AdjustToUniversal);
            #endregion
            // var currentDateTime = DateTime.UtcNow;
            var upperInterval = currentDateTime.AddMinutes(shiftiInterval);
            var todayShifts = upperInterval.Date;
            var myShiftResponse = await GetDataFromShiftApi(todayShifts);
            
            var dataToBeNotify = JsonConvert.DeserializeObject<PaginatedList<ShiftEntity>>(myShiftResponse);
           
            var listUpcomingShiftsStart = dataToBeNotify.Items.
                Where(x => x.StartDateTime.TimeOfDay.TotalMinutes < upperInterval.TimeOfDay.TotalMinutes
                         && x.StartDateTime.TimeOfDay.TotalMinutes >= currentDateTime.TimeOfDay.TotalMinutes).ToList();

            var listUpcomingShiftsEnd = dataToBeNotify.Items.
               Where(x => x.EndDateTime.TimeOfDay.TotalMinutes < upperInterval.TimeOfDay.TotalMinutes
                        && x.EndDateTime.TimeOfDay.TotalMinutes >= currentDateTime.TimeOfDay.TotalMinutes).ToList();

            var allShifts = listUpcomingShiftsStart.Concat(listUpcomingShiftsEnd);


            //check if the data empty
            if (allShifts.Count() <= 0) return "NodataToBeNotify";
            List<ShiftEntityLog> shiftlogs = _mapper.Map<List<ShiftEntityLog>>(allShifts);

            var listofid = shiftlogs.Select(x=>x.Id).ToList();
            //get the data from DB
           var existingIdsLogs = await _logsRepository.GetLogsQueries(listofid);

            #region comparing the upcoming list with existing one
            HashSet<Guid> diffids = new HashSet<Guid>(existingIdsLogs.Select(s => s.Id));
            var results = shiftlogs.Where(m => !diffids.Contains(m.Id)).ToList();
            #endregion

            if (results.Count <= 0) return "NodataToBeNotify";
            //save the logs in DB
            await _logsRepository.CreateNewLogsCommand(results);
            return myShiftResponse;
        }
        public bool SendDataToQueue(string message)
        {
            try
            {
                _queueService.SendMessage(message);
               
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetDataFromShiftApi(DateTime TodayShifts)
        {
            HttpClient httpClient = new HttpClient();
            var BaseUrl = _configuration.GetValue<string>("BaseUrl");
            var RequstUrl = BaseUrl + "?StartDate=" + TodayShifts;
            var MyShiftResponse = await httpClient.GetStringAsync(RequstUrl);
            return MyShiftResponse;
        }
    }
}

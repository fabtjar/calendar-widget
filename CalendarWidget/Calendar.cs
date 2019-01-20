using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CalendarWidget
{
    class Calendar
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/calendar-dotnet-quickstart.json
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string ApplicationName = "Google Calendar API .NET Quickstart";

        public struct CalendarData
        {
            public List<CalendarDay> Days;
        }

        public struct CalendarDay
        {
            public string Date;
            public List<CalendarEvent> Events;
        }

        public struct CalendarEvent
        {
            public string Summary;
            public string Time;
        }

        public CalendarData GetData(int resultsCount)
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Get all calendar IDs
            List<string> calendarIds = new List<string>();
            CalendarList calendars = service.CalendarList.List().Execute();
            foreach (var calendar in calendars.Items)
            {
                calendarIds.Add(calendar.Id);
            }

            // Get events of IDs
            List<Event> eventItems = new List<Event>();
            foreach (var id in calendarIds)
            {
                // Define parameters of request.
                EventsResource.ListRequest request = service.Events.List(id);
                request.TimeMin = DateTime.Now;
                request.MaxResults = resultsCount;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                // List events.
                Events events = request.Execute();
                eventItems.AddRange(events.Items);
            }

            // Get correct count of events in order
            eventItems = eventItems.OrderBy(eventItem => {
                string startDate = eventItem.Start.Date;
                string startDateTimeRaw = eventItem.Start.DateTimeRaw;
                return DateTime.Parse(string.IsNullOrEmpty(startDate) ? startDateTimeRaw : startDate);
            }).ToList();
            eventItems.RemoveRange(resultsCount, eventItems.Count - resultsCount);

            CalendarData calendarData;
            calendarData.Days = new List<CalendarDay>();

            // Show events
            if (eventItems != null && eventItems.Count > 0)
            {
                CalendarDay calendarDay = new CalendarDay();

                string currentDate = null;

                foreach (var eventItem in eventItems)
                {
                    bool isAllDay = true;
                    string startDate = eventItem.Start.Date;

                    // Date only available in DateTimeRaw if not all day
                    if (String.IsNullOrEmpty(startDate))
                    {
                        startDate = eventItem.Start.DateTimeRaw;
                        isAllDay = false;
                    }
                    startDate = DateTime.Parse(startDate).ToString("dddd, dd MMMM yyyy");

                    if (startDate != currentDate)
                    {
                        currentDate = startDate;

                        calendarDay.Date = startDate;
                        calendarDay.Events = new List<CalendarEvent>();
                        calendarData.Days.Add(calendarDay);
                    }

                    string time = "";
                    if (!isAllDay)
                    {
                        DateTime? startTime = eventItem.Start.DateTime;
                        DateTime? endTime = eventItem.End.DateTime;
                        time = string.Format(
                            "{0} - {1}",
                            startTime.Value.ToString("h:mm tt"),
                            endTime.Value.ToString("h:mm tt")
                            );
                    }

                    CalendarEvent calendarEvent = new CalendarEvent();
                    calendarEvent.Summary = eventItem.Summary;
                    calendarEvent.Time = time;
                    calendarDay.Events.Add(calendarEvent);
                }
            }

            return calendarData;
        }
    }
}

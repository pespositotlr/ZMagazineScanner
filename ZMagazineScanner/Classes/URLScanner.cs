using ZMagazineScanner.Entities;
using ZMagazineScanner.Loggers;
using ZMagazineScanner.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace ZMagazineScanner.Classes
{
    /// <summary>
    /// Scans specific urls for if a valid page is found there. Allows for checking a range of IDs. Supports searching for issues by name.
    /// </summary>
    public class URLScanner
    {
        IConfigurationRoot config;
        Logger logger;
        EmailNotifier emailNotifier;
        int attemptNumber;
        int remainingTasks = 0;
        bool searchResultFound = false;

        public URLScanner(int maximumAttempts = 100000000, int timeBetweenAttemptsMilliseconds = 3)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            emailNotifier = new EmailNotifier(config);
        }

        public async Task CheckIssueRange(int startingInt = 0, int endingInt = 0, string searchValue = "")
        {
            //await bot.LogToDiscord();
            attemptNumber = 1;
            int currentInt = startingInt;
            HashSet<int> foundIssueIds = new HashSet<int>();
            Dictionary<int, string> foundIssueIdsToValues= new Dictionary<int, string>();
            string currentUrl = StringHelper.SetUrlToSpecificIDs(config["detailsAPIURL"], Convert.ToInt32(config["magazineId"]), config["secretId"], startingInt);
            bool rangeDone = false;
            bool issueFound = false;
            var currentIssueData = string.Empty;

            logger.Log(String.Format("Beginning scan from {0} to {1}. The current time is {2}", startingInt, endingInt, DateTime.Now.ToString()));

            while (!rangeDone)
            {
                logger.Log(attemptNumber + ": " + currentUrl);
                issueFound = false;

                try
                {

                    //Check actual HTML of result
                    currentIssueData = await WebHelper.GetIssueData(config, currentUrl);
                    if (WebHelper.IsIssueFound(currentIssueData))
                        issueFound = true;

                    //Found an issue at this URL so hold in set
                    if (issueFound)
                    {
                        logger.Log(String.Format("Issue found at {0}.", currentInt));
                        var foundValue = currentIssueData.Substring(currentIssueData.IndexOf(config["dataStartIndexValue"]) + config["dataStartIndexValue"].Length + 2, Convert.ToInt32(config["issueTitleLength"]));
                        foundIssueIds.Add(currentInt);
                        foundIssueIdsToValues.Add(currentInt, foundValue);

                        logger.Log(foundValue);

                        //If searched issue found, noftify and stop checking
                        if (!String.IsNullOrEmpty(searchValue)) {
                            if (WebHelper.IsSearchResultFound(currentIssueData, searchValue))
                            {
                                await RunFoundUrlProcess(currentInt);
                                rangeDone = true;
                                searchResultFound = true;
                            }
                        }
                    }

                    //Continue to end of range
                    currentInt++;
                    attemptNumber++;
                    currentUrl = StringHelper.SetUrlToSpecificIDs(config["detailsAPIURL"], Convert.ToInt32(config["magazineId"]), config["secretId"], currentInt);

                    if (currentInt > endingInt)
                        rangeDone = true;

                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Got an error trying to get url. Error: {0}. Current time is: {1}", ex.ToString(), DateTime.Now.ToString()));
                    await Wait(1000); //Sleep for 1 second before trying again.
                }

            }

            //After finishing range, print results of found URLs
            if (foundIssueIds.Any())
            {
                var detailsURL = config["detailsURL"];
                logger.Log("Total Found URLS: " + foundIssueIds.Count);
                foreach (int issueId in foundIssueIds)
                {
                    var updatedURL = detailsURL.Replace("{magazineId}", config["magazineId"]);
                    updatedURL = updatedURL.Replace("{issueId}", issueId.ToString());

                    try
                    {
                        logger.Log(foundIssueIdsToValues[issueId] + " : " + updatedURL);
                    } catch
                    {
                        logger.Log("COULDN'T LOG TITLE : " + updatedURL);
                    }
                }
            }

            remainingTasks--;

            return;
        }

        public async Task<bool> CheckIssueRangeParallel(int startindIssueId = 0, int issuesPerTask = 50, int issuesToCheck = 800, string searchValues = "")
        {
            var tasks = new List<Task>();
            tasks.Add(CheckIssueRange(0, 0));
            var numOfTasks = issuesToCheck / issuesPerTask;
            remainingTasks = numOfTasks;

            for (int i = 0; i < numOfTasks; i++)
            {
                int startingIssue = startindIssueId + (i * issuesPerTask);
                int endingIssue = startingIssue + (issuesPerTask - 1);
                tasks.Add(CheckIssueRange(startingIssue, endingIssue, searchValues));
            }

            await Task.WhenAll(tasks);

            if (searchResultFound)
                logger.Log(String.Format("All done! Search Found: {0}", searchResultFound));

            return searchResultFound;
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private async Task RunFoundUrlProcess(int issueId)
        {
            var detailsURL = config["detailsURL"];
            var foundURL = detailsURL.Replace("{magazineId}", config["magazineId"]);
            foundURL = foundURL.Replace("{issueId}", issueId.ToString());

            logger.Log(String.Format("########## URL found! {0} found at {1}", foundURL, DateTime.Now.ToString()));
            emailNotifier.SendNotificationUrlFound(foundURL);
            return;
        }

    }
}

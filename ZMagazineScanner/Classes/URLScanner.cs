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
        Dictionary<int, string> foundIssueIdsToValuesParallel = new Dictionary<int, string>();

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
            Dictionary<int, string> foundIssueIdsToValues = new Dictionary<int, string>();
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
                        string issueTitle = GetIssueTitle(currentIssueData);

                        foundIssueIds.Add(currentInt);
                        foundIssueIdsToValues.Add(currentInt, issueTitle);

                        logger.Log(issueTitle);

                        //If searched issue found, noftify and stop checking
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            if (WebHelper.IsSearchResultFound(currentIssueData, searchValue))
                            {
                                await RunFoundSearchValueProcess(currentInt);
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

            foreach (KeyValuePair<int, string> kvp in foundIssueIdsToValues)
            {
                foundIssueIdsToValuesParallel.Add(kvp.Key, kvp.Value);
            }

            remainingTasks--;

            return;
        }

        private string GetIssueTitle(string currentIssueData)
        {
            var issueTitle = string.Empty;

            issueTitle = StringHelper.StripNonPrintableUnicode(currentIssueData);
            int startIndex = issueTitle.IndexOf(config["dataStartIndexValue"]);
            int issueTitleMaxLength = Convert.ToInt32(config["issueTitleLength"]);

            if ((startIndex + config["dataStartIndexValue"].Length) <= (issueTitle.Length - 1))
                issueTitle = issueTitle.Substring(startIndex + config["dataStartIndexValue"].Length, issueTitleMaxLength);
            else
                issueTitle = issueTitle.Substring(startIndex);

            if (issueTitle.Contains("("))
                issueTitle = issueTitle.Substring(0, issueTitle.IndexOf("("));

            if (issueTitle.Contains("$"))
                issueTitle = issueTitle.Substring(issueTitle.IndexOf("$"));

            return issueTitle;
        }

        public async Task<bool> CheckIssueRangeParallel(int startindIssueId = 0, int issuesPerTask = 50, int issuesToCheck = 800, string searchValues = "")
        {
            logger.Log(String.Format("Beginning parallel scan of {0} items from {1} to {2}", issuesToCheck, startindIssueId, issuesPerTask));
            if(!String.IsNullOrWhiteSpace(searchValues))
                logger.Log(String.Format("---Searching for value: ", searchValues));
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

            //Print full list
            logger.Log("###Full found issues list:");
            logger.Log(foundIssueIdsToValuesParallel.Count + " items found");
            var detailsURL = config["detailsURL"].Replace("{magazineId}", config["magazineId"]);
            foreach (KeyValuePair<int, string> kvp in foundIssueIdsToValuesParallel)
            {
                var updatedURL = detailsURL.Replace("{issueId}", kvp.Key.ToString());
                logger.Log(kvp.Key + " - " + kvp.Value + " : " + updatedURL);
            }

            if (searchResultFound)
                logger.Log(String.Format("All done! Search Found: {0}", searchResultFound));

            return searchResultFound;
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private async Task RunFoundSearchValueProcess(int issueId)
        {
            var detailsURL = config["detailsURL"];
            var foundURL = detailsURL.Replace("{magazineId}", config["magazineId"]);
            foundURL = foundURL.Replace("{issueId}", issueId.ToString());

            logger.Log(String.Format("########## Searched value found! {0} found at {1}", foundURL, DateTime.Now.ToString()));
            emailNotifier.SendNotificationUrlFound(foundURL);
            return;
        }

    }
}

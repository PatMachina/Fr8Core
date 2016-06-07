﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Atlassian.Jira;
using Newtonsoft.Json;
using StructureMap;
using Fr8Data.DataTransferObjects;
using Fr8Infrastructure.Interfaces;
using TerminalBase.Errors;
using terminalAtlassian.Interfaces;
using TerminalBase.Models;

namespace terminalAtlassian.Services
{
    public class AtlassianService : IAtlassianService
    {
        private readonly IRestfulServiceClient _client;


        public AtlassianService()
        {
            _client = ObjectFactory.GetInstance<IRestfulServiceClient>();
        }

        public bool IsValidUser(CredentialsDTO curCredential)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", curCredential.Username, curCredential.Password))));

                using (HttpResponseMessage response = client.GetAsync(
                            curCredential.Domain).Result)
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
           
        }

        public void SetBasicAuthHeader(WebRequest request, String userName, String userPassword)
        {
            string authInfo = userName + ":" + userPassword;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            request.Headers["Authorization"] = "Basic " + authInfo;
        }

        private void InterceptJiraExceptions(Action process)
        {
            try
            {
                process();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("Unauthorized (401)") > -1)
                {
                    throw new AuthorizationTokenExpiredOrInvalidException("Please make sure that username, password and domain are correct.");
                }
                else
                {
                    throw;
                }
            }
        }

        private T InterceptJiraExceptions<T>(Func<T> process)
        {
            try
            {
                return process();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("Unauthorized (401)") > -1)
                {
                    throw new AuthorizationTokenExpiredOrInvalidException("Please make sure that username, password and domain are correct.");
                }
                else
                {
                    throw;
                }
            }
        }

        public List<FieldDTO> GetJiraIssue(string jiraKey, AuthorizationToken authToken)
        {
            return InterceptJiraExceptions(() =>
            {
                Jira jira = CreateRestClient(authToken.Token);
                var issue = jira.GetIssue(jiraKey);
                return CreateKeyValuePairList(issue);
            });
        }

        public List<FieldDTO> GetProjects(AuthorizationToken authToken)
        {
            return InterceptJiraExceptions(() =>
            {
                var jira = CreateRestClient(authToken.Token);

                var projects = jira.GetProjects();
                var result = projects
                    .Select(x => new FieldDTO()
                    {
                        Key = x.Name,
                        Value = x.Key
                    }
                    )
                    .ToList();

                return result;
            });
        }

        public List<FieldDTO> GetIssueTypes(string projectKey,
            AuthorizationToken authToken)
        {
            return InterceptJiraExceptions(() =>
            {
                var jira = CreateRestClient(authToken.Token);

                var issueTypes = jira.GetIssueTypes(projectKey);
                var result = issueTypes
                    .Select(x => new FieldDTO()
                        {
                            Key = x.Name,
                            Value = x.Id
                        }
                    )
                    .ToList();

                return result;
            });
        }

        public List<FieldDTO> GetPriorities(AuthorizationToken authToken)
        {
            return InterceptJiraExceptions(() =>
            {
                var jira = CreateRestClient(authToken.Token);

                var priorities = jira.GetIssuePriorities();
                var result = priorities
                    .Select(x => new FieldDTO()
                        {
                            Key = x.Name,
                            Value = x.Id
                        }
                    )
                    .ToList();

                return result;
            });
        }

        public List<FieldDTO> GetCustomFields(AuthorizationToken authToken)
        {
            return InterceptJiraExceptions(() =>
            {
                var jira = CreateRestClient(authToken.Token);
                var customFields = jira.GetCustomFields();

                var result = customFields
                    .Select(x => new FieldDTO()
                    {
                        Key = x.Name,
                        Value = x.Id
                    }
                    )
                    .OrderBy(x => x.Key)
                    .ToList();

                return result;
            });
        }

        public void CreateIssue(IssueInfo issueInfo, AuthorizationToken authToken)
        {
            InterceptJiraExceptions(() =>
            {
                var jira = CreateRestClient(authToken.Token);

                var issueTypes = jira.GetIssueTypes(issueInfo.ProjectKey);
                var issueType = issueTypes.FirstOrDefault(x => x.Id == issueInfo.IssueTypeKey);
                if (issueType == null)
                {
                    throw new ApplicationException("Invalid Jira Issue Type specified.");
                }

                var priorities = jira.GetIssuePriorities();
                var priority = priorities.FirstOrDefault(x => x.Id == issueInfo.PriorityKey);
                if (priority == null)
                {
                    throw new ApplicationException("Invalid Jira Priority specified.");
                }

                var jiraCustomFields = jira.GetCustomFields();

                var issue = jira.CreateIssue(issueInfo.ProjectKey);
                issue.Type = issueType;
                issue.Priority = priority;
                issue.Summary = issueInfo.Summary;
                issue.Description = issueInfo.Description;

                if (issueInfo.CustomFields != null)
                {
                    var customFieldsCollection = issue.CustomFields.ForEdit();
                    foreach (var customField in issueInfo.CustomFields)
                    {
                        var jiraCustomField = jiraCustomFields.FirstOrDefault(x => x.Id == customField.Key);
                        if (jiraCustomField == null)
                        {
                            throw new ApplicationException($"Invalid custom field {customField.Key}");
                        }

                        customFieldsCollection.Add(jiraCustomField.Name, customField.Value);
                    }
                }

                issue.SaveChanges();
                issueInfo.Key = issue.Key.Value;
            });
        }
        
        private List<FieldDTO> CreateKeyValuePairList(Issue curIssue)
        {
            List<FieldDTO> returnList = new List<FieldDTO>();
            returnList.Add(new FieldDTO("Key", curIssue.Key.Value));
            returnList.Add(new FieldDTO("Summary", curIssue.Summary));
            returnList.Add(new FieldDTO("Reporter", curIssue.Reporter));
            return returnList;
        }

        private Jira CreateRestClient(string token)
        {
            var credentialsDTO = JsonConvert.DeserializeObject<CredentialsDTO>(token);
            credentialsDTO.Domain = credentialsDTO.Domain.Replace("http://", "https://");
            if (!credentialsDTO.Domain.StartsWith("https://"))
            {
                credentialsDTO.Domain = "https://" + credentialsDTO.Domain;
            }
            return Jira.CreateRestClient(credentialsDTO.Domain, credentialsDTO.Username, credentialsDTO.Password);
        }
    }
}
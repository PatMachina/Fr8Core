﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using StructureMap;
using Utilities;

namespace Data.Repositories
{
    public class EnvelopeRepository : GenericRepository<EnvelopeDO>
    {
        private static readonly IConfigRepository _ConfigRepository = ObjectFactory.GetInstance<IConfigRepository>();
        public static readonly Dictionary<String, String> TemplateDescriptionMapping = new Dictionary<string, string>
        {
            { _ConfigRepository.Get("ForgotPassword_template"), "Forgot Password" }
        };

        public EnvelopeRepository(IUnitOfWork uow) : base(uow)
        {
        }

        public EnvelopeDO ConfigurePlainEmail(EmailDO email)
        {
            if (email == null)
                throw new ArgumentNullException("email");
            return ConfigureEnvelope(email, EnvelopeDO.MailHandler);
        }

        public EnvelopeDO ConfigureTemplatedEmail(EmailDO email, string templateName, IDictionary<string, object> mergeData = null)
        {
            if (mergeData == null)
                mergeData = new Dictionary<string, object>();
            if (email == null)
                throw new ArgumentNullException("email");
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentNullException("templateName", "Template name is null or empty.");

            return ConfigureEnvelope(email, EnvelopeDO.MailHandler, templateName, mergeData);
        }

        private EnvelopeDO ConfigureEnvelope(EmailDO email, string handler, string templateName = null, IDictionary<string, object> mergeData = null)
        {
            var envelope = new EnvelopeDO
            {
                TemplateName = templateName,
                Handler = handler
            };

            if (!String.IsNullOrEmpty(templateName) && TemplateDescriptionMapping.ContainsKey(templateName))
                envelope.TemplateDescription = @"This email was generated by the template '" + TemplateDescriptionMapping[templateName] + "' and was sent to '" + String.Join(", ", email.Recipients.Select(r => r.EmailAddress.ToDisplayName()));

            if (mergeData == null)
                mergeData = new Dictionary<string, object>();

            var baseUrls = new List<String>();
            const string baseUrlKey = "kwasantBaseURL";
            if (mergeData.ContainsKey(baseUrlKey))
            {
                var currentBaseURL = mergeData[baseUrlKey];
                var baseUrlList = currentBaseURL as List<String>;
                if (baseUrlList == null)
                    baseUrls = new List<string> { currentBaseURL as String };
                else
                    baseUrls = baseUrlList;
            }
            mergeData[baseUrlKey] = baseUrls;

            foreach (var pair in mergeData)
            {
                envelope.MergeData.Add(pair);
            }

            email.EmailStatus = EmailState.Queued;
            ((IMailerDO)envelope).Email = email;
            envelope.EmailID = email.Id;

            Add(envelope);
            return envelope;
        }
    }
}
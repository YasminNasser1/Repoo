using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace CorporateOnboardingAPIs.CRMWrapper
{
    public class CRMWrapper 
    {
        public IOrganizationService orgConnection;
        IServiceManagement<IOrganizationService> OrgServiceManagement;
        string serviceuri;
        public CRMWrapper(string URI, string UserName, string Password)
        {
            this.serviceuri = URI;
         
            if (!bool.Parse(System.Configuration.ConfigurationManager.AppSettings["CRMOAUTH20"].ToString()))
            {

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

                OrgServiceManagement = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(new Uri(serviceuri));

                ClientCredentials authcred = new ClientCredentials();
                authcred.UserName.UserName = UserName;
                authcred.UserName.Password = Password;
                // AuthenticationCredentials authcred = OrgServiceManagement.Authenticate(authcred);

                //OrganizationServiceProxy organizationProxy = new OrganizationServiceProxy(OrgServiceManagement, tokencredintials);

                OrganizationServiceProxy organizationProxy = new OrganizationServiceProxy(new Uri(this.serviceuri), null, authcred, null);
                organizationProxy.EnableProxyTypes();
                IOrganizationService service = (IOrganizationService)organizationProxy;
                this.orgConnection = service;
            }
            else
            {
                string connectionString = @"AuthType=ClientSecret;" +
                                            "Url=" + this.serviceuri + ";" +
                                            "ClientId=" + UserName + "; " + //username = Application (Client) ID
                                            "ClientSecret=" + Password; //password = Secret value
                CrmServiceClient _crmServiceClient = new CrmServiceClient(connectionString);
                this.orgConnection = (IOrganizationService)_crmServiceClient.OrganizationWebProxyClient != null ? (IOrganizationService)_crmServiceClient.OrganizationWebProxyClient : (IOrganizationService)_crmServiceClient.OrganizationServiceProxy;
            }


        }
        /* Filter example
          FilterExpression filter = new FilterExpression(LogicalOperator.Or);

            FilterExpression filter1 = new FilterExpression(LogicalOperator.And);
            filter1.Conditions.Add(new ConditionExpression("A_LogicalName", ConditionOperator.Equal, id1));
            filter1.Conditions.Add(new ConditionExpression("B_LogicalName", ConditionOperator.Equal, id2));

            FilterExpression filter2 = new FilterExpression(LogicalOperator.And);
            filter2.Conditions.Add(new ConditionExpression("B_LogicalName", ConditionOperator.Equal, id2));
            filter2.Conditions.Add(new ConditionExpression("C_LogicalName", ConditionOperator.Equal, id3));

            filter.AddFilter(filter1);
            filter.AddFilter(filter2);

            query.Criteria = filter;
        */
        public EntityCollection retrieveMultiple(string entity, string[] columns, FilterExpression filter)
        {
            QueryExpression query = new QueryExpression(entity);
            query.ColumnSet.AddColumns(columns);
            query.Criteria.AddFilter(filter);

            EntityCollection result1 = this.orgConnection.RetrieveMultiple(query);
            return result1;
        }
        public EntityCollection retrieveMultiple(string entity, string[] columns, FilterExpression filter, OrderExpression oe)
        {
            QueryExpression query = new QueryExpression(entity);
            query.ColumnSet.AddColumns(columns);
            query.Criteria.AddFilter(filter);
            query.Orders.Add(oe);
            EntityCollection result1 = this.orgConnection.RetrieveMultiple(query);
            return result1;
        }
        public EntityCollection retrieveMultiple(string entity, string[] columns, FilterExpression filter, OrderExpression oe, int toprecords)
        {
            QueryExpression query = new QueryExpression(entity);
            query.ColumnSet.AddColumns(columns);
            query.Criteria.AddFilter(filter);
            if (oe != null)
                query.Orders.Add(oe);
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = toprecords;
            query.PageInfo.PageNumber = 1;
            EntityCollection result1 = this.orgConnection.RetrieveMultiple(query);
            return result1;
        }
        public Entity RetrieveById(string strEntityLogicalName, Guid guidEntityId, string[] columns)

        {

            Entity RetrievedEntityById = this.orgConnection.Retrieve(strEntityLogicalName, guidEntityId, new ColumnSet(columns)); //it will retrieve the all attrributes

            return RetrievedEntityById;

        }
        public Guid CreateEntity(Entity entity)
        {


        //    if (entity == null)
        //    {
        //        throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
        //    }


        //    if (this.orgConnection == null)
        //    {
        //        throw new InvalidOperationException("OrganizationService is not initialized.");
        //    }

            return this.orgConnection.Create(entity);
        }


        public void DeleteEntity(Guid entityId, string entityname)

        {
            this.orgConnection.Delete(entityname, entityId);
        }
        public EntityCollection ExecuteFetchXML(string XMLQuery)

        {
            EntityCollection result = this.orgConnection.RetrieveMultiple(new FetchExpression(XMLQuery));
            return result;
        }
        public bool UpdateEntity(Entity entity)

        {
            try

            {
                this.orgConnection.Update(entity);
                return true;
            }
            catch (FaultException<OrganizationServiceFault> ex)

            {

                throw new InvalidPluginExecutionException(ex.Message);

            }

        }
        public ExecuteMultipleResponse Execute(ExecuteMultipleRequest multipleRequest)

        {
            try

            {

                return (ExecuteMultipleResponse)this.orgConnection.Execute(multipleRequest);
            }
            catch (FaultException<OrganizationServiceFault> ex)

            {

                throw new InvalidPluginExecutionException(ex.Message);

            }

        }
        //public Entity AuthenticateSystemUser(string username, string password)
        //{
        //    try
        //    {

        //        ClientCredentials authcred = new ClientCredentials();
        //        authcred.UserName.UserName = username;
        //        authcred.UserName.Password = password;



        //        OrganizationServiceProxy organizationProxy = new OrganizationServiceProxy(new Uri(this.serviceuri), null, authcred, null);
        //        // Get the user id
        //        Guid userid = ((WhoAmIResponse)organizationProxy.Execute(new WhoAmIRequest())).UserId;

        //        // Get the calendar id of the user
        //        Entity systemUserEntity = organizationProxy.Retrieve("systemuser", userid, new ColumnSet(new string[] { "cis_branchmanager", "firstname", "lastname", "fullname", "internalemailaddress", "mobilephone", "cis_averageservingtime", "cis_relatedbranch" }));
        //        return systemUserEntity;
        //    }
        //    catch (MessageSecurityException mse)
        //    {
        //        return null;
        //    }

        //}
        public EntityCollection GetManytoManyRelationShip(
            Guid entityBGuid,
            string offenceEntityName,
            string[] offenceColumnSet,
            string relationShipName,
            string legalEntityName,
            string legalColumnSet)
        {
            QueryExpression query = new QueryExpression(offenceEntityName);
            ColumnSet cols = new ColumnSet();
            for (int i = 0; i < offenceColumnSet.Length; i++)
            {
                cols.AddColumn(offenceColumnSet[i]);
            }

            query.ColumnSet = cols;

            Relationship relationship = new Relationship(relationShipName);
            RelationshipQueryCollection relationshipColl = new RelationshipQueryCollection();
            relationshipColl.Add(relationship, query);

            RetrieveRequest request = new RetrieveRequest();
            request.RelatedEntitiesQuery = relationshipColl;
            request.Target = new EntityReference(legalEntityName, entityBGuid);
            request.ColumnSet = new ColumnSet(legalColumnSet);
            RetrieveResponse response = (RetrieveResponse)this.orgConnection.Execute(request);

            return response.Entity.RelatedEntities[relationship];
        }

        public OptionMetadata[] getOptionSet(string entityname, string attributename)
        {
            var attributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityname,
                LogicalName = attributename,
                RetrieveAsIfPublished = true
            };

            var attributeResponse = (RetrieveAttributeResponse)this.orgConnection.Execute(attributeRequest);
            var attributeMetadata = (EnumAttributeMetadata)attributeResponse.AttributeMetadata;

            OptionMetadata[] optionList = attributeMetadata.OptionSet.Options.ToArray();
            return optionList;
        }

        public Guid CreateNoteAttachment(string subject, Guid entityId, string entityname, string filename, string data, string dmimetype)

        {
            Guid attachmentId = Guid.Empty;
            Entity note = new Entity("annotation");

            note["subject"] = subject;
            note["filename"] = filename;
            note["documentbody"] = data;
            note["mimetype"] = dmimetype;
            note["objectid"] = new EntityReference(entityname, entityId);
            attachmentId = this.orgConnection.Create(note);

            return attachmentId;
        }

        public Guid CreateNoteText(string subject, Guid userid, Guid entityId, string entityname, string notetext)

        {
            Guid attachmentId = Guid.Empty;
            Entity note = new Entity("annotation");

            note["createdby"] = new EntityReference("systemusers", Guid.Parse(userid.ToString()));
            note["subject"] = subject;
            note["notetext"] = notetext;
            note["objectid"] = new EntityReference(entityname, entityId);
            attachmentId = this.orgConnection.Create(note);

            return attachmentId;
        }

        public EntityCollection RetrieveNotes(Guid entityId)
        {
            EntityCollection results = null;
            QueryExpression _noteattachmentQuery = new QueryExpression
            {
                EntityName = "annotation",
                ColumnSet = new ColumnSet(
                    "subject",
                    "notetext",
                    "createdon"
                ),
                Criteria = new FilterExpression
                {
                    Conditions = {
                        new ConditionExpression
                        {
                            AttributeName = "objectid",
                            Operator = ConditionOperator.Equal,
                            Values = { entityId }
                        }
                    }

                }
            };
            results = this.orgConnection.RetrieveMultiple(_noteattachmentQuery);

            return results;

        }

        public EntityCollection RetrieveNoteAttachments(Guid entityId)
        {
            EntityCollection results = null;
            QueryExpression _noteattachmentQuery = new QueryExpression
            {
                EntityName = "annotation",
                ColumnSet = new ColumnSet(
                    "subject",
                    "filename",
                    "notetext",
                    "documentbody"
                ),
                Criteria = new FilterExpression
                {
                    Conditions = {
                        new ConditionExpression
                        {
                            AttributeName = "objectid",
                            Operator = ConditionOperator.Equal,
                            Values = { entityId }
                        },
                        new ConditionExpression
                        {
                            AttributeName = "isdocument",
                            Operator = ConditionOperator.Equal,
                            Values = { true }
                        }
                    }

                }
            };
            results = this.orgConnection.RetrieveMultiple(_noteattachmentQuery);

            return results;

        }
    }

}
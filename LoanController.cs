using CorporateOnboardingAPIs.CRMWrapper;
using Loan_Orginition.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Loan_Orginition.Controllers
{
    public class LoanController : ApiController
    {
        //        Define mapping for picklist values

        //var idTypeMapping = new Dictionary<string, int>
        //{
        //    { "NID", 1 },       // Replace with actual integer values from CRM
        //    { "Passport", 2 }   // Replace with actual integer values from CRM
        //};

        //// Handle cis_companytier picklist
        //var companyTypeMapping = new Dictionary<string, int>
        //{
        //    { "PrivateSector", 1 },
        //    { "GovernmentSector", 2 }
        //};

        public CRMWrapper _crmWrapper;

        public LoanController()
        {
            // Retrieve connection details from configuration
            string crmUri = ConfigurationManager.AppSettings["CRM_URI"];
            string username = ConfigurationManager.AppSettings["CRM_USERNAME"];
            string password = ConfigurationManager.AppSettings["CRM_PASSWORD"];


            // Initialize CRMWrapper with the connection details
            _crmWrapper = new CRMWrapper(crmUri, username, password);
        }     

        #region New Lead

        [HttpPost]
        [Route("createNewLead")]
        public IHttpActionResult AddCustomer(JObject Data)
        {
            try
            {
                Entity newCustomer = new Entity("lead");

                // Map fields from Customer model to CRM entity attributes
                newCustomer["cis_firstname"] = Data["FirstName"]?.ToString();
                newCustomer["cis_lastname"] = Data["LastName"]?.ToString();
                newCustomer["cis_mobilenumber"] = Data["Phone"]?.ToString();
                newCustomer["cis_email"] = Data["Email"]?.ToString();
                Guid customerId = _crmWrapper.CreateEntity(newCustomer);
                // Prepare the response object
                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
               {
                   { "CustomerId", customerId.ToString() },
                   { "FirstName", Data["FirstName"]?.ToString() },
                   { "LastName", Data["LastName"]?.ToString() }
               },
                    Massage = "The New Lead added successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }

        }
        #endregion

        #region Update Lead

        [HttpPut]
        [Route("updateLead/{id}")]
        public IHttpActionResult UpdateCustomer(Guid id, JObject Data)
        {
            try
            {
                // Retrieve the existing entity
                Entity existingCustomer = new Entity("lead") { Id = id };

                // Update entity attributes from the provided data
                if (Data["FirstName"] != null)
                    existingCustomer["cis_firstname"] = Data["FirstName"].ToString();

                if (Data["LastName"] != null)
                    existingCustomer["cis_lastname"] = Data["LastName"].ToString();

                if (Data["Phone"] != null)
                    existingCustomer["cis_mobilenumber"] = Data["Phone"].ToString();

                if (Data["Email"] != null)
                    existingCustomer["cis_email"] = Data["Email"].ToString();

                // Update the entity in CRM
                _crmWrapper.UpdateEntity(existingCustomer);

                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
                   {
                       { "CustomerId", id.ToString() },
                       { "FirstName", Data["FirstName"]?.ToString() },
                       { "LastName", Data["LastName"]?.ToString() }
                   },
                    Massage = "The lead was updated successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion

        #region Create Opportunity

        [HttpPost]
        [Route("createOpportunity")]
        public IHttpActionResult CreateOpportunity(JObject data)
        {
            try
            {
                // Extract customer details from the request
                string customerIdentifier = data["customerIdentifier"]?.ToString(); // Could be an email, phone, or another unique identifier
                string opportunityName = data["name"]?.ToString();

                // Check if the customer exists
                EntityCollection existingCustomer = _crmWrapper.retrieveMultiple("contact", new[] { "contactid", "accountid" },
                    new FilterExpression
                    {
                        Conditions = {
                    new ConditionExpression("emailaddress1", ConditionOperator.Equal, customerIdentifier)
                        }
                    });

                if (existingCustomer.Entities.Count > 0)
                {
                    // Customer exists, check if there is an associated account
                    Entity customer = existingCustomer.Entities[0];
                    Guid? accountId = customer.GetAttributeValue<EntityReference>("accountid")?.Id;

                    if (accountId.HasValue)
                    {
                        // Account exists, create an opportunity linked to the account
                        Entity opportunity = new Entity("opportunity")
                        {
                            ["name"] = opportunityName,
                            ["customerid"] = new EntityReference("loan", accountId.Value)
                        };

                        Guid opportunityId = _crmWrapper.CreateEntity(opportunity);

                        var response = new Response
                        {
                            Success = "true",
                            Result = new JObject
                    {
                        { "OpportunityId", opportunityId.ToString() },
                        { "AccountId", accountId.Value.ToString() }
                    },
                            Massage = "The opportunity was created successfully"
                        };

                        return Ok(response);
                    }
                    else
                    {
                        // No associated account, handle as needed
                        var response = new Response
                        {
                            Success = "false",
                            Result = null,
                            Massage = "No associated account found for the customer."
                        };

                        return BadRequest(response.Massage);
                    }
                }
                else
                {
                    // Customer does not exist, create a new lead
                    Entity lead = new Entity("lead");

                    // Map fields from Customer model to CRM entity attributes
                    lead["cis_firstname"] = data["FirstName"]?.ToString();
                    lead["cis_lastname"] = data["LastName"]?.ToString();
                    lead["cis_mobilenumber"] = data["Phone"]?.ToString();
                    lead["cis_email"] = data["Email"]?.ToString();
                    Guid leadId = _crmWrapper.CreateEntity(lead);

                    // Optionally, create an opportunity linked to the newly created lead
                    Entity opportunity = new Entity("opportunity")
                    {
                        ["name"] = opportunityName,
                        ["originatingleadid"] = new EntityReference("lead", leadId)
                    };

                    Guid opportunityId = _crmWrapper.CreateEntity(opportunity);

                    var response = new Response
                    {
                        Success = "true",
                        Result = new JObject
                {
                    { "OpportunityId", opportunityId.ToString() },
                    { "LeadId", leadId.ToString() }
                },
                        Massage = "The opportunity was created successfully and a new lead was created."
                    };

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion

        #region Create KYC Personal Info

        [HttpPost]
        [Route("createKYCPersonalInfo")]
        public IHttpActionResult CreateKYC(JObject data)
        {
            try
            {
                Entity kyc = new Entity("cis_knowyourcustomer");

                // Mapping fields
                kyc["cis_firstname"] = data["firstname"]?.ToString();
                kyc["cis_lastname"] = data["lastname"]?.ToString();

                if (DateTime.TryParse(data["cis_dateofbirth"]?.ToString(), out var dateOfBirth))
                {
                    kyc["cis_dateofbirth"] = dateOfBirth;
                }
                else
                {
                    return BadRequest("Invalid date format for cis_dateofbirth.");
                }

                if (DateTime.TryParse(data["cis_issuedate"]?.ToString(), out var issueDate))
                {
                    kyc["cis_issuedate"] = issueDate;
                }
                else
                {
                    return BadRequest("Invalid date format for cis_issuedate.");
                }

                if (DateTime.TryParse(data["cis_expirydate"]?.ToString(), out var expiryDate))
                {
                    kyc["cis_expirydate"] = expiryDate;
                }
                else
                {
                    return BadRequest("Invalid date format for cis_expirydate.");
                }

                // Handle picklist for ID Type

                if (data["cis_gender"] != null)
                {
                    var companyTierValue = data["cis_gender"] != null ? data["cis_gender"].ToString() : string.Empty;
                    if (companyTierValue != string.Empty)
                    {
                        kyc["cis_gender"] = new OptionSetValue(int.Parse(companyTierValue));
                    }
                    else
                    {
                        return BadRequest("Invalid value for cis_gender.");
                    }
                }


                // Handle City lookup
                var cityName = data["cis_city"]?.ToString();
                if (!string.IsNullOrEmpty(cityName))
                {
                    // Retrieve the city entity
                    var cityFilter = new FilterExpression(LogicalOperator.And);
                    cityFilter.AddCondition("cis_name", ConditionOperator.Equal, cityName);

                    var cityQuery = new QueryExpression("cis_city")
                    {
                        ColumnSet = new ColumnSet("cis_cityid"),
                        Criteria = cityFilter
                    };

                    var cityResults = _crmWrapper.retrieveMultiple("cis_city", new string[] { "cis_cityid" }, cityFilter);

                    if (cityResults.Entities.Count > 0)
                    {
                        var cityEntity = cityResults.Entities.First();
                        var cityId = cityEntity.Id;
                        kyc["cis_city"] = new EntityReference("cis_city", cityId); // Set the lookup field
                    }
                    else
                    {
                        return BadRequest("City not found.");
                    }
                }
                //        Define mapping for picklist values


                // Handle Country lookup
                var countryCode = data["cis_country"]?.ToString(); // Use country code from the request
                if (!string.IsNullOrEmpty(countryCode))
                {
                    // Retrieve the country entity based on the country code
                    var countryFilter = new FilterExpression(LogicalOperator.And);
                    countryFilter.AddCondition("cis_name", ConditionOperator.Equal, countryCode); // Make sure the condition is set on the correct field

                    var countryResults = _crmWrapper.retrieveMultiple("cis_country", new string[] { "cis_countryid" }, countryFilter);

                    if (countryResults.Entities.Count > 0)
                    {
                        var countryEntity = countryResults.Entities.First();
                        var countryId = countryEntity.Id;
                        kyc["cis_country"] = new EntityReference("cis_country", countryId); // Set the lookup field
                    }
                    else
                    {
                        return BadRequest("Country not found.");
                    }
                }




                // Handle picklist for ID Type

                if (data["cis_idtype"] != null)
                {
                    var companyTierValue = data["cis_idtype"] != null ? data["cis_idtype"].ToString() : string.Empty;
                    if (companyTierValue != string.Empty)
                    {
                        kyc["cis_idtype"] = new OptionSetValue(int.Parse(companyTierValue));
                    }
                    else
                    {
                        return BadRequest("Invalid value for cis_companytier.");
                    }
                }

                kyc["cis_mobilenumber"] = data["mobilenumber"]?.ToString() ?? string.Empty;
                kyc["cis_email"] = data["cis_email"]?.ToString();
                kyc["cis_legalid"] = data["cis_legalid"]?.ToString();
                kyc["cis_area"] = data["cis_area"]?.ToString();
                kyc["cis_street"] = data["cis_street"]?.ToString();

                // Create the KYC entity in CRM
                Guid kycId = _crmWrapper.CreateEntity(kyc);

                // Prepare the response object
                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
                    {
                        { "KYCId", kycId },
                    },
                    Massage = "KYC Personal Info created successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion

        #region KYC Employment Informations 

        [HttpPost]
        [Route("createKYCEmployment")]
        public IHttpActionResult CreateKYCEmployment(JObject data)
        {
            try
            {
                // Retrieve the KYC ID from the input data
                Guid kycId = data["id"]?.ToObject<Guid>() ?? Guid.Empty;

                if (kycId == Guid.Empty)
                {
                    return BadRequest("Invalid or missing KYC ID.");
                }

                // Retrieve the existing KYC entity using the KYC ID
                var existingKyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", kycId, null);

                // Define mappings for picklist values
                var currentWorkStatusMapping = new Dictionary<string, int>
                {
                    { "SelfEmployed", 1 },
                    { "Employee", 2 },
                    { "Retired", 3 }
                };

                var salaryTransferMapping = new Dictionary<string, int>
                {
                    { "Cash", 1 },
                    { "TransferredToBank", 2 },
                    { "TransferredToAnotherBank", 3 }
                };

                // Update the KYC entity with employment details
                existingKyc["cis_jobtitle"] = data["cis_jobtitle"]?.ToString();
                existingKyc["cis_salary"] = data["cis_salary"]?.ToObject<decimal>();

                // Employment Status Mapping
                if (data["cis_employeestatus"] != null)
                {
                    var employeestatusValue = data["cis_employeestatus"].ToString();
                    if (currentWorkStatusMapping.TryGetValue(employeestatusValue, out var statusOptionValue))
                    {
                        existingKyc["cis_employeestatus"] = new OptionSetValue(statusOptionValue);
                    }
                    else
                    {
                        return BadRequest("Invalid value for cis_employeestatus.");
                    }
                }

                // Salary Transfer Mapping
                if (data["cis_salarytransfer"] != null)
                {
                    var salaryTransferValue = data["cis_salarytransfer"].ToString();
                    if (salaryTransferMapping.TryGetValue(salaryTransferValue, out var transferOptionValue))
                    {
                        existingKyc["cis_salarytransfer"] = new OptionSetValue(transferOptionValue);
                    }
                    else
                    {
                        return BadRequest("Invalid value for cis_salarytransfer.");
                    }
                }

                // Update the KYC entity in CRM with employment details
                _crmWrapper.UpdateEntity(existingKyc);

                // Prepare the success response
                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
                    {
                        { "KYCId", kycId },
                    },
                    Massage = "KYC Employment information created successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion


        #region KYC Company Information

        [HttpPost]
        [Route("createKYCCompanyInfo")]
        public IHttpActionResult CreateKYCCompanyInfo(JObject data)
        {
            try
            {
                Guid kycId = data["id"]?.ToObject<Guid>() ?? Guid.Empty;

                if (kycId == Guid.Empty)
                {
                    return BadRequest("Invalid or missing KYC ID.");
                }

                var existingKyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", kycId, null);

                // Map fields from JObject to the KYC Company entity
                existingKyc["cis_companyaddress"] = data["companyAddress"]?.ToString();
                existingKyc["cis_monthsofbusiness"] = data["monthsOfBusiness"]?.ToObject<int>();

                // Define mapping for picklist values for CompanyType
                var companyTypeMapping = new Dictionary<string, int>
                {
                    { "PrivateSector", 1 },
                    { "GovernmentSector", 2 }
                };

                if (data["companyType"] != null)
                {
                    var companyTypeValue = data["companyType"].ToString();
                    if (companyTypeMapping.TryGetValue(companyTypeValue, out var companyTypeOptionValue))
                    {
                        existingKyc["cis_companytype"] = new OptionSetValue(companyTypeOptionValue);
                    }
                    else
                    {
                        return BadRequest("Invalid value for companyType.");
                    }
                }


                

                // Handle picklist for Company Tier


                if (data["cis_companytier"] != null)
                {
                    var companyTierValue = data["cis_companytier"] != null ? data["cis_companytier"].ToString() : string.Empty;
                    if (companyTierValue != string.Empty)
                    {
                        existingKyc["cis_companytier"] = new OptionSetValue(int.Parse(companyTierValue));
                    }
                    else
                    {
                        return BadRequest("Invalid value for cis_companytier.");
                    }
                }


                // Handle Company lookup
                var company = data["cis_company"]?.ToString();
                if (!string.IsNullOrEmpty(company))
                {
                    // Retrieve the city entity
                    var cityFilter = new FilterExpression(LogicalOperator.And);
                    cityFilter.AddCondition("cis_name", ConditionOperator.Equal, company);

                    var cityQuery = new QueryExpression("cis_company")
                    {
                        ColumnSet = new ColumnSet("cis_companyid"),
                        Criteria = cityFilter

                    };

                    var cityResults = _crmWrapper.retrieveMultiple("cis_company", new string[] { "cis_companyid" }, cityFilter);

                    if (cityResults.Entities.Count > 0)
                    {
                        var cityEntity = cityResults.Entities.First();
                        var cityId = cityEntity.Id;
                        existingKyc["cis_company"] = new EntityReference("cis_company", cityId); // Set the lookup field
                    }
                    else
                    {
                        return BadRequest("company not found.");
                    }
                }


                // Update the KYC entity in CRM with company information
                _crmWrapper.UpdateEntity(existingKyc);

                // Prepare the success response
                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
                    {
                        { "KYCId", kycId },

                    },
                    Massage = "KYC Company Information created successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion

        #region GET KYC

        [HttpGet]
        [Route("getKYC/{id}")]
        public IHttpActionResult GetKYC(Guid id)
        {
            try
            {
                // Define the columns you want to retrieve
                string[] columns =
                {
                    "cis_firstname",
                    "cis_lastname",
                    "cis_dateofbirth",
                    "cis_issuedate",
                    "cis_expirydate",
                    "cis_age",
                    "cis_gender",
                    "cis_mobilenumber",
                    "cis_email",
                    "cis_legalid",
                    "cis_area",
                    "cis_street",
                    "cis_jobtitle",
                    "cis_salary",
                    "cis_employeestatus",
                    "cis_salarytransfer",
                    "cis_companyaddress", // Add this field
                    "cis_monthsofbusiness", // Add this field
                    "cis_companytype" // Add this field
                };

                // Retrieve the KYC entity from CRM
                Entity kyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, columns);

                if (kyc == null)
                {
                    return NotFound();
                }

                // Map CRM entity attributes to a response object
                var kycData = new
                {
                    KYCId = kyc.Id,
                    FirstName = kyc.Contains("cis_firstname") ? kyc["cis_firstname"].ToString() : null,
                    LastName = kyc.Contains("cis_lastname") ? kyc["cis_lastname"].ToString() : null,
                    DateOfBirth = kyc.Contains("cis_dateofbirth") ? (DateTime?)kyc["cis_dateofbirth"] : null,
                    issuedate = kyc.Contains("cis_issuedate") ? (DateTime?)kyc["cis_issuedate"] : null,
                    expirydate = kyc.Contains("cis_expirydate") ? (DateTime?)kyc["cis_expirydate"] : null,
                    Gender = kyc.Contains("cis_gender") ?
                        (kyc["cis_gender"] is OptionSetValue optionSetValue ? optionSetValue.Value : (int?)null) :
                        (int?)null,
                    MobileNumber = kyc.Contains("cis_mobilenumber") ? kyc["cis_mobilenumber"].ToString() : null,
                    EmailAddress = kyc.Contains("cis_email") ? kyc["cis_email"].ToString() : null,
                    LegalId = kyc.Contains("cis_legalid") ? kyc["cis_legalid"].ToString() : null,
                    Area = kyc.Contains("cis_area") ? kyc["cis_area"].ToString() : null,
                    Street = kyc.Contains("cis_street") ? kyc["cis_street"].ToString() : null,
                    JobTitle = kyc.Contains("cis_jobtitle") ? kyc["cis_jobtitle"].ToString() : null,
                    Salary = kyc.Contains("cis_salary") ?
                        (decimal.TryParse(kyc["cis_salary"].ToString(), out var salaryValue) ? (int?)salaryValue : (int?)null) :
                        (int?)null,
                    EmploymentStatus = kyc.Contains("cis_employeestatus") ?
                        (kyc["cis_employeestatus"] is OptionSetValue optionSetValuee ? optionSetValuee.Value : (int?)null) :
                        (int?)null,
                    SalaryTransferMethod = kyc.Contains("cis_salarytransfer") ?
                        (kyc["cis_salarytransfer"] is OptionSetValue optionSetValueee ? optionSetValueee.Value : (int?)null) :
                        (int?)null,
                    CompanyAddress = kyc.Contains("cis_companyaddress") ? kyc["cis_companyaddress"].ToString() : null, // Add this field
                    MonthsOfBusiness = kyc.Contains("cis_monthsofbusiness") ? (int?)kyc["cis_monthsofbusiness"] : null, // Add this field
                    CompanyType = kyc.Contains("cis_companytype") ?
                (kyc["cis_companytype"] is OptionSetValue optionValue ? optionValue.Value : (int?)null) :
                (int?)null // Add this field
                };

                return Ok(kycData);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Internal server error: {ex.Message}"));
            }
        }

        #endregion


        #region GET KYC Personal Info

        [HttpGet]
        [Route("getKYCPersonalInfo/{id}")]
        public IHttpActionResult GetKYCPersonalInfo(Guid id)
        {
            try
            {
                // Define the columns you want to retrieve
                string[] columns =
                {
            "cis_firstname",
            "cis_lastname",
            "cis_dateofbirth",
            "cis_age",
            "cis_gender",
            "cis_mobilenumber",
            "cis_email",
            "cis_legalid",
            "cis_area",
            "cis_street",
            "cis_issuedate",
            "cis_expirydate",
            "cis_city",      // Lookup field for City
            "cis_country",   // Lookup field for Country
            "cis_idtype"     // OptionSet for ID Type
        };

                // Retrieve the KYC entity from CRM
                Entity kyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, columns);

                if (kyc == null)
                {
                    return NotFound();
                }

                // Retrieve the city name if the city lookup is populated
                string cityName = null;
                if (kyc.Contains("cis_city"))
                {
                    var cityRef = (EntityReference)kyc["cis_city"];
                    var cityEntity = _crmWrapper.RetrieveById(cityRef.LogicalName, cityRef.Id, new string[] { "cis_name" });
                    cityName = (bool)(cityEntity?.Contains("cis_name")) ? cityEntity["cis_name"].ToString() : null;
                }

                // Retrieve the country name if the country lookup is populated
                string countryName = null;
                if (kyc.Contains("cis_country"))
                {
                    var countryRef = (EntityReference)kyc["cis_country"];
                    var countryEntity = _crmWrapper.RetrieveById(countryRef.LogicalName, countryRef.Id, new string[] { "cis_name" });
                    countryName = (bool)(countryEntity?.Contains("cis_name")) ? countryEntity["cis_name"].ToString() : null;
                }

                // Get ID Type as an option set value
                int? idType = null;
                if (kyc.Contains("cis_idtype") && kyc["cis_idtype"] is OptionSetValue optionSet)
                {
                    idType = optionSet.Value;
                }

                // Map CRM entity attributes to a response object
                var kycData = new
                {
                    KYCId = kyc.Id,
                    FirstName = kyc.Contains("cis_firstname") ? kyc["cis_firstname"].ToString() : null,
                    LastName = kyc.Contains("cis_lastname") ? kyc["cis_lastname"].ToString() : null,
                    DateOfBirth = kyc.Contains("cis_dateofbirth") ? (DateTime?)kyc["cis_dateofbirth"] : null,
                    IssueDate = kyc.Contains("cis_issuedate") ? (DateTime?)kyc["cis_issuedate"] : null,
                    ExpiryDate = kyc.Contains("cis_expirydate") ? (DateTime?)kyc["cis_expirydate"] : null,
                    Gender = kyc.Contains("cis_gender") ?
                        (kyc["cis_gender"] is OptionSetValue genderValue ? genderValue.Value : (int?)null) :
                        (int?)null,
                    MobileNumber = kyc.Contains(attributeName: "cis_mobilenumber") ? kyc["cis_mobilenumber"].ToString() : null,
                    EmailAddress = kyc.Contains("cis_email") ? kyc["cis_email"].ToString() : null,
                    LegalId = kyc.Contains("cis_legalid") ? kyc["cis_legalid"].ToString() : null,
                    Area = kyc.Contains("cis_area") ? kyc["cis_area"].ToString() : null,
                    Street = kyc.Contains("cis_street") ? kyc["cis_street"].ToString() : null,
                    City = cityName, // Retrieved city name
                    Country = countryName, // Retrieved country name
                    IDType = idType // Option set value for ID Type
                };

                // Prepare the success response
                return Ok(kycData);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }
        #endregion


        #region Get KYC EmployeementINfo

        [HttpGet]
        [Route("GetKYCEmployeementINfo/{id}")]
        public IHttpActionResult GetKYCEmployeementINfo(Guid id)
        {
            try
            {
                // Define the columns you want to retrieve
                string[] columns =
                {
                    "cis_jobtitle",
                    "cis_salary",
                    "cis_employeestatus",
                    "cis_salarytransfer",
                };

                // Retrieve the KYC entity from CRM
                Entity kyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, columns);

                if (kyc == null)
                {
                    return NotFound();
                }
                // Map CRM entity attributes to a response object
                var kycData = new
                {
                    KYCId = kyc.Id,

                    JobTitle = kyc.Contains("cis_jobtitle") ? kyc["cis_jobtitle"].ToString() : null,
                    Salary = kyc.Contains("cis_salary") ? ((Money)kyc["cis_salary"]).Value : (decimal?)null,
                    EmploymentStatus = kyc.Contains("cis_employeestatus") ?
                        (kyc["cis_employeestatus"] is OptionSetValue optionSetValuee ? optionSetValuee.Value : (int?)null) :
                        (int?)null,
                    SalaryTransferMethod = kyc.Contains("cis_salarytransfer") ?
                        (kyc["cis_salarytransfer"] is OptionSetValue optionSetValueee ? optionSetValueee.Value : (int?)null) :
                        (int?)null

                };

                return Ok(kycData);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion


        #region Get KYC CompanyInfo

        [HttpGet]
        [Route("getKYCCompanyInfo/{id}")]
        public IHttpActionResult GetKYCCompanyInfo(Guid id)

        {
            try
            {
                // Define the columns you want to retrieve
                string[] columns =
                {
                    
                    "cis_companyaddress", // Add this field
                    "cis_monthsofbusiness", // Add this field
                    "cis_companytype" ,// Add this field
                    "cis_company"
                };

                // Retrieve the KYC entity from CRM
                Entity kyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, columns);

                if (kyc == null)
                {
                    return NotFound();
                }
                // Retrieve the company name if the company lookup is populated
                string companyName = null;
                if (kyc.Contains("cis_company"))
                {
                    var companyRef = (EntityReference)kyc["cis_company"];
                    var companyEntity = _crmWrapper.RetrieveById(companyRef.LogicalName, companyRef.Id, new string[] { "cis_name" });
                    companyName = (bool)(companyEntity?.Contains("cis_name")) ? companyEntity["cis_name"].ToString() : null;
                }
                // Map CRM entity attributes to a response object
                var kycData = new
                {
                    KYCId = kyc.Id,
                    CompanyAddress = kyc.Contains("cis_companyaddress") ? kyc["cis_companyaddress"].ToString() : null, // Add this field
                    MonthsOfBusiness = kyc.Contains("cis_monthsofbusiness") ? (int?)kyc["cis_monthsofbusiness"] : null, // Add this field
                    CompanyType = kyc.Contains("cis_companytype") ?
                (kyc["cis_companytype"] is OptionSetValue optionValue ? optionValue.Value : (int?)null) :
                (int?)null, // Add this field
                     CompanyName = companyName // Retrieved company name from the lookup

                };

                return Ok(kycData);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Internal server error: {ex.Message}"));
            }
        }
        #endregion


        #region updateKYCPersonalInfo

        [HttpPut]
        [Route("updateKYCPersonalInfo/{id}")]
        public IHttpActionResult UpdateKYCPersonalInfo(Guid id, [FromBody] JObject kycData)
        {
            try
            {
                // Retrieve the existing KYC entity from CRM
                Entity kyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, new string[]
                {
                     "cis_firstname", "cis_lastname", "cis_dateofbirth", "cis_age",
                     "cis_gender", "cis_mobilenumber", "cis_email",
                     "cis_legalid", "cis_area", "cis_street",
                     "cis_issuedate", "cis_expirydate", "cis_city", "cis_country", "cis_idtype"
                });

                if (kyc == null)
                {
                    return NotFound(); // Return 404 if KYC not found
                }

                // Update entity fields with new values from kycData
                if (kycData.ContainsKey("FirstName"))
                    kyc["cis_firstname"] = kycData["FirstName"].ToString();

                if (kycData.ContainsKey("LastName"))
                    kyc["cis_lastname"] = kycData["LastName"].ToString();

                if (kycData.ContainsKey("DateOfBirth"))
                    kyc["cis_dateofbirth"] = DateTime.Parse(kycData["DateOfBirth"].ToString());

                if (kycData.ContainsKey("IssueDate"))
                    kyc["cis_issuedate"] = DateTime.Parse(kycData["IssueDate"].ToString());

                if (kycData.ContainsKey("ExpiryDate"))
                    kyc["cis_expirydate"] = DateTime.Parse(kycData["ExpiryDate"].ToString());

                if (kycData.ContainsKey("Gender"))
                    kyc["cis_gender"] = new OptionSetValue(Convert.ToInt32(kycData["Gender"]));

                if (kycData.ContainsKey("MobileNumber"))
                    kyc["cis_mobilenumber"] = kycData["MobileNumber"].ToString();

                if (kycData.ContainsKey("EmailAddress"))
                    kyc["cis_email"] = kycData["EmailAddress"].ToString();

                if (kycData.ContainsKey("LegalId"))
                    kyc["cis_legalid"] = kycData["LegalId"].ToString();

                if (kycData.ContainsKey("Area"))
                    kyc["cis_area"] = kycData["Area"].ToString();

                if (kycData.ContainsKey("Street"))
                    kyc["cis_street"] = kycData["Street"].ToString();

                // Handle City lookup
                if (kycData.ContainsKey("City"))
                {
                    var cityName = kycData["City"].ToString();
                    if (!string.IsNullOrEmpty(cityName))
                    {
                        var cityFilter = new FilterExpression(LogicalOperator.And);
                        cityFilter.AddCondition("cis_name", ConditionOperator.Equal, cityName);

                        var cityResults = _crmWrapper.retrieveMultiple("cis_city", new string[] { "cis_cityid" }, cityFilter);

                        if (cityResults.Entities.Count > 0)
                        {
                            var cityEntity = cityResults.Entities.First();
                            var cityId = cityEntity.Id;
                            kyc["cis_city"] = new EntityReference("cis_city", cityId); // Set the lookup field
                        }
                        else
                        {
                            return BadRequest("City not found.");
                        }
                    }
                }

                // Handle Country lookup
                if (kycData.ContainsKey("Country"))
                {
                    var countryCode = kycData["Country"].ToString();
                    if (!string.IsNullOrEmpty(countryCode))
                    {
                        var countryFilter = new FilterExpression(LogicalOperator.And);
                        countryFilter.AddCondition("cis_name", ConditionOperator.Equal, countryCode);

                        var countryResults = _crmWrapper.retrieveMultiple("cis_country", new string[] { "cis_countryid" }, countryFilter);

                        if (countryResults.Entities.Count > 0)
                        {
                            var countryEntity = countryResults.Entities.First();
                            var countryId = countryEntity.Id;
                            kyc["cis_country"] = new EntityReference("cis_country", countryId); // Set the lookup field
                        }
                        else
                        {
                            return BadRequest("Country not found.");
                        }
                    }
                }

                // Handle ID Type picklist
                if (kycData.ContainsKey("IDType"))
                {
                    var idTypeValue = kycData["IDType"].ToString();
                    if (!string.IsNullOrEmpty(idTypeValue))
                    {
                        kyc["cis_idtype"] = new OptionSetValue(Convert.ToInt32(idTypeValue));
                    }
                    else
                    {
                        return BadRequest("Invalid value for IDType.");
                    }
                }

                // Update the entity in CRM
                _crmWrapper.UpdateEntity(kyc);

                // Prepare success response
                var response = new Response
                {
                    Success = "true",
                    Result = kycData,
                    Massage = "KYC personal information updated successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Failed to update KYC personal information: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion updateKYCPersonalInfo


        #region Update KYC EmploymentInfo

        [HttpPut]
        [Route("updateKYCEmploymentInfo/{id}")]
        public IHttpActionResult UpdateKYCEmploymentInfo(Guid id, [FromBody] JObject kycData)
        {
            try
            {
                // Retrieve the existing KYC entity from CRM
                Entity kyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, new string[]
                {
            "cis_jobtitle", "cis_salary", "cis_employeestatus", "cis_salarytransfer"
                });

                if (kyc == null)
                {
                    return NotFound(); // Return 404 if KYC not found
                }

                // Update the entity with new values from kycData
                if (kycData.ContainsKey("JobTitle"))
                    kyc["cis_jobtitle"] = kycData["JobTitle"].ToString();

                if (kycData.ContainsKey("Salary"))
                    kyc["cis_salary"] = Convert.ToDecimal(kycData["Salary"]);

                if (kycData.ContainsKey("EmploymentStatus"))
                    kyc["cis_employeestatus"] = new OptionSetValue(Convert.ToInt32(kycData["EmploymentStatus"]));

                if (kycData.ContainsKey("SalaryTransferMethod"))
                    kyc["cis_salarytransfer"] = new OptionSetValue(Convert.ToInt32(kycData["SalaryTransferMethod"]));

                // Update the entity in CRM
                _crmWrapper.UpdateEntity(kyc);

                // Prepare successful response
                var response = new Response
                {
                    Success = "true",
                    Result = kycData,
                    Massage = "KYC employment information updated successfully."
                };

                // Prepare success response
                var responsee = new Response
                {
                    Success = "true",
                    Result = kycData,
                    Massage = "KYC Employment information updated successfully."
                };
                return Ok (responsee);
            }
            catch (Exception ex)
            {
                // Prepare error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Failed to update KYC employment information: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion


        #region updateKYCCompanyInfo

        [HttpPut]
        [Route("updateKYCCompanyInfo/{id}")]
        public IHttpActionResult UpdateKYCCompanyInfo(Guid id, [FromBody] JObject data)
        {
            try
            {
                // Retrieve the existing KYC entity from CRM
                Entity existingKyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", id, new string[]
                {
            "cis_companyaddress", "cis_monthsofbusiness", "cis_companytype",
            "cis_companytier", "cis_company"
                });

                if (existingKyc == null)
                {
                    return NotFound(); // Return 404 if KYC not found
                }

                // Update entity fields with new values from data
                if (data["companyAddress"] != null)
                    existingKyc["cis_companyaddress"] = data["companyAddress"].ToString();

                if (data["monthsOfBusiness"] != null)
                    existingKyc["cis_monthsofbusiness"] = data["monthsOfBusiness"].ToObject<int>();

                // Define mapping for picklist values for CompanyType
                var companyTypeMapping = new Dictionary<string, int>
        {
            { "PrivateSector", 1 },
            { "GovernmentSector", 2 }
        };

                if (data["companyType"] != null)
                {
                    var companyTypeValue = data["companyType"].ToString();
                    if (companyTypeMapping.TryGetValue(companyTypeValue, out var companyTypeOptionValue))
                    {
                        existingKyc["cis_companytype"] = new OptionSetValue(companyTypeOptionValue);
                    }
                    else
                    {
                        return BadRequest("Invalid value for companyType.");
                    }
                }

                // Handle picklist for Company Tier
                if (data["companyTier"] != null)
                {
                    var companyTierValue = data["companyTier"].ToString();
                    if (!string.IsNullOrEmpty(companyTierValue))
                    {
                        existingKyc["cis_companytier"] = new OptionSetValue(int.Parse(companyTierValue));
                    }
                    else
                    {
                        return BadRequest("Invalid value for companyTier.");
                    }
                }

                // Handle Company lookup
                var company = data["company"]?.ToString();
                if (!string.IsNullOrEmpty(company))
                {
                    var companyFilter = new FilterExpression(LogicalOperator.And);
                    companyFilter.AddCondition("cis_name", ConditionOperator.Equal, company);

                    var companyQuery = new QueryExpression("cis_company")
                    {
                        ColumnSet = new ColumnSet("cis_companyid"),
                        Criteria = companyFilter
                    };

                    var companyResults = _crmWrapper.retrieveMultiple("cis_company", new string[] { "cis_companyid" }, companyFilter);

                    if (companyResults.Entities.Count > 0)
                    {
                        var companyEntity = companyResults.Entities.First();
                        var companyId = companyEntity.Id;
                        existingKyc["cis_company"] = new EntityReference("cis_company", companyId); // Set the lookup field
                    }
                    else
                    {
                        return BadRequest("Company not found.");
                    }
                }

                // Update the KYC entity in CRM with company information
                _crmWrapper.UpdateEntity(existingKyc);

                // Prepare the success response
                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
            {
                { "KYCId", id }
            },
                    Massage = "KYC Company Information updated successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion updateKYCCompanyInfo


        #region KYC Additional Information

        [HttpPost]
        [Route("createKYCAdditionalInfo")]
        public IHttpActionResult CreateKYCAdditionalInfo(JObject data)
        {
            try
            {
                Guid kycId = data["id"]?.ToObject<Guid>() ?? Guid.Empty;

                if (kycId == Guid.Empty)
                {
                    return BadRequest("Invalid or missing KYC ID.");
                }

                var existingKyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", kycId, null);

                // Handle picklist for ClubTier
                if (data["cis_clubtier"] != null)
                {
                    var clubTierValue = data["cis_clubtier"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(clubTierValue))
                    {
                        existingKyc["cis_clubtier"] = new OptionSetValue(int.Parse(clubTierValue));
                    }
                    else
                    {
                        return BadRequest("Invalid value for cis_clubtier.");
                    }
                }

                //// Check if user has a club category
                //var hasClubCategory = data["hasClubCategory"]?.ToString().ToLower() == "y";

                //if (hasClubCategory)
                //{



                //    // Handle Club Category lookup With CRM team

                //    var clubCategoryName = data["cis_clubcategory"]?.ToString(); // Use club category name from the request

                //    if (!string.IsNullOrEmpty(clubCategoryName))
                //    {
                //        // Retrieve the club category entity based on its name
                //        var clubCategoryFilter = new FilterExpression(LogicalOperator.And);
                //        clubCategoryFilter.AddCondition("cis_name", ConditionOperator.Equal, clubCategoryName); // Assuming the field storing the club name is cis_name

                //        // Retrieve the club category entity (using the correct entity name)
                //        var clubCategoryResults = _crmWrapper.retrieveMultiple("cis_club", new string[] { "cis_clubid" }, clubCategoryFilter);

                //        if (clubCategoryResults.Entities.Count > 0)
                //        {
                //            var clubCategoryEntity = clubCategoryResults.Entities.First();
                //            var clubCategoryId = clubCategoryEntity.Id;
                //            // Set the lookup field (using the correct entity name)
                //            existingKyc["cis_club"] = new EntityReference("cis_clubcategory", clubCategoryId); // Set the lookup field
                //        }
                //        else
                //        {
                //            return BadRequest("Club category not found.");
                //        }
                //    }
                //    else
                //    {
                //        return BadRequest("Club category name is missing.");
                //    }

                //}
                //else
                //{
                //    // Clear the club category field if the user does not have a club category
                //    existingKyc["cis_clubcategory"] = null;
                //}

                // Update the KYC entity in CRM with additional information


                _crmWrapper.UpdateEntity(existingKyc);

                // Prepare the success response
                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
                    {
                        { "KYCId", kycId }
                    },
                    Massage = "KYC Additional Information created successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion


        #region Upload Flie

        [HttpPost]
        [Route("uploadBankStatement")]
        public IHttpActionResult UploadBankStatement()
        {
            try
            {

                var httpRequest = HttpContext.Current.Request;

                // Retrieve the KYC ID from the form data
                Guid kycId = Guid.Empty;
                if (!string.IsNullOrEmpty(httpRequest.Form["id"]))
                {
                    kycId = Guid.Parse(httpRequest.Form["id"]);
                }

                if (kycId == Guid.Empty)
                {
                    return BadRequest("Invalid or missing KYC ID.");
                }

                // Retrieve the existing KYC entity using the KYC ID
                var existingKyc = _crmWrapper.RetrieveById("cis_knowyourcustomer", kycId, null);

                // Check if a file was uploaded
                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded.");

                // Get the uploaded file
                var postedFile = httpRequest.Files[0];

                // Check the file type
                var supportedTypes = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(postedFile.FileName).ToLower();

                if (!supportedTypes.Contains(fileExtension))
                    return BadRequest("Unsupported file type. Only PDF, JPG, and PNG are allowed.");

                // Convert file to base64 string
                using (var memoryStream = new MemoryStream())
                {
                    postedFile.InputStream.CopyTo(memoryStream);
                    var fileContent = memoryStream.ToArray();
                    var base64FileContent = Convert.ToBase64String(fileContent);

                    // Create an annotation (note) in CRM
                    var annotation = new Entity("annotation")
                    {
                        ["subject"] = "Bank Statement",
                        ["filename"] = postedFile.FileName,
                        ["mimetype"] = postedFile.ContentType,
                        ["documentbody"] = base64FileContent,
                        ["objectid"] = new EntityReference("cis_knowyourcustomer", kycId),
                        ["objecttypecode"] = "cis_knowyourcustomer"
                    };

                    _crmWrapper.CreateEntity(annotation);
                }

                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
                    {
                        { "KYCId", kycId },
                    },
                    Massage = "KYC File Uploaded successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion

        #region Update File

        [HttpPost]
        [Route("updateBankStatement")]
        public IHttpActionResult UpdateBankStatement()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // Retrieve the KYC ID from the form data
                Guid kycId = Guid.Empty;
                if (!string.IsNullOrEmpty(httpRequest.Form["id"]))
                {
                    kycId = Guid.Parse(httpRequest.Form["id"]);
                }

                if (kycId == Guid.Empty)
                {
                    return BadRequest("Invalid or missing KYC ID.");
                }

                // Check if a file was uploaded
                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded.");

                // Get the uploaded file
                var postedFile = httpRequest.Files[0];

                // Check the file type
                var supportedTypes = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(postedFile.FileName).ToLower();

                if (!supportedTypes.Contains(fileExtension))
                    return BadRequest("Unsupported file type. Only PDF, JPG, and PNG are allowed.");

                // Convert file to base64 string
                string base64FileContent;
                using (var memoryStream = new MemoryStream())
                {
                    postedFile.InputStream.CopyTo(memoryStream);
                    var fileContent = memoryStream.ToArray();
                    base64FileContent = Convert.ToBase64String(fileContent);
                }

                // Define filter to check for existing annotation
                var filter = new FilterExpression(LogicalOperator.And);
                filter.AddCondition("objectid", ConditionOperator.Equal, kycId);
                filter.AddCondition("subject", ConditionOperator.Equal, "Bank Statement");

                // Retrieve existing annotations
                var existingAnnotations = _crmWrapper.retrieveMultiple("annotation", new[] { "annotationid" }, filter);

                if (existingAnnotations.Entities.Count == 0)
                {
                    return NotFound(); // Return 404 if no existing annotation is found
                }

                // Update existing annotation
                var existingAnnotationId = existingAnnotations.Entities.First().Id;
                var annotation = new Entity("annotation", existingAnnotationId)
                {
                    ["filename"] = postedFile.FileName,
                    ["mimetype"] = postedFile.ContentType,
                    ["documentbody"] = base64FileContent
                };

                _crmWrapper.UpdateEntity(annotation);

                var response = new Response
                {
                    Success = "true",
                    Result = new JObject
            {
                { "KYCId", kycId },
            },
                    Massage = "KYC Bank Statement updated successfully."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Prepare the error response
                var errorResponse = new Response
                {
                    Success = "false",
                    Result = null,
                    Massage = $"Internal server error: {ex.Message}"
                };

                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        #endregion


    }

}









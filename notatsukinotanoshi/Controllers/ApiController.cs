﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using notatsukinotanoshi.Models.Utilities;
using notatsukinotanoshi.ViewModels.Home;
using Microsoft.AspNetCore.Localization;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using notatsukinotanoshi.Localizers;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace notatsukinotanoshi.Controllers
{
    public class ApiController : Controller
    {
        private readonly string connectionString;
        private readonly IStringLocalizer<ApiController> _localizer;
        private readonly IStringLocalizer<CompanyName> _companyName;

        public ApiController(IStringLocalizer<ApiController> localizer, IStringLocalizer<CompanyName> companyName, IConfiguration config)
        {
            _localizer = localizer;
            _companyName = companyName;
            connectionString = config.GetValue<string>("ConnectionStrings:DefaultConnection"); //MySQL settings
        }

        /// <summary>
        /// Generate the mail
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generate(EmailSubmitViewModel model)
        {
            var response = new ResponseAPI();

            //Get requested culture
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            string[] supportedCultures = { "en", "ja" };
            if (!supportedCultures.Contains(culture))
            {
                culture = "en";
            }

            var msg = "";
            var companyName = "";
            var companyMail = "";
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    //Get a random template
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT text_body FROM email_templates et WHERE et.locale = @locale AND approved = true ORDER BY RAND() LIMIT 1";
                    cmd.Parameters.AddWithValue("@locale", culture);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        msg = reader.GetString(0);
                    }
                    reader.Close();

                    //Get the target company info
                    cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name, email FROM company_info WHERE active = true LIMIT 1";
                    cmd.Parameters.AddWithValue("@locale", culture);
                    reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        companyName = _companyName[reader.GetString(0)];
                        companyMail = reader.GetString(1);
                    }
                    reader.Close();

                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    //Close the connection
                    conn.Close();
                }
            };

            var returnData = new Dictionary<string, string>
            {
                { "template", msg },
                { "email", companyMail }
            };

            response.Status = ResponseState.Success;
            response.Message = "Get template successfully";
            response.ReturnData = returnData;
            return Json(response);
        }
    }
}
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MyInnovationFunApp1
{
    public static class Function1
    {
        [FunctionName("CallAPI-KD")]//http://localhost:7279/api/CallAPI
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "CallAPI-KD")] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                String valueAPIURL_AddDeptKD = System.Environment.GetEnvironmentVariable("APIURL_AddDeptKD", EnvironmentVariableTarget.Process);
                String valueAPIURL_GetDeptKD = System.Environment.GetEnvironmentVariable("APIURL_GetDeptKD", EnvironmentVariableTarget.Process);
                


                using (HttpClient newClientPost = new HttpClient())
                {
                    // Call Khalid Sir's API - To AddDepartment
                      Department department = new Department
                    {
                        DepartmentName = "Accounts"
                    };

                    string jsonContent = JsonConvert.SerializeObject(department);

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Send POST request
                    HttpResponseMessage Postresponse = await newClientPost.PostAsync(valueAPIURL_AddDeptKD, content);

                    // Check if request was successful
                    if (Postresponse.IsSuccessStatusCode)
                    {
                        string PostresponseContent = await Postresponse.Content.ReadAsStringAsync();
                        Console.WriteLine("Department added successfully");
                        Console.WriteLine(PostresponseContent);
                    }
                    else
                    {
                        Console.WriteLine($"Error: {Postresponse.StatusCode}");
                        string errorContent = await Postresponse.Content.ReadAsStringAsync();
                        Console.WriteLine(errorContent);
                    }

                }

                using (HttpClient newClientKD = new HttpClient())
                {
                    // Call Khalid Sir's API - To Get All Departments
                    HttpResponseMessage responseKD = await newClientKD.GetAsync(valueAPIURL_GetDeptKD);


                    if (responseKD.IsSuccessStatusCode)
                    {
                        string responseContentKD = await responseKD.Content.ReadAsStringAsync();
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(responseContentKD, Encoding.UTF8, "application/json")
                        };
                    }

                }

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("Department added successfully")
                };
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred: {ex.Message}");
                return req.CreateResponse(System.Net.HttpStatusCode.InternalServerError, "An error occurred while processing the request.");
            }
        }

    }

    public static class Function2
    {
        [FunctionName("TakeBackUpOnValidMPin")]// http://localhost:7279/api/TakeBackUpOnValidMPin?MpnID=Afiya12345
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "TakeBackUpOnValidMPin")] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                // Extract MpnID from the query parameters
                string mpnId = req.RequestUri.ParseQueryString()["MpnID"];
                //Extract parameter from Localsettings.json

                String valueAPIURL_MPIN = System.Environment.GetEnvironmentVariable("APIURL_MPIN", EnvironmentVariableTarget.Process);
                String valueDBURL = System.Environment.GetEnvironmentVariable("DBURL", EnvironmentVariableTarget.Process);
                String valuesourceDirectory = System.Environment.GetEnvironmentVariable("sourceDirectory", EnvironmentVariableTarget.Process);
                String valuebackupDirectory = System.Environment.GetEnvironmentVariable("backupDirectory", EnvironmentVariableTarget.Process);


                if (string.IsNullOrEmpty(mpnId))
                {
                    return req.CreateResponse(System.Net.HttpStatusCode.BadRequest, "Please provide a valid partner Mpn Id!");
                }

                using (HttpClient newClient = new HttpClient())
                {
                    HttpResponseMessage response = await newClient.GetAsync(valueAPIURL_MPIN+ "?mpnId="+ mpnId);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Taking Backup once Mpin is Valid
                        string[] files = Directory.GetFiles(valuesourceDirectory);

                        // Copy each file to the backup directory
                        foreach (string filePath in files)
                        {
                            string fileName = Path.GetFileName(filePath);
                            string destinationPath = Path.Combine(valuebackupDirectory, fileName);

                            File.Copy(filePath, destinationPath, true);
                        }

                        // Insert into DB
                        string connectionString = valueDBURL;
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string insertQuery = "INSERT INTO MpinBackupTable (mpin, backupdatetime) VALUES (@Mpin, @BackupDateTime)";
                            SqlCommand command = new SqlCommand(insertQuery, connection);
                            command.Parameters.AddWithValue("@Mpin", mpnId);
                            command.Parameters.AddWithValue("@BackupDateTime", DateTime.UtcNow);
                            await command.ExecuteNonQueryAsync();
                        }

                        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                        {
                            Content = new StringContent(responseContent + "Mpin Successfully Validated , Backup Process done corresponding enteries successfully saved in  database")
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(responseContent)
                        };
                    }

                }
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred: {ex.Message}");
                return req.CreateResponse(System.Net.HttpStatusCode.InternalServerError, "An error occurred while processing the request.");
            }
        }
    }
}


public class PartnerMpnModel
{
    public string MpnID { get; set; }
}


public class PartnerMpnResponseModel
{
    public bool isValidMpn { get; set; }
}


public class Department
{
    
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; }
}


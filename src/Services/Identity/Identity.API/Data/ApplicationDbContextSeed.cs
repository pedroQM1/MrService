using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Identity.API.Extensions;
using Identity.API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.API.Data
{
    public class ApplicationDbContextSeed
    {
         private readonly IPasswordHasher<ApplicationUser> _passwordHasher = new PasswordHasher<ApplicationUser>();

         public async Task SeedAsync(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<ApplicationDbContextSeed> logger,
            IOptions<AppSettings> settings,
            int? retry = 0
        ){
            int retryForAvaiablility = retry.Value;

            try
            {
                var userCustomizationData = settings.Value.UseCustomizationData;
                var contentRootPath = env.ContentRootPath;
                var webroot = env.WebRootPath;

                if(!context.Users.Any()){
                    context.Users.AddRange(userCustomizationData 
                    ? GetUserFromFile(contentRootPath,logger)
                    : GetDefaultUser());

                    await context.SaveChangesAsync();
                }
                if(userCustomizationData){
                    GetPreconfigureImages(contentRootPath,webroot,logger);
                }

               
            }
            catch (System.Exception ex)
            {
                
                if(retryForAvaiablility < 10){
                    retryForAvaiablility++;
                    logger.LogError(ex, "EXCEPTION ERROR while migrating {DbContextName}", nameof(ApplicationDbContext));
                    await SeedAsync(context, env, logger, settings, retryForAvaiablility);
                }
            }    
        }
        private IEnumerable<ApplicationUser> GetUserFromFile(string contentRootPath,ILogger logger){

            string csvFileUsers = Path.Combine(contentRootPath,"Setup","Users.csv");

            if(!File.Exists(csvFileUsers))return GetDefaultUser();

            string[] csvheaders;

            try
            {
                string[] requiredHeaders = {
                    "normalizedemail", "normalizedusername", "password","phonenumber","email","username"
                };
                csvheaders = GetHeaders(requiredHeaders,csvFileUsers);
            }
            catch (System.Exception ex)
            {
                
                logger.LogError(ex, "EXCEPTION ERROR: {Message}", ex.Message);
                return GetDefaultUser();
            }

            return File.ReadAllLines(csvFileUsers)
                        .Skip(1)
                        .Select(row=>Regex.Split(row,"(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"))
                        .SelectTry(colum=>CreateApplicationUser(colum,csvheaders))
                        .OnCaughtException(ex=>{logger.LogError(ex,"EXCEPTION ERROR: {message}",ex.Message);return null;})
                        .Where(x=>x != null)
                        .ToList();

        }
         private IEnumerable<ApplicationUser> GetDefaultUser()
        {
            var user =
            new ApplicationUser()
            {
                Id = Guid.NewGuid().ToString(),
                Email = "demouser@microsoft.com",
                UserName = "demouser@microsoft.com",
                NormalizedEmail = "DEMOUSER@MICROSOFT.COM",
                NormalizedUserName = "DEMOUSER@MICROSOFT.COM",
                PhoneNumber = "1234567890",
                SecurityStamp = Guid.NewGuid().ToString("D"),
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, "Pass@word1");
            return new List<ApplicationUser>()
            {
                user
            };
        }
        static string[] GetHeaders(string[] requiredHeaders,string csvfile){

            string [] csvheaders = File.ReadAllLines(csvfile).First().ToLowerInvariant().Split(',');

            if(csvheaders.Count() != requiredHeaders.Count())
                throw new Exception($"requiredHeader count '{ requiredHeaders.Count()}' is different then read header '{csvheaders.Count()}'");

            foreach (var requiredHeader in requiredHeaders)
            {
                if(!csvheaders.Contains(requiredHeader))
                    throw new Exception($"does not contain required header '{requiredHeader}'");
            }
            return csvheaders;
        }
        private ApplicationUser CreateApplicationUser(string[] column, string[] headers)
        {
            if (column.Count() != headers.Count())
            {
                throw new Exception($"column count '{column.Count()}' not the same as headers count'{headers.Count()}'");
            }

            var user = new ApplicationUser
            {
                Email = column[Array.IndexOf(headers, "email")].Trim('"').Trim(),
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = column[Array.IndexOf(headers, "phonenumber")].Trim('"').Trim(),
                UserName = column[Array.IndexOf(headers, "username")].Trim('"').Trim(),
                NormalizedEmail = column[Array.IndexOf(headers, "normalizedemail")].Trim('"').Trim(),
                NormalizedUserName = column[Array.IndexOf(headers, "normalizedusername")].Trim('"').Trim(),
                SecurityStamp = Guid.NewGuid().ToString("D"),
                PasswordHash = column[Array.IndexOf(headers, "password")].Trim('"').Trim(), // Note: This is the password
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            return user;
        }
        static void GetPreconfigureImages(string contentRootPath, string webroot,ILogger logger){

            try
            {
                string imagesZipFile = Path.Combine(contentRootPath,"Setup","images.zip");
                if(!File.Exists(imagesZipFile)){
                    logger.LogError("Zip file '{ZipFileName}' does not exists.", imagesZipFile);
                    return;
                }

                string imagePath = Path.Combine(webroot,"images");
                string[] imageFiles = Directory.GetFiles(imagePath).Select(file => Path.GetFileName(file)).ToArray();

                using(ZipArchive  zip = ZipFile.Open(imagesZipFile,ZipArchiveMode.Read)){

                    foreach (ZipArchiveEntry  entry in zip.Entries)
                    {
                        if(imageFiles.Contains(entry.Name)){

                            string destinationFilename = Path.Combine(imagePath,entry.Name);
                            if(File.Exists(destinationFilename)){
                                File.Delete(destinationFilename);
                            }
                            entry.ExtractToFile(destinationFilename);
                        }
                        else {
                            logger.LogWarning("Skipped file '{FileName}' in zipfile '{ZipFileName}'", entry.Name, imagesZipFile);
                        }
                    }
                }

            }
            catch (System.Exception ex)
            {
                
                logger.LogError(ex, "EXCEPTION ERROR: {Message}", ex.Message); 
            }
        }
        
    }
}
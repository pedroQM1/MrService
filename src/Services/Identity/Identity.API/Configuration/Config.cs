using System.Collections.Generic;
using IdentityServer4.Models;

namespace Identity.API.Configuration
{
    public class Config
    {
        public static IEnumerable<ApiResource> GetApis(){
            
            return new List<ApiResource>{

                new ApiResource("ServiceRequest","Service Request")
            };
        }
        public static IEnumerable<IdentityResource> GetResources(){
            return new List<IdentityResource>{
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };
        }

        public static IEnumerable<Client> GetClients(Dictionary<string,string> clientesUrl){

            return new List<Client>{

                new Client{

                }
            };
        }
    }
}
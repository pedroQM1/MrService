using Microsoft.Extensions.DependencyInjection;

namespace Identity.API.Devspaces
{
    static class IdentityDevspaceBuilderExtensions
    {
         public static IIdentityServerBuilder AddDevSpacesIfNeeded(this IIdentityServerBuilder builder, bool useDevspaces){

             if(useDevspaces)
                builder.AddRedirectUriValidator<DevspaceRedirectUriValidator>();

            return builder;
         }
    }
}
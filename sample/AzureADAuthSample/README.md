# Saturn Azure Active Directory Authentication Sample

This sample schows how to integrate Azure AD for authentication via its OAuth 2.0 (v2) 
and Microsoft Graph endpoints using the `use_azuread_oauth` extension from the `Saturn.Extensions.Authorization` package.

You can read more about how authetication with Azure AD works [in the official documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-app-types).

## Enabling Azure AD Authentication

To enable Azure AD Auth you nee to: 
- [Set up a new Azure AD Tenant](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-create-new-tenant), or use an existing one.
- [Register a new application](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app), or use an exiting registration. The sample uses `http://localhost:8085/auth` as the callback path, so configure that in the App Registration.
- Enter your `tenantId`, `clientId` and `clientSecret` into the sample code.
- [Create a user in your directory](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/add-users-azure-active-directory), if you do not have one.
- Run the sample, visit http://localhost:8085/ and login as your created user. You will get the name of the user returned.

## Further Reading

### Microsoft Graph API

User Information is provided via the Graph API endpoints. What you can read from the graph is defined in "scopes". 
You can read more about scopes [in the official documentation](https://docs.microsoft.com/en-us/graph/permissions-reference). 
Scopes need to be set for the app in the [App Registration's](https://aka.ms/AppRegistrationsPreview) `API Permissions` tab and additionally need to be set up in the app iteself.

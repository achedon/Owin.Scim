namespace Owin.Scim.Tests.Integration.Users.Create
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;

    using Machine.Specifications;

    using Model;
    using Model.Users;

    using v2.Model;

    public class with_multiple_validation_errors : when_creating_a_user
    {
        Establish context = async () =>
        {
            var userName = UserNameUtility.GenerateUserName();
            var initialUser = new ScimUser2
            {
                UserName = userName
            };

            // Insert the first user so there's one already in-memory.
            Response = await Server
                .HttpClient
                .PostAsync("v2/users", new ObjectContent<ScimUser>(initialUser, new JsonMediaTypeFormatter()))
                .AwaitResponse()
                .AsTask;
            
            UserDto = new ScimUser2
            {
                UserName = userName,
                PreferredLanguage = "invalidLanguage"
            };
        };

        It should_return_bad_request = () => Response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    }
}
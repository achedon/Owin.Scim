﻿namespace Owin.Scim.Tests.Integration.Groups.Create
{
    using System.Net;
    using Machine.Specifications;
    using Model.Users;
    using Model.Groups;

    using v2.Model;

    public class with_invalid_member_type : when_creating_a_group
    {
        Establish context = () =>
        {
            ExistingUser = CreateUser(new ScimUser2 { UserName = Users.UserNameUtility.GenerateUserName() });

            GroupDto = new ScimGroup2
            {
                DisplayName = "hello",
                ExternalId = "hello",
                Members = new []
                {
                    new Member {Value = ExistingUser.Id, Type = "bad"},
                }
            };
        };

        It should_return_bad_request = () => Response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);

        It should_return_invalid_syntax = () => Error.ScimType.ShouldEqual(Model.ScimErrorType.InvalidSyntax);

        It should_return_indicate_invalid_attribute = () => Error.Detail.ShouldContain("member.type");

        private static ScimUser ExistingUser;
    }
}
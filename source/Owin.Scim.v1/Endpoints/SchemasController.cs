﻿namespace Owin.Scim.v1.Endpoints
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Configuration;

    using Extensions;

    using Scim.Endpoints;
    using Scim.Services;

    [AllowAnonymous]
    [RoutePrefix(ScimConstantsV1.Endpoints.Schemas)]
    public class SchemasController : ScimControllerBase
    {
        public const string GetSchemasRouteName = @"GetSchemas1";

        private readonly ISchemaService _SchemaService;

        public SchemasController(
            ScimServerConfiguration serverConfiguration,
            ISchemaService schemaService) 
            : base(serverConfiguration)
        {
            _SchemaService = schemaService;
        }


        [Route("{schemaId?}", Name = GetSchemasRouteName)]
        public async Task<HttpResponseMessage> GetSchemas(string schemaId = null)
        {
            if (string.IsNullOrWhiteSpace(schemaId))
                return (await _SchemaService.GetSchemas())
                    .Let(schemata => SetMetaLocations(schemata, GetSchemasRouteName, schema => new { schemaId = schema.Id }))
                    .ToHttpResponseMessage(Request);

            return (await _SchemaService.GetSchema(schemaId))
                .Let(schema => SetMetaLocation(schema, GetSchemasRouteName, new { schemaId = schema.Id }))
                .ToHttpResponseMessage(Request, (schema, response) => SetContentLocationHeader(response, GetSchemasRouteName, new { schemaId = schema.Id }));
        }
    }
}
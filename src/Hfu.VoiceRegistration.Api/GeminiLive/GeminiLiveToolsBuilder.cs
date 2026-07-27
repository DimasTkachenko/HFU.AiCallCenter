namespace Hfu.VoiceRegistration.Api.GeminiLive;

using Hfu.VoiceRegistration.Domain.Registration;
using Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;

public static class GeminiLiveToolsBuilder
{
    public static GeminiToolDeclaration BuildRegistrationTools()
    {
        return new GeminiToolDeclaration
        {
            FunctionDeclarations = new List<GeminiFunctionDeclaration>
            {
                new()
                {
                    Name = "update_registration_fields",
                    Description = "Save captured HFU registration field values in the backend draft. Use Ukrainian region names when saving regions.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            fields = new
                            {
                                type = "ARRAY",
                                description = "Registration fields to save.",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        name = new
                                        {
                                            type = "STRING",
                                            description = "Backend registration field name."
                                        },
                                        value = new
                                        {
                                            type = "STRING",
                                            description = "Captured field value. Use ISO yyyy-MM-dd for dateOfBirth and a number string for displacedCertificateYear."
                                        },
                                        rawValue = new
                                        {
                                            type = "STRING",
                                            description = "Original user wording when it differs from the normalized value."
                                        }
                                    },
                                    required = new[] { "name", "value" }
                                }
                            }
                        },
                        required = new[] { "fields" }
                    }
                },
                new()
                {
                    Name = "confirm_registration_fields",
                    Description = "Mark captured registration fields as explicitly confirmed by the user.",
                    Parameters = CreateFieldNamesSchema()
                },
                new()
                {
                    Name = "mark_fields_for_clarification",
                    Description = "Mark registration fields as needing clarification when the user answer is ambiguous or incomplete.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            fieldNames = CreateFieldNamesArraySchema(),
                            reason = new
                            {
                                type = "STRING",
                                description = "Short reason to show in diagnostics and state."
                            }
                        },
                        required = new[] { "fieldNames" }
                    }
                },
                new()
                {
                    Name = "clear_registration_fields",
                    Description = "Clear incorrect registration fields from the backend draft.",
                    Parameters = CreateFieldNamesSchema()
                },
                new()
                {
                    Name = "get_registration_state",
                    Description = "Read the current backend registration state, including missing fields, clarification needs, confirmation needs, and completion issues.",
                    Parameters = new { type = "OBJECT", properties = new { } }
                },
                new()
                {
                    Name = "complete_registration",
                    Description = "Complete the demo registration only after the user gives personal data consent and final confirmation.",
                    Parameters = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            personalDataConsent = new
                            {
                                type = "BOOLEAN",
                                description = "True only when the user explicitly agrees to personal data processing."
                            },
                            registrationConfirmed = new
                            {
                                type = "BOOLEAN",
                                description = "True only when the user explicitly gives final registration confirmation."
                            }
                        },
                        required = new[] { "personalDataConsent", "registrationConfirmed" }
                    }
                }
            }
        };
    }

    private static object CreateFieldNamesSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                fieldNames = CreateFieldNamesArraySchema()
            },
            required = new[] { "fieldNames" }
        };
    }

    private static object CreateFieldNamesArraySchema()
    {
        return new
        {
            type = "ARRAY",
            description = "Backend registration field names.",
            items = new
            {
                type = "STRING",
                description = "Backend registration field name."
            }
        };
    }
}

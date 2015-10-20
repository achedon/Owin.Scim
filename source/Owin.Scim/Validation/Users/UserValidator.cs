﻿namespace Owin.Scim.Validation.Users
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Extensions;

    using FluentValidation;

    using Model.Users;

    using Repository;

    using Security;

    public class UserValidator : ValidatorBase<User>
    {
        private readonly IUserRepository _UserRepository;

        private readonly IVerifyPasswordComplexity _PasswordComplexityVerifier;

        private readonly IManagePasswords _PasswordManager;

        public UserValidator(
            IUserRepository userRepository,
            IVerifyPasswordComplexity passwordComplexityVerifier,
            IManagePasswords passwordManager)
        {
            _UserRepository = userRepository;
            _PasswordComplexityVerifier = passwordComplexityVerifier;
            _PasswordManager = passwordManager;
        }

        protected override async Task<ValidationResult> ValidateAsyncInternal(User entity, string ruleSet = RuleSetConstants.Default)
        {
            var validator = await CreateFluentValidator();

            var result = await validator.ValidateAsync(entity, ruleSet: ruleSet);
            
            return new ValidationResult(
                errorMessages:
                result.Errors.Any() 
                    ? result.Errors.Select(e => e.ToString()) 
                    : null);
        }

        private Task<IValidator<User>> CreateFluentValidator()
        {
            return Task.FromResult<IValidator<User>>(
                new FluentUserValidator(_UserRepository, _PasswordComplexityVerifier, _PasswordManager));
        }

        private class FluentUserValidator : AbstractValidator<User>
        {
            private readonly IUserRepository _UserRepository;

            private readonly IVerifyPasswordComplexity _PasswordComplexityVerifier;

            private readonly IManagePasswords _PasswordManager;

            private string _UserId;

            public FluentUserValidator(
                IUserRepository userRepository,
                IVerifyPasswordComplexity passwordComplexityVerifier,
                IManagePasswords passwordManager)
            {
                _UserRepository = userRepository;
                _PasswordComplexityVerifier = passwordComplexityVerifier;
                _PasswordManager = passwordManager;

                var userRecord = new Lazy<User>(() => GetUser().Result, LazyThreadSafetyMode.ExecutionAndPublication);
                ConfigureDefaultRuleSet();
                ConfigureCreateRuleSet();
                ConfigureUpdateRuleSet(userRecord);
            }

            private void ConfigureDefaultRuleSet()
            {
                RuleSet("default", () =>
                {
                    RuleFor(u => u.UserName)
                        .NotEmpty();

                    When(user => !string.IsNullOrWhiteSpace(user.PreferredLanguage),
                        () =>
                        {
                            RuleFor(user => user.PreferredLanguage)
                                .Must(ValidatePreferredLanguage);
                        });
                    When(user => !string.IsNullOrWhiteSpace(user.Locale),
                        () =>
                        {
                            RuleFor(user => user.Locale)
                                .Must(locale =>
                                {
                                    try
                                    {
                                        CultureInfo.GetCultureInfo(locale);
                                        return true;
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    return false;
                                });
                        });
                    When(user => user.Emails != null && user.Emails.Any(),
                        () =>
                        {
                            RuleFor(user => user.Emails)
                                .Must(emails => emails.Count(e => e.Primary) <= 1)  // User can only have one primary email.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<Email>
                                    {
                                        { email => email.Value, config => config.NotEmpty().EmailAddress() }
                                    });
                        });
                    When(user => user.Ims != null && user.Ims.Any(),
                        () =>
                        {
                            RuleFor(user => user.Ims)
                                .Must(ims => ims.Count(im => im.Primary) <= 1)  // User can only have one primary email.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<InstantMessagingAddress>
                                    {
                                        {
                                            im => im.Value,
                                            config => config.NotEmpty()
                                        }
                                    });
                        });
                    When(user => user.PhoneNumbers != null && user.PhoneNumbers.Any(),
                        () =>
                        {
                            // TODO: (DG) Add validation / configuration for PhoneNumberTypes for validation.
                            /* The value SHOULD be specified according to the format defined 
                               in [RFC3966], e.g., 'tel:+1-201-555-0123'. */

                            RuleFor(user => user.PhoneNumbers)
                                .Must(numbers => numbers.Count(n => n.Primary) <= 1)  // User can only have one primary number.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<PhoneNumber>
                                    {
                                        {
                                            pn => pn.Value,
                                            config => config.NotEmpty().Must(PhoneNumbers.PhoneNumberUtil.IsViablePhoneNumber)
                                        }
                                    });
                        });
                    When(user => user.Photos != null && user.Photos.Any(),
                        () =>
                        {
                            RuleFor(user => user.Photos)
                                .Must(photos => photos.Count(p => p.Primary) <= 1)  // User can only have one primary photo.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<Photo>
                                    {
                                        {
                                            photo => photo.Value,
                                            config =>
                                                config.NotEmpty()
                                                    .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                                        }
                                    });
                        });
                    When(user => user.Addresses != null && user.Addresses.Any(),
                        () =>
                        {
                            RuleFor(user => user.Addresses)
                                .Must(addresses => addresses.Count(a => a.Primary) <= 1)  // User can only have one primary address.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<Address>
                                    {
                                        v => v.When(a => !string.IsNullOrWhiteSpace(a.Country),
                                            () =>
                                            {
                                                v.RuleFor(a => a.Country)
                                                    .Must(countryCode =>
                                                    {
                                                        try
                                                        {
                                                            new RegionInfo(countryCode);
                                                            return true;
                                                        }
                                                        catch
                                                        {
                                                        }

                                                        return false;
                                                    });
                                            })
                                    });
                        });
                    When(user => user.Entitlements != null && user.Entitlements.Any(),
                        () =>
                        {
                            RuleFor(user => user.Entitlements)
                                .Must(entitlements => entitlements.Count(e => e.Primary) <= 1)  // User can only have one primary entitlement.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<Entitlement>
                                    {
                                        { entitlement => entitlement.Value, config => config.NotEmpty() }
                                    });
                        });
                    When(user => user.Roles != null && user.Roles.Any(),
                        () =>
                        {
                            RuleFor(user => user.Roles)
                                .Must(roles => roles.Count(r => r.Primary) <= 1)  // User can only have one primary role.
                                .SetCollectionValidator(
                                    new GenericExpressionValidator<Role>
                                    {
                                        { role => role.Value, config => config.NotEmpty() }
                                    });
                        });
                });
            }

            private void ConfigureCreateRuleSet()
            {
                RuleSet("create", () =>
                {
                    RuleFor(user => user.UserName)
                        .MustAsync(async userName =>
                        {
                            /* Before comparing or evaluating the uniqueness of a "userName" or 
                               "password" attribute, service providers MUST use the preparation, 
                               enforcement, and comparison of internationalized strings (PRECIS) 
                               preparation and comparison rules described in Sections 3 and 4, 
                               respectively, of [RFC7613], which is based on the PRECIS framework
                               specification [RFC7564]. */

                            return await _UserRepository.IsUserNameAvailable(
                                Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(userName)));
                        })
                        .WithState(_ => 409)
                        .WithMessage("UserName is already in use.");

                    When(user => !string.IsNullOrWhiteSpace(user.Password),
                        () =>
                        {
                            /* Before comparing or evaluating the uniqueness of a "userName" or 
                               "password" attribute, service providers MUST use the preparation, 
                               enforcement, and comparison of internationalized strings (PRECIS) 
                               preparation and comparison rules described in Sections 3 and 4, 
                               respectively, of [RFC7613], which is based on the PRECIS framework
                               specification [RFC7564]. */

                            RuleFor(user => user.Password)
                                .MustAsync(password => _PasswordComplexityVerifier.MeetsRequirements(
                                    Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(password))));
                        });
                });
            }

            private void ConfigureUpdateRuleSet(Lazy<User> userRecord)
            {
                RuleSet("update", () =>
                {
                    RuleFor(user => user.Id)
                        .Immutable(() => userRecord.Value.Id, StringComparer.OrdinalIgnoreCase);

                    // Updating a username validation
                    When(user =>
                        !string.IsNullOrWhiteSpace(user.UserName) &&
                        !Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(user.UserName))
                            .Equals(Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(userRecord.Value.UserName)),
                                StringComparison.OrdinalIgnoreCase),
                        () =>
                        {
                            RuleFor(user => user.UserName)
                                .MustAsync(async (user, userName) =>
                                {
                                    /* Before comparing or evaluating the uniqueness of a "userName" or 
                                       "password" attribute, service providers MUST use the preparation, 
                                       enforcement, and comparison of internationalized strings (PRECIS) 
                                       preparation and comparison rules described in Sections 3 and 4, 
                                       respectively, of [RFC7613], which is based on the PRECIS framework
                                       specification [RFC7564]. */
                                    
                                    return await _UserRepository.IsUserNameAvailable(
                                        Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(userName)));
                                })
                                .WithMessage("UserName is already in use.");
                        });

                    // Updating a user password
                    When(user =>
                        !string.IsNullOrWhiteSpace(user.Password) &&
                        (userRecord.Value.Password == null ||
                         !_PasswordManager.VerifyHash(
                             Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(user.Password)),
                             Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(userRecord.Value.Password)))),
                        () =>
                        {
                            /* Before comparing or evaluating the uniqueness of a "userName" or 
                               "password" attribute, service providers MUST use the preparation, 
                               enforcement, and comparison of internationalized strings (PRECIS) 
                               preparation and comparison rules described in Sections 3 and 4, 
                               respectively, of [RFC7613], which is based on the PRECIS framework
                               specification [RFC7564]. */

                            RuleFor(user => user.Password)
                                .MustAsync(password => _PasswordComplexityVerifier.MeetsRequirements(
                                    Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(password))));
                        });
                });
            }

            public override Task<FluentValidation.Results.ValidationResult> ValidateAsync(ValidationContext<User> context)
            {
                _UserId = context.InstanceToValidate.Id;

                return base.ValidateAsync(context);
            }

            private async Task<User> GetUser()
            {
                return await _UserRepository.GetUser(_UserId);
            }

            /// <summary>
            /// The value indicates the set of natural languages that are preferred. 
            /// The format of the value is the same as the HTTP Accept-Language header 
            /// field (not including "Accept-Language:") and is specified in Section 
            /// 5.3.5 of[RFC7231].  The intent of this value is to enable cloud 
            /// applications to perform matching of language tags [RFC4647]
            /// </summary>
            /// <param name="preferredLanguage"></param>
            /// <returns></returns>
            private bool ValidatePreferredLanguage(string preferredLanguage)
            {
                IEnumerable<Tuple<string, decimal>> stringsWithQuality;
                if (TryParseWeightedValues(preferredLanguage, out stringsWithQuality))
                {
                    if (stringsWithQuality.Any(
                        langWithQuality =>
                        {
                            try
                            {
                                CultureInfo.GetCultureInfo(langWithQuality.Item1);
                                return true;
                            }
                            catch (CultureNotFoundException) { }

                            return false;
                        }))
                    {
                        return true;
                    }
                }

                return false;
            }
            
            private bool TryParseWeightedValues(string multipleValueStringWithQuality, out IEnumerable<Tuple<string, decimal>> stringsWithQuality)
            {
                stringsWithQuality = null;

                if (string.IsNullOrWhiteSpace(multipleValueStringWithQuality)) return false;

                var values = multipleValueStringWithQuality.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                if (!values.Any()) return false;

                var parsed = values.Select(x =>
                {
                    var sections = x.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    var mediaRange = sections[0].Trim();
                    var quality = 1m;

                    for (var index = 1; index < sections.Length; index++)
                    {
                        var trimmedValue = sections[index].Trim();
                        if (trimmedValue.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal temp;
                            var stringValue = trimmedValue.Substring(2);
                            if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out temp))
                            {
                                quality = temp;
                            }
                        }
                        else
                        {
                            mediaRange += ";" + trimmedValue;
                        }
                    }

                    return new Tuple<string, decimal>(mediaRange, quality);
                });

                if (!parsed.Any()) return false;

                stringsWithQuality = parsed.OrderByDescending(x => x.Item2).ToArray();

                return true;
            }
        }
    }
}